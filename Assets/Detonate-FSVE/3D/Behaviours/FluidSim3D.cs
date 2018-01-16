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


        void SetBoundary()
        {
            
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

        }

        private void ApplyAdvectionVelocity()
        {
        }


        private void ApplyBuoyancy()
        {
        }


        private void ApplyImpulse(float _amount, ComputeBuffer[] _grids)
        {
        }


        private void CalculateDivergence()
        {
        }


        private void CalculatePressure()
        {           
        }


        private void CalculateProjection()
        {     
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
