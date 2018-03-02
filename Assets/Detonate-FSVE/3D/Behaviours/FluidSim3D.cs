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
        [SerializeField] float temperature_amount = 10.0f;

        //Compute shader interfacing classes
        [Space]
        [Header("GPU Modules")]
        [SerializeField] AdvectModule3D advection_module = new AdvectModule3D();
        [SerializeField] DivergenceModule3D divergence_module = new DivergenceModule3D();
        [SerializeField] JacobiModule3D jacobi_module = new JacobiModule3D();
        [SerializeField] BuoyancyModule3D buoyancy_module = new BuoyancyModule3D();
        [SerializeField] ImpulseModule3D impulse_module = new ImpulseModule3D();
        [SerializeField] ProjectionModule3D projection_module = new ProjectionModule3D();
        [SerializeField] ObstacleModule3D obstacle_module = new ObstacleModule3D();
        [SerializeField] OutputModule3D output_module = new OutputModule3D();
        [Space]
        [SerializeField] VolumeRenderer output_renderer = null;


        private RenderTexture volume_output;
        private ComputeBuffer[] density_grids = new ComputeBuffer[2];
        private ComputeBuffer[] velocity_grids = new ComputeBuffer[2];
        private ComputeBuffer[] temperature_grids = new ComputeBuffer[2];
        private ComputeBuffer[] pressure_grids = new ComputeBuffer[2];
        private ComputeBuffer divergence_grid;
        private ComputeBuffer obstacle_grid;

        private Vector3 size = Vector3.zero;
        private intVector3 thread_count = intVector3.Zero;

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
            thread_count.x = (int)(sim_params.width / THREAD_COUNT);
            thread_count.y = (int)(sim_params.height / THREAD_COUNT);
            thread_count.z = (int)(sim_params.depth / THREAD_COUNT);
        }


        private void CreateOutputTexture()
        {   
            volume_output = new RenderTexture(sim_params.width, sim_params.height, sim_params.depth)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                volumeDepth = sim_params.depth,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true//must be set before creation
            };
 
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
            obstacle_module.SetBoundary(size, obstacle_grid, thread_count);
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
            advection_module.ApplyAdvection(DT, size, sim_params.temperature_dissipation,
                temperature_grids, velocity_grids, obstacle_grid, thread_count);

            advection_module.ApplyAdvection(DT, size, sim_params.density_dissipation,
                density_grids, velocity_grids, obstacle_grid, thread_count);

            advection_module.ApplyAdvectionVelocity(DT, size, sim_params.velocity_dissipation,
                velocity_grids, obstacle_grid, thread_count);
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

            output_module.ConvertToVolume(size, density_grids, volume_output, thread_count);
            output_renderer.size = size;
            output_renderer.texture = volume_output;
        }


        private void ApplyBuoyancy()
        {
            buoyancy_module.ApplyBuoyancy(DT, size, sim_params.smoke_buoyancy, sim_params.smoke_weight, sim_params.ambient_temperature,
                velocity_grids, density_grids, temperature_grids, thread_count);
        }


        private void ApplyImpulse(float _amount, ComputeBuffer[] _grids)
        {
            if (impulse_target_transform != null)
            {
                impulse_position = ConvertPositionToGridSpace(impulse_target_transform.position);//use transform target as source position
            }

            impulse_module.ApplyImpulse(DT, size, _amount, impulse_radius, impulse_position, _grids, thread_count);
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
            divergence_module.CalculateDivergence(size, divergence_grid, velocity_grids, obstacle_grid, thread_count);
        }


        private void CalculatePressure()
        {
            jacobi_module.CalculatePressure(size, divergence_grid, obstacle_grid,
                sim_params.jacobi_iterations, pressure_grids, thread_count);
        }


        private void CalculateProjection()
        {
           projection_module.CalculateProjection(size, pressure_grids, obstacle_grid, velocity_grids, thread_count);
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
            if (!draw_bounds)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }

    }
}
