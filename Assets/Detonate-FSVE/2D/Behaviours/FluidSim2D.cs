using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Detonate
{
    [System.Serializable]
    public class RenderTextureEvent : UnityEvent<RenderTexture> { };//event that passes render texture

    public class FluidSim2D : MonoBehaviour
    {
        [SerializeField] FluidSim2DParams sim_params = new FluidSim2DParams();

        [Space]
        [SerializeField] Vector2 impulse_position = new Vector2(0.5f, 0);
        [SerializeField] float impulse_radius = 1.0f;
        [SerializeField] float density_amount = 1.0f;
        [SerializeField] private float temperature_amount = 10.0f;

        //Compute shaders
        [Space] [Header("GPU Functions")]
        [SerializeField] ComputeShader jacobi = null;
        [SerializeField] ComputeShader advect = null;
        [SerializeField] ComputeShader buoyancy = null;
        [SerializeField] ComputeShader impulse = null;
        [SerializeField] ComputeShader divergence = null;
        [SerializeField] ComputeShader projection = null;
        [SerializeField] ComputeShader obstacles = null;

        [Space] 
        [SerializeField] RenderTextureEvent on_texture_update;

        //Render textures used for simulation (grids)
        [SerializeField]private RenderTexture[] velocity_grids = new RenderTexture[2];
        [SerializeField]private RenderTexture[] density_grids = new RenderTexture[2];
        [SerializeField]private RenderTexture[] temperature_grids = new RenderTexture[2];
        [SerializeField]private RenderTexture[] preassure_grids = new RenderTexture[2];
        [SerializeField]private RenderTexture obstacle_grid; //will only read
        [SerializeField]private RenderTexture temp_grid; //used for storing temporary grid states

        private Vector2 inverse_size = Vector2.zero;

        //Number of threads required based on texture size
        private int x_thread_count = 0;
        private int y_thread_count = 0;

        //Constants
        private const uint READ = 0; //for accessing grid sets

        private const uint WRITE = 1;
        private const uint THREAD_COUNT = 8; //threads used by compute shader


        private void Start()
        {
            ValidateTextureDimensions();
            CalculateInverseSize();
            CalculateThreadCount();
            CreateGridSets(); //creates render texture grid sets
            SetBoundary();
        }


        private void ValidateTextureDimensions()
        {
            sim_params.width = Mathf.ClosestPowerOfTwo(sim_params.width);
            sim_params.height = Mathf.ClosestPowerOfTwo(sim_params.height);
        }


        private void CalculateInverseSize()
        {
            inverse_size = new Vector2(1.0f /*/ sim_params.width*/,
                1.0f /*/ sim_params.height*/);
        }


        private void CalculateThreadCount()
        {
            x_thread_count = (int) (sim_params.width / THREAD_COUNT);
            y_thread_count = (int) (sim_params.height / THREAD_COUNT);
        }


        private void Swap(ref RenderTexture[] _grid)
        {
            RenderTexture temp = _grid[READ];
            _grid[READ] = _grid[WRITE];
            _grid[WRITE] = temp;
        }


        private void Update()
        {
            //advect grids with quantities
            ApplyAdvection(sim_params.temperature_dissipation, 0.0f, ref temperature_grids);
            ApplyAdvection(sim_params.density_dissipation, 0.0f, ref density_grids);
            //TODO advect reaction

            //apply advections
            ApplyAdvectionVelocity();
            ApplyBuoyancy();

            //apply impulses
            ApplyImpulse(density_amount, ref density_grids);
            ApplyImpulse(temperature_amount, ref temperature_grids);

            //extinguishment
            //vorticity

            CalculateDivergence();
            CalculatePressure();
            CalculateProjection();

            on_texture_update.Invoke(density_grids[READ]);
        }


        private void ApplyAdvection(float _dissipation, float _decay, ref RenderTexture[] _grids,
            float _forward = 1.0f)
        {
            //set compute vars
            advect.SetFloat("dt", Time.deltaTime);
            advect.SetFloat("forward", _forward);
            advect.SetFloat("dissipation", _dissipation);
            advect.SetFloat("decay", _decay);

            //set texture grids
            int kernel_id = advect.FindKernel("Advect");
            advect.SetTexture(kernel_id, "write_R", _grids[WRITE]); //ony 1 channel
            advect.SetTexture(kernel_id, "read_R", _grids[READ]);
            advect.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            advect.SetTexture(kernel_id, "obstacles", obstacle_grid);

            //run calculation on GPU
            advect.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(ref _grids); //swap read and write grids
        }


        private void ApplyAdvectionVelocity()
        {
            //set compute vars
            advect.SetFloat("dt", Time.deltaTime);
            advect.SetFloat("dissipation", sim_params.velocity_dissipation);
            advect.SetFloat("forward", 1.0f);
            advect.SetFloat("decay", 0.0f);

            //set texture grids
            int kernel_id = advect.FindKernel("AdvectVelocity");
            advect.SetTexture(kernel_id, "read_RG", velocity_grids[READ]); //two channels to represent vector components
            advect.SetTexture(kernel_id, "write_RG", velocity_grids[WRITE]);
            advect.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            advect.SetTexture(kernel_id, "obstacles", obstacle_grid);

            //run calculation on GPU
            advect.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(ref velocity_grids); //swap read and write velocity grids
        }


        private void ApplyBuoyancy()
        {
            //set compute vars
            buoyancy.SetFloat("dt", Time.deltaTime);
            buoyancy.SetVector("up", new Vector4(0, 1, 0, 0)); //y is up
            buoyancy.SetFloat("weight", sim_params.smoke_weight);
            buoyancy.SetFloat("buoyancy", sim_params.smoke_buoyancy);
            buoyancy.SetFloat("ambient_temperature", sim_params.ambient_temperature);

            //set texture grids
            int kernel_id = buoyancy.FindKernel("ApplyBuoyancy");
            buoyancy.SetTexture(kernel_id, "temperature", temperature_grids[READ]);
            buoyancy.SetTexture(kernel_id, "write_RG", velocity_grids[WRITE]);
            buoyancy.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            buoyancy.SetTexture(kernel_id, "density", density_grids[READ]);

            //run calculation on GPU
            buoyancy.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(ref velocity_grids);
        }


        private void ApplyImpulse(float _amount, ref RenderTexture[] _grids)
        {     
            impulse.SetVector("inverse_size", inverse_size);
            impulse.SetFloat("dt", Time.deltaTime);
            impulse.SetFloat("radius", impulse_radius);
            impulse.SetFloat("source_amount", _amount);
            impulse.SetVector("source_pos", impulse_position);

            int kernel_id = impulse.FindKernel("Impulse");
            impulse.SetTexture(kernel_id, "write_R", _grids[WRITE]);
            impulse.SetTexture(kernel_id, "read_R", _grids[READ]);

            impulse.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(ref _grids);
        }


        private void CalculateDivergence()
        {
            //set texture grids
            int kernel_id = divergence.FindKernel("Divergence");
            divergence.SetTexture(kernel_id, "write_RG", temp_grid);
            divergence.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            divergence.SetTexture(kernel_id, "obstacles", obstacle_grid);
            divergence.SetVector("inverse_size", inverse_size);

            //run calculation on GPU
            divergence.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
        }


        private void CalculatePressure()
        {
            int kernel_id = jacobi.FindKernel("Jacobi");
            jacobi.SetVector("inverse_size", inverse_size);
            jacobi.SetTexture(kernel_id, "divergence", temp_grid);
            jacobi.SetTexture(kernel_id, "obstacles", obstacle_grid);

            for (int i = 0; i < sim_params.jacobi_iterations; ++i)
            {
                jacobi.SetTexture(kernel_id, "write_R", preassure_grids[WRITE]);
                jacobi.SetTexture(kernel_id, "pressure", preassure_grids[READ]);
                jacobi.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
                Swap(ref preassure_grids);
            }
        }


        private void CalculateProjection()
        {
            int kernel_id = projection.FindKernel("Projection");
            projection.SetTexture(kernel_id, "obstacles", obstacle_grid);
            projection.SetVector("inverse_size", inverse_size);
            projection.SetTexture(kernel_id, "pressure", preassure_grids[READ]);
            projection.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            projection.SetTexture(kernel_id, "write_RG", velocity_grids[WRITE]);

            projection.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(ref velocity_grids);
        }


        private void SetBoundary()
        {
            Debug.Log("Setting bounds");
            int kernel_id = obstacles.FindKernel("Boundary");
            obstacles.SetVector("inverse_size", inverse_size);
            obstacles.SetTexture(kernel_id, "write_R", obstacle_grid);
            obstacles.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
        }


        #region Render Texture "Grid" Creation

        //Create all the required grids.
        private void CreateGridSets()
        {
            CreateGridSet(ref velocity_grids, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
            CreateGridSet(ref density_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateGridSet(ref temperature_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateGridSet(ref preassure_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);

            //Obstacles grid will only be read, so only one grid needed
            CreateGrid(ref obstacle_grid, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateGrid(ref temp_grid, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
        }


        //Create a read and write grid for the given grid set.
        private void CreateGridSet(ref RenderTexture[] _grid, RenderTextureFormat _format,
            FilterMode _filter)
        {
            CreateGrid(ref _grid[READ], _format, _filter);
            CreateGrid(ref _grid[WRITE], _format, _filter);
        }


        //Create and intialise the grid render texture.
        private void CreateGrid(ref RenderTexture _grid, RenderTextureFormat _format, FilterMode _filter)
        {
            _grid = new RenderTexture(sim_params.width, sim_params.height,
                0, _format, RenderTextureReadWrite.Linear)
            {
                filterMode = _filter,
                wrapMode = TextureWrapMode.Clamp
            };
            _grid.Create();
        }
        #endregion

    }
}
