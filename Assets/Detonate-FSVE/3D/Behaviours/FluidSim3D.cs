using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Detonate
{
    public class FluidSim3D : MonoBehaviour
    {
        [SerializeField] FluidSim3DParams sim_params = new FluidSim3DParams();

        [Space]
        [SerializeField] Vector3 impulse_position = new Vector3(0.5f, 0.5f, 0.5f);
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

        private ComputeBuffer[] density_grids = new ComputeBuffer[2];
        private ComputeBuffer[] velocity_grids = new ComputeBuffer[2];
        private ComputeBuffer[] temperature_grids = new ComputeBuffer[2];
        private ComputeBuffer[] pressure_grids = new ComputeBuffer[2];
        private ComputeBuffer temp_grid;
        private ComputeBuffer obstacle_grid;

        private Vector3 size = Vector3.zero;

        private int x_thread_count = 0;
        private int y_thread_count = 0;
        private int z_thread_count = 0;

        private const uint READ = 0; //for accessing grid sets
        private const uint WRITE = 1;
        private const uint THREAD_COUNT = 8; //threads used by compute shader
        private const float DT = 0.05f;//simulation blows up with large time steps?


        void Start()
        {
            ResetSim();
        }


        public void ResetSim()
        {
            CalculateSize();
            CalculateThreadCount();
            CreateGridSets(); //creates render texture grid sets
            SetBoundary();
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


        private void CreateGridSets()
        {
            int buffer_size = sim_params.width * sim_params.height * sim_params.depth;

            density_grids[READ] = new ComputeBuffer(buffer_size, sizeof(float));
            density_grids[WRITE] = new ComputeBuffer(buffer_size, sizeof(float));

            velocity_grids[READ] = new ComputeBuffer(buffer_size, sizeof(float) * 3);//will store float 3
            velocity_grids[WRITE] = new ComputeBuffer(buffer_size, sizeof(float) * 3);

            temperature_grids[READ] = new ComputeBuffer(buffer_size, sizeof(float));
            temperature_grids[WRITE] = new ComputeBuffer(buffer_size, sizeof(float));

            pressure_grids[READ] = new ComputeBuffer(buffer_size, sizeof(float));
            pressure_grids[WRITE] = new ComputeBuffer(buffer_size, sizeof(float));

            temp_grid = new ComputeBuffer(buffer_size, sizeof(float) * 3);
            obstacle_grid = new ComputeBuffer(buffer_size, sizeof(float));
        }


        private void SetBoundary()
        {
            obstacles.SetVector("size", size);
            int kernel_id = obstacles.FindKernel("Boundary");
            obstacles.SetBuffer(kernel_id, "write_R", obstacle_grid);
            obstacles.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
        }


        void Swap(ComputeBuffer[] _grid)
        {
            ComputeBuffer temp = _grid[READ];
            _grid[READ] = _grid[WRITE];
            _grid[WRITE] = temp;
        }


        void Update()
        {
            ApplyAdvection(sim_params.temperature_dissipation, 0.0f, temperature_grids);
            ApplyAdvection(sim_params.density_dissipation, 0.0f, density_grids);
            ApplyAdvectionVelocity();

            ApplyBuoyancy();
            ApplyImpulse(density_amount, density_grids);
            ApplyImpulse(temperature_amount, temperature_grids);

            CalculateDivergence();
            CalculatePressure();
            CalculateProjection();
        }


        private void ApplyAdvection(float _dissipation, float _decay, ComputeBuffer[] _grids,
            float _forward = 1.0f)
        {
            advect.SetVector("size", size);
            advect.SetFloat("dt", DT);
            advect.SetFloat("dissipation", _dissipation);
            advect.SetFloat("forward", _forward);
            advect.SetFloat("decay", _decay);

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
            buoyancy.SetBuffer(kernel_id, "write_R", velocity_grids[WRITE]);
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
            impulse.SetVector("source_pos", impulse_position);

            int kernel_id = impulse.FindKernel("Impulse");
            impulse.SetBuffer(kernel_id, "read_R", _grids[READ]);
            impulse.SetBuffer(kernel_id, "write_R", _grids[WRITE]);
            impulse.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
            Swap(_grids);
        }


        private void CalculateDivergence()
        {
            divergence.SetVector("size", size);
            int kernel_id = divergence.FindKernel("Divergence");
            divergence.SetBuffer(kernel_id, "write_RGB", temp_grid);
            divergence.SetBuffer(kernel_id, "velocity", velocity_grids[READ]);
            divergence.SetBuffer(kernel_id, "obstacles", obstacle_grid);
            divergence.Dispatch(kernel_id, x_thread_count, y_thread_count, z_thread_count);
        }


        private void CalculatePressure()
        {
            jacobi.SetVector("size", size);
            int kernel_id = divergence.FindKernel("Jacobi");
            jacobi.SetBuffer(kernel_id, "divergence", temp_grid);
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
            temp_grid.Release();
        }  
    }
}
