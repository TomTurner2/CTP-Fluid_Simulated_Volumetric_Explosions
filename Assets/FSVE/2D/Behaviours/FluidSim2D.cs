using UnityEngine;
using UnityEngine.Events;


namespace FSVE
{
    [System.Serializable]
    public class RenderTextureEvent : UnityEvent<RenderTexture> { }// Event that passes render texture

    [System.Obsolete("Obsolete! 2D fluid sim development abandoned due to texture issue", false)]
    public class FluidSim2D : MonoBehaviour
    {
        [SerializeField] FluidSim2DParams sim_params = new FluidSim2DParams();

        [Space]
        [SerializeField] Vector2 impulse_position = new Vector2(0.5f, 0);
        [SerializeField] float impulse_radius = 1.0f;
        [SerializeField] float density_amount = 1.0f;
        [SerializeField] private float temperature_amount = 10.0f;

        // Compute shaders
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
        [Header("Debug Events")]
        [SerializeField] RenderTextureEvent velocity_read;
        [SerializeField] RenderTextureEvent density_read;
        [SerializeField] RenderTextureEvent temperature_read;
        [SerializeField] RenderTextureEvent pressure_read;

        // Render textures used for simulation (grids)
        private RenderTexture[] velocity_grids = new RenderTexture[2];
        private RenderTexture[] density_grids = new RenderTexture[2];
        private RenderTexture[] temperature_grids = new RenderTexture[2];
        private RenderTexture[] pressure_grids = new RenderTexture[2];
        private RenderTexture obstacle_grid; // Will only read
        private RenderTexture temp_grid; // Used for storing temporary grid states

        private Vector2 size = Vector2.zero;

        // Number of threads required based on texture size
        private int x_thread_count = 0;
        private int y_thread_count = 0;

        // Constants
        private const uint READ = 0; // For accessing grid sets
        private const uint WRITE = 1;
        private const uint THREAD_COUNT = 8; // Threads used by compute shader
        private const float DT = 0.05f;// Simulation blows up with large time steps?


        private void Start()
        {          
            ResetSim();
        }


        public void ResetSim()
        {
            CalculateSize();
            CalculateThreadCount();
            CreateGridSets(); // Creates render texture grid sets
            SetBoundary();
        }


        private void CalculateSize()
        {
            ValidateTextureDimensions();
            size = new Vector2(sim_params.width, sim_params.height);
        }


        private void ValidateTextureDimensions()
        {
            sim_params.width = Mathf.ClosestPowerOfTwo(sim_params.width);
            sim_params.height = Mathf.ClosestPowerOfTwo(sim_params.height);
        }


        private void CalculateThreadCount()
        {
            x_thread_count = (int) (sim_params.width / THREAD_COUNT);
            y_thread_count = (int) (sim_params.height / THREAD_COUNT);
        }


        private void Swap(RenderTexture[] _grid)
        {
            RenderTexture temp = _grid[READ];
            _grid[READ] = _grid[WRITE];
            _grid[WRITE] = temp;
        }


        private void Update()
        {
            // Advect grids with quantities
            ApplyAdvection(sim_params.temperature_dissipation, 0.0f, ref temperature_grids);
            ApplyAdvection(sim_params.density_dissipation, 0.0f, ref density_grids);

            // Apply advections
            ApplyAdvectionVelocity();
            ApplyBuoyancy();

            // Apply impulses
            ApplyImpulse(density_amount, ref density_grids);
            ApplyImpulse(temperature_amount, ref temperature_grids);

            CalculateDivergence();
            CalculatePressure();
            CalculateProjection();

            on_texture_update.Invoke(density_grids[READ]);
            UpdateDebugTextureOutput();
        }


        private void UpdateDebugTextureOutput()
        {
            velocity_read.Invoke(velocity_grids[READ]);
            density_read.Invoke(density_grids[READ]);
            temperature_read.Invoke(temperature_grids[READ]);
            pressure_read.Invoke(pressure_grids[READ]);
        }


        private void ApplyAdvection(float _dissipation, float _decay, ref RenderTexture[] _grids)
        {
            // Set compute vars
            advect.SetFloat("dt", DT);
            advect.SetFloat("forward", 1.0f);
            advect.SetFloat("dissipation", _dissipation);
            advect.SetFloat("decay", _decay);

            // Set texture grids
            int kernel_id = advect.FindKernel("Advect");
            advect.SetTexture(kernel_id, "write_R", _grids[WRITE]); // Ony 1 channel
            advect.SetTexture(kernel_id, "read_R", _grids[READ]);
            advect.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            advect.SetTexture(kernel_id, "obstacles", obstacle_grid);

            // Run calculation on GPU
            advect.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(_grids); // Swap read and write grids
        }


        private void ApplyAdvectionVelocity()
        {
            // Set compute vars
            advect.SetFloat("dt", DT);
            advect.SetFloat("dissipation", sim_params.velocity_dissipation);
            advect.SetFloat("forward", 1.0f);
            advect.SetFloat("decay", 0.0f);

            // Set texture grids
            int kernel_id = advect.FindKernel("AdvectVelocity");
            advect.SetTexture(kernel_id, "read_RG", velocity_grids[READ]); // Two channels to represent vector components
            advect.SetTexture(kernel_id, "write_RG", velocity_grids[WRITE]);
            advect.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            advect.SetTexture(kernel_id, "obstacles", obstacle_grid);

            // Run calculation on GPU
            advect.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(velocity_grids); // Swap read and write velocity grids
        }


        private void ApplyBuoyancy()
        {
            // Set compute vars
            buoyancy.SetFloat("dt", DT);
            buoyancy.SetVector("up", new Vector4(0, 1, 0, 0)); //y is up
            buoyancy.SetFloat("weight", sim_params.smoke_weight);
            buoyancy.SetFloat("buoyancy", sim_params.smoke_buoyancy);
            buoyancy.SetFloat("ambient_temperature", sim_params.ambient_temperature);

            // Set texture grids
            int kernel_id = buoyancy.FindKernel("ApplyBuoyancy");
            buoyancy.SetTexture(kernel_id, "temperature", temperature_grids[READ]);
            buoyancy.SetTexture(kernel_id, "write_RG", velocity_grids[WRITE]);
            buoyancy.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            buoyancy.SetTexture(kernel_id, "density", density_grids[READ]);

            // Run calculation on GPU
            buoyancy.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(velocity_grids);
        }


        private void ApplyImpulse(float _amount,  ref RenderTexture[] _grids)
        {     
            impulse.SetVector("size", size);
            impulse.SetFloat("dt", DT);
            impulse.SetFloat("radius", impulse_radius);
            impulse.SetFloat("source_amount", _amount);
            impulse.SetVector("source_pos", impulse_position);

            int kernel_id = impulse.FindKernel("Impulse");
            impulse.SetTexture(kernel_id, "write_R", _grids[WRITE]);
            impulse.SetTexture(kernel_id, "read_R", _grids[READ]);

            impulse.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(_grids);
        }


        private void CalculateDivergence()
        {
            // Set texture grids
            int kernel_id = divergence.FindKernel("Divergence");
            divergence.SetTexture(kernel_id, "write_RG", temp_grid);
            divergence.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            divergence.SetTexture(kernel_id, "obstacles", obstacle_grid);
            divergence.SetVector("size", size);

            // Run calculation on GPU
            divergence.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
        }


        private void CalculatePressure()
        {
            int kernel_id = jacobi.FindKernel("Jacobi");
            jacobi.SetVector("size", size);
            jacobi.SetTexture(kernel_id, "divergence", temp_grid);
            jacobi.SetTexture(kernel_id, "obstacles", obstacle_grid);

            // Clear pressure grids?
            Graphics.SetRenderTarget(pressure_grids[READ]);
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.SetRenderTarget(null);

            for (int i = 0; i < sim_params.jacobi_iterations; ++i)
            {
                jacobi.SetTexture(kernel_id, "write_R", pressure_grids[WRITE]);
                jacobi.SetTexture(kernel_id, "pressure", pressure_grids[READ]);
                jacobi.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
                Swap(pressure_grids);
            }
        }


        private void CalculateProjection()
        {
            int kernel_id = projection.FindKernel("Projection");
            projection.SetTexture(kernel_id, "obstacles", obstacle_grid);
            projection.SetVector("size", size);
            projection.SetTexture(kernel_id, "pressure", pressure_grids[READ]);
            projection.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
            projection.SetTexture(kernel_id, "write_RG", velocity_grids[WRITE]);

            projection.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
            Swap(velocity_grids);
        }


        private void SetBoundary()
        {
            int kernel_id = obstacles.FindKernel("Boundary");
            obstacles.SetVector("size", size);
            obstacles.SetTexture(kernel_id, "write_R", obstacle_grid);
            obstacles.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
        }


        #region Render Texture "Grid" Creation

        // Create all the required grids.
        private void CreateGridSets()
        {
            CreateGridSet(ref velocity_grids, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
            CreateGridSet(ref density_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateGridSet(ref temperature_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateGridSet(ref pressure_grids, RenderTextureFormat.RFloat, FilterMode.Point);

            // Obstacles grid will only be read, so only one grid needed
            CreateGrid(ref obstacle_grid, RenderTextureFormat.RFloat, FilterMode.Point);
            CreateGrid(ref temp_grid, RenderTextureFormat.RGFloat, FilterMode.Point);
        }


        // Create a read and write grid for the given grid set.
        private void CreateGridSet(ref RenderTexture[] _grid, RenderTextureFormat _format,
            FilterMode _filter)
        {
            CreateGrid(ref _grid[READ], _format, _filter);
            CreateGrid(ref _grid[WRITE], _format, _filter);
        }


        // Create and intialise the grid render texture.
        private void CreateGrid(ref RenderTexture _grid, RenderTextureFormat _format, FilterMode _filter)
        {
            _grid = new RenderTexture(sim_params.width, sim_params.height,
                0, _format, RenderTextureReadWrite.Linear)
            {
                filterMode = _filter,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            _grid.Create();
        }
        #endregion

    }
}
