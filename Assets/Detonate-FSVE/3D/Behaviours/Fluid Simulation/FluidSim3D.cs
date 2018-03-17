using System.Collections.Generic;
using UnityEngine;


namespace Detonate
{
    public class FluidSim3D : MonoBehaviour
    {
        [SerializeField] FluidSim3DParams sim_params = new FluidSim3DParams();

        //Compute shader interfacing classes
        [SerializeField] AdvectModule3D advection_module = new AdvectModule3D();
        [SerializeField] DivergenceModule3D divergence_module = new DivergenceModule3D();
        [SerializeField] JacobiModule3D jacobi_module = new JacobiModule3D();
        [SerializeField] BuoyancyModule3D buoyancy_module = new BuoyancyModule3D();
        [SerializeField] ImpulseModule3D impulse_module = new ImpulseModule3D();
        [SerializeField] ProjectionModule3D projection_module = new ProjectionModule3D();
        [SerializeField] ObstacleModule3D obstacle_module = new ObstacleModule3D();

        enum GridType
        {
            DENSITY,
            OBSTACLE,
            TEMPERATURE,
            PRESSURE,
            VELOCITY
        }

        [SerializeField] OutputModule3D output_module = new OutputModule3D();
        [SerializeField] GridType grid_to_output = GridType.DENSITY;
        [SerializeField] VolumeRenderer output_renderer = null;


        [SerializeField] List<FluidEmitter> emitters = new List<FluidEmitter>();
        [SerializeField] List<SphereCollider> sphere_colliders = new List<SphereCollider>();


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

        [SerializeField] private bool draw_bounds = false;
        [SerializeField] private bool velocity_debug = false;
        [SerializeField] private uint velocity_debug_resolution = 10;
        [SerializeField] private float velocity_debug_colour_threshold = 0.5f;
        [SerializeField] private bool velocity_debug_normalise = false;


        private void Start()
        {
            InitSim();
        }


        public void ResetSim()
        {
            OnDestroy();//in case of reset
            InitSim();
        }


        private void InitSim()
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
            
            divergence_grid = new ComputeBuffer(buffer_size, sizeof(float));
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
            CreateObstacles();
            UpdateVolumeRenderer();
        }


        private void CreateObstacles()
        {
            obstacle_module.ClearObstacles(obstacle_grid);       
            SetBoundary();
            AddSphereObstacles();
        }


        private void AddSphereObstacles()
        {
            for (int i = 0; i < sphere_colliders.Count; ++i)
            {
                if (sphere_colliders[i] == null)
                {
                    sphere_colliders.RemoveAt(i);
                    continue;
                }


                if (!sphere_colliders[i].enabled)
                    continue;

                if (!sphere_colliders[i].gameObject.activeInHierarchy)
                    continue;


                obstacle_module.AddSphereObstacle(size, ConvertPositionToGridSpace(sphere_colliders[i].transform.position),
                    sphere_colliders[i].radius * sphere_colliders[i].transform.localScale.x, obstacle_grid, thread_count);//voxelise sphere to obstacle grid
            }
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
            ApplyEmitters();
        }


        private void ApplyBuoyancy()
        {
            buoyancy_module.ApplyBuoyancy(DT, size, sim_params.smoke_buoyancy, sim_params.smoke_weight, sim_params.ambient_temperature,
                velocity_grids, density_grids, temperature_grids, thread_count);
        }


        public List<FluidEmitter> Emitters
        {
            get
            {
                return emitters;
            }
            set
            {
                emitters = value;
            }
        }


        public List<SphereCollider> SphereColliders
        {
            get
            {
                return sphere_colliders;
            }
            set
            {
                sphere_colliders = value;
            }
        }


        private void ApplyEmitters()
        {
            for(int i = 0; i < emitters.Count; ++i)
            {
                if (emitters[i] == null)
                {
                    emitters.RemoveAt(i);
                    continue;
                }

                if (!emitters[i].isActiveAndEnabled)
                    continue;

                if (!emitters[i].gameObject.activeInHierarchy)
                    continue;

                if (!emitters[i].Emit)
                    continue;

                ApplyImpulse(emitters[i].DenisityAmount, emitters[i].EmissionRadius, density_grids, emitters[i].transform.position);
                ApplyImpulse(emitters[i].TemperatureAmount, emitters[i].EmissionRadius, temperature_grids, emitters[i].transform.position);
            }
        }


        private void MassConservationStage()
        {
            jacobi_module.CalculatePressure(size, divergence_grid, obstacle_grid,
                sim_params.jacobi_iterations, pressure_grids, thread_count);

            projection_module.CalculateProjection(size, pressure_grids,
                obstacle_grid, velocity_grids, thread_count);
        }


        private void UpdateVolumeRenderer()
        {
            if (output_renderer == null)
                return;

            ConvertGridToVolume(grid_to_output);
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x, transform.localScale.x);//scale must be uniform
            output_renderer.size = size;
            output_renderer.texture = volume_output;
        }


        private void ConvertGridToVolume(GridType _grid_type)
        {
            switch (_grid_type)
            {
                case GridType.DENSITY:
                    output_module.ConvertToVolume(size, density_grids[READ], volume_output, thread_count);
                    break;
                case GridType.OBSTACLE:
                    output_module.ConvertToVolume(size, obstacle_grid, volume_output, thread_count);
                    break;
                case GridType.TEMPERATURE:
                    output_module.ConvertToVolume(size, temperature_grids[READ], volume_output, thread_count);
                    break;
                case GridType.PRESSURE:
                    output_module.ConvertToVolume(size, pressure_grids[READ], volume_output, thread_count);
                    break;
                case GridType.VELOCITY:
                    output_module.ConvertToVolume(size, velocity_grids[READ], volume_output, thread_count);
                    break;
                default:
                    break;
            }
        }


        private void ApplyImpulse(float _amount, float _radius, ComputeBuffer[] _grids, Vector3 _position)
        {
            impulse_module.ApplyImpulse(DT, size, _amount, _radius,
                ConvertPositionToGridSpace(_position), _grids, thread_count);
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


        private void DrawVelocityField()
        {
            Vector3[] velocities = new Vector3[velocity_grids[READ].count];
            velocity_grids[READ].GetData(velocities);

            velocity_debug_resolution = (uint) Mathf.Max(5, velocity_debug_resolution);// should be 5 minimum

            for (uint x = 0; x < size.x; x += velocity_debug_resolution)
            {
                for (uint y = 0; y < size.y; y += velocity_debug_resolution)
                {
                    for (uint z = 0; z < size.z; z += velocity_debug_resolution)
                    {
                        int index = Mathf.FloorToInt(x + y * size.x + z * size.x * size.y);

                        if (index > size.x * size.y * size.z)
                            continue;

                        Vector3 grid_pos = new Vector3(x, y, z) * 0.1f;
                        grid_pos += transform.localPosition;

                        Vector3 velocity = velocity_debug_normalise ? velocities[index].normalized : velocities[index];


                        Gizmos.color = velocity.magnitude > velocity_debug_colour_threshold ? Color.red : Color.yellow;

                        Gizmos.DrawLine(grid_pos, grid_pos + (velocity * 0.1f));
                    }
                }
            }
        }


        private void OnDrawGizmos()
        {
            if (draw_bounds)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }

            if (velocity_debug && velocity_grids[READ] != null)
            {
                DrawVelocityField();
            }
        }
    }
}
