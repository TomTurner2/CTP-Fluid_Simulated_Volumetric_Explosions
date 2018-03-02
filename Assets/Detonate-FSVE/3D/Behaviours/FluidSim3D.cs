using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Detonate
{
    public class FluidSim3D : MonoBehaviour
    {
        [Header("Draw Debug")]
        [SerializeField] private bool draw_bounds = true;

        [Space]
        [SerializeField] FluidSim3DParams sim_params = new FluidSim3DParams();

        [Space]
        [SerializeField] Vector3 impulse_position = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField] Transform impulse_target_transform = null;
        [SerializeField] float impulse_radius = 1.0f;
        [SerializeField] float density_amount = 1.0f;
        [SerializeField] private float temperature_amount = 10.0f;

        //Compute shaders
        [Space]
        [Header("GPU Functions")]
        [SerializeField] ComputeShader jacobi = null;
        [SerializeField] ComputeShader advect = null;
        [SerializeField] ComputeShader buoyancy = null;
        [SerializeField] ComputeShader impulse = null;
        [SerializeField] ComputeShader divergence = null;
        [SerializeField] ComputeShader projection = null;
        [SerializeField] ComputeShader obstacles = null;
        [SerializeField] ComputeShader output_converter = null;

        [SerializeField] private VolumeRenderer output_renderer = null;

        private RenderTexture volume_output;
        private ComputeBuffer[] density_grids = new ComputeBuffer[2];
        private ComputeBuffer[] velocity_grids = new ComputeBuffer[2];
        private ComputeBuffer[] temperature_grids = new ComputeBuffer[2];
        private ComputeBuffer[] pressure_grids = new ComputeBuffer[2];
        private ComputeBuffer divergence_grid;
        private ComputeBuffer obstacle_grid;

        private Vector3 size = Vector3.zero;

        private int x_thread_count = 0;
        private int y_thread_count = 0;
        private int z_thread_count = 0;

        private const uint READ = 0; //for accessing grid sets
        private const uint WRITE = 1;
        private const uint THREAD_COUNT = 8; //threads used by compute shader
        private const float DT = 0.1f;//simulation blows up with large time steps?


        private void Start()
        {
            ResetSim();
        }


        public void ResetSim()
        {
            CalculateSize();
            CalculateThreadCount();
            CreateGridSets(); //creates render texture grid sets
            SetBoundary();
            CreateOutputTexture();
        }


        private void CalculateSize()
        {
            ValidateDimensions();
            size = new Vector3(sim_params.width, sim_params.height,
                sim_params.depth);
        }


        private void ValidateDimensions()
        {
            sim_params.width = Mathf.ClosestPowerOfTwo(sim_params.width);
            sim_params.height = Mathf.ClosestPowerOfTwo(sim_params.height);
            sim_params.depth = Mathf.ClosestPowerOfTwo(sim_params.depth);
        }


        private void CalculateThreadCount()
        {
            x_thread_count = (int)(sim_params.width / THREAD_COUNT);
            y_thread_count = (int)(sim_params.height / THREAD_COUNT);
            z_thread_count = (int) (sim_params.depth / THREAD_COUNT);
        }


        private void CreateOutputTexture()
        {   
            volume_output = new RenderTexture(sim_params.width, sim_params.height, sim_params.depth)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                volumeDepth = sim_params.depth,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };

            //must be set before creation
            volume_output.Create();
        }


        private void CreateGridSets()
        {
            int buffer_size = sim_params.width * sim_params.height * sim_params.depth;

            CreateDensityGrids(buffer_size);
            CreateVelocityGrids(buffer_size);
            CreateTemperatureGrids(buffer_size);
            CreatePressureGrids(buffer_size);
            
            divergence_grid = new ComputeBuffer(buffer_size, sizeof(float) * 3);
            obstacle_grid = new ComputeBuffer(buffer_size, sizeof(float));
        }


        private void CreateDensityGrids(int _buffer_size)
        {
            density_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            density_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));
        }


        private void CreateVelocityGrids(int _buffer_size)
        {
            velocity_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float) * 3);//will store float 3
            velocity_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float) * 3);
        }


        private void CreateTemperatureGrids(int _buffer_size)
        {
            temperature_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            temperature_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));
        }


        private void CreatePressureGrids(int _buffer_size)
        {
            pressure_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            pressure_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));
        }


        private void SetBoundary()
        {
            obstacles.SetVector("size", size);
            int kernel_id = obstacles.FindKernel("Boundary");
            obstacles.SetBuffer(kernel_id, "write_R", obstacle_grid);
            obstacles.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
        }


        private void Swap(ComputeBuffer[] _grid)
        {
            ComputeBuffer temp = _grid[READ];
            _grid[READ] = _grid[WRITE];
            _grid[WRITE] = temp;
        }


        private void Update()
        {
            MoveStage();
            AddForcesStage();
            CalculateDivergence();//i.e. fluid diffusion
            MassConservationStage();
            UpdateVolumeRenderer();//may want to interact with shader directly
        }


        private void MoveStage()
        {
            ApplyAdvection(sim_params.temperature_dissipation, temperature_grids);//move temperature
            ApplyAdvection(sim_params.density_dissipation, density_grids);//move densities
            ApplyAdvectionVelocity();//move velocites
        }


        private void AddForcesStage()
        {
            ApplyBuoyancy();
            ApplyImpulse(density_amount, density_grids);
            ApplyImpulse(temperature_amount, temperature_grids);
        }


        private void MassConservationStage()
        {
            CalculatePressure();//produce pressure gradient
            CalculateProjection();//use pressure gradient to maintain mass conservation
        }


        private void UpdateVolumeRenderer()
        {
            if (output_renderer == null)
                return;

            ////convert structured buffer to 3d volume texture using gpu
            int kernel_id = output_converter.FindKernel("ConvertToVolume");
            output_converter.SetBuffer(kernel_id, "read_R", density_grids[READ]);
            output_converter.SetTexture(kernel_id, "write_R", volume_output);
            output_converter.SetVector("size", size);
            output_converter.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);

            output_renderer.size = size;
            output_renderer.texture = volume_output;
        }


        private void ApplyAdvection(float _dissipation, ComputeBuffer[] _grids)
        {
            advect.SetVector("size", size);
            advect.SetFloat("dt", DT);
            advect.SetFloat("dissipation", _dissipation);

            int kernel_id = advect.FindKernel("Advect");
            advect.SetBuffer(kernel_id, "read_R", _grids[READ]);
            advect.SetBuffer(kernel_id, "write_R", _grids[WRITE]);
            advect.SetBuffer(kernel_id, "velocity", velocity_grids[READ]);
            advect.SetBuffer(kernel_id, "obstacles", obstacle_grid);

            advect.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
            Swap(_grids);
        }

        private void ApplyAdvectionVelocity()
        {
            advect.SetVector("size", size);
            advect.SetFloat("dt", DT);
            advect.SetFloat("dissipation", sim_params.velocity_dissipation);
            advect.SetFloat("forward", 1.0f);
            advect.SetFloat("decay", sim_params.velocity_dissipation);

            int kernel_id = advect.FindKernel("AdvectVelocity");
            advect.SetBuffer(kernel_id, "read_RGB", velocity_grids[READ]);
            advect.SetBuffer(kernel_id, "write_RGB", velocity_grids[WRITE]);
            advect.SetBuffer(kernel_id, "velocity", velocity_grids[READ]);
            advect.SetBuffer(kernel_id, "obstacles", obstacle_grid);

            advect.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
            Swap(velocity_grids);
        }


        private void ApplyBuoyancy()
        {
            buoyancy.SetVector("size", size);
            buoyancy.SetVector("up", new Vector4(0,1,0,0));
            buoyancy.SetFloat("buoyancy", sim_params.smoke_buoyancy);
            buoyancy.SetFloat("weight", sim_params.smoke_weight);
            buoyancy.SetFloat("ambient_temperature", sim_params.ambient_temperature);
            buoyancy.SetFloat("dt", DT);

            int kernel_id = buoyancy.FindKernel("ApplyBuoyancy");
            buoyancy.SetBuffer(kernel_id, "write_RGB", velocity_grids[WRITE]);
            buoyancy.SetBuffer(kernel_id, "velocity", velocity_grids[READ]);
            buoyancy.SetBuffer(kernel_id, "density", density_grids[READ]);
            buoyancy.SetBuffer(kernel_id, "temperature", temperature_grids[READ]);

            buoyancy.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
            Swap(velocity_grids);
        }


        private void ApplyImpulse(float _amount, ComputeBuffer[] _grids)
        {
            impulse.SetVector("size", size);
            impulse.SetFloat("radius", impulse_radius);
            impulse.SetFloat("source_amount", _amount);
            impulse.SetFloat("dt", DT);

            if (impulse_target_transform != null)
            {
                impulse_position = ConvertPositionToGridSpace(impulse_target_transform.position);//use transform target as source position
            }

            impulse.SetVector("source_pos", impulse_position);

            int kernel_id = impulse.FindKernel("Impulse");
            impulse.SetBuffer(kernel_id, "read_R", _grids[READ]);
            impulse.SetBuffer(kernel_id, "write_R", _grids[WRITE]);
            impulse.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
            Swap(_grids);
        }


        public Vector3 ConvertPositionToGridSpace(Vector3 _pos)
        {
            return ConvertToGridScale(_pos - transform.localPosition);//get relative position then factor in scale
        }


        private Vector3 ConvertToGridScale(Vector3 _pos)
        {
            Vector3 scale_convert = (_pos + transform.localScale * 0.5f);

            return new Vector3(scale_convert.x / transform.localScale.x,
                scale_convert.y / transform.localScale.y, scale_convert.z / transform.localScale.z);
        }


        private void CalculateDivergence()
        {
            divergence.SetVector("size", size);
            int kernel_id = divergence.FindKernel("Divergence");
            divergence.SetBuffer(kernel_id, "write_RGB", divergence_grid);
            divergence.SetBuffer(kernel_id, "velocity", velocity_grids[READ]);
            divergence.SetBuffer(kernel_id, "obstacles", obstacle_grid);
            divergence.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
        }


        private void CalculatePressure()
        {
            jacobi.SetVector("size", size);
            int kernel_id = jacobi.FindKernel("Jacobi");
            jacobi.SetBuffer(kernel_id, "divergence", divergence_grid);
            jacobi.SetBuffer(kernel_id, "obstacles", obstacle_grid);

            for (int i = 0; i < sim_params.jacobi_iterations; ++i)
            {
                jacobi.SetBuffer(kernel_id, "write_R", pressure_grids[WRITE]);
                jacobi.SetBuffer(kernel_id, "pressure", pressure_grids[READ]);
                jacobi.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
                Swap(pressure_grids);
            }
        }


        private void CalculateProjection()
        {
            projection.SetVector("size", size);
            int kernel_id = projection.FindKernel("Projection");
            projection.SetBuffer(kernel_id, "obstacles", obstacle_grid);
            projection.SetBuffer(kernel_id, "pressure", pressure_grids[READ]);
            projection.SetBuffer(kernel_id, "velocity", velocity_grids[READ]);
            projection.SetBuffer(kernel_id, "write_RGB", velocity_grids[WRITE]);

            projection.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
            Swap(velocity_grids);
        }


        //all buffers should be released on destruction
        private void OnDestroy()
        {
            density_grids[READ].Release();
            density_grids[WRITE].Release();

            velocity_grids[READ].Release();
            velocity_grids[WRITE].Release();

            temperature_grids[READ].Release();
            temperature_grids[WRITE].Release();

            pressure_grids[READ].Release();
            pressure_grids[WRITE].Release();

            obstacle_grid.Release();
            divergence_grid.Release();
        }


        private void OnDrawGizmos()
        {
            if (draw_bounds)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }

    }
}
