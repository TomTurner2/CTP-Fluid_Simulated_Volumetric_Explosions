using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace FSVE
{
    abstract public class FluidSimulation3D : MonoBehaviour
    {
        private Transform simulation_transform = null;
        private UnityEvent sim_transform_change_event = new UnityEvent();

        [SerializeField] protected FluidSim3DParams sim_params = new FluidSim3DParams();

        // Base modules all simulations will use (base requirements for a fluid sim)
        [SerializeField] protected AdvectModule3D advection_module = new AdvectModule3D();
        [SerializeField] protected DivergenceModule3D divergence_module = new DivergenceModule3D();
        [SerializeField] protected JacobiModule3D jacobi_module = new JacobiModule3D();
        [SerializeField] protected ImpulseModule3D impulse_module = new ImpulseModule3D();
        [SerializeField] protected ProjectionModule3D projection_module = new ProjectionModule3D();
        [SerializeField] protected ObstacleModule3D obstacle_module = new ObstacleModule3D();
        [SerializeField] protected BuoyancyModule3D buoyancy_module = new BuoyancyModule3D();// Not technicaly a fluid requirement but most sims will have buoyancy

        protected enum GridType
        {
            DENSITY,// Should probably rename to CUSTOM
            OBSTACLE,
            TEMPERATURE,
            PRESSURE,
            VELOCITY
        }

        [SerializeField] protected OutputModule3D output_module = new OutputModule3D();
        [SerializeField] protected List<VolumeRenderer> output_renderers = new List<VolumeRenderer>();
        [SerializeField] GridType grid_to_output = GridType.DENSITY;
        [SerializeField] private List<SphereCollider> sphere_colliders = new List<SphereCollider>();

        protected RenderTexture volume_output;
        protected ComputeBuffer[] velocity_grids = new ComputeBuffer[2];
        protected ComputeBuffer[] temperature_grids = new ComputeBuffer[2];
        protected ComputeBuffer[] pressure_grids = new ComputeBuffer[2];
        protected ComputeBuffer divergence_grid;
        protected ComputeBuffer obstacle_grid;

        protected Vector3 size = Vector3.zero;
        protected intVector3 thread_count = intVector3.Zero;

        protected const uint READ = 0;// For accessing grid sets
        protected const uint WRITE = 1;
        protected const uint THREAD_GROUP_COUNT = 8;// Threads used by compute shader
        protected float sim_dt = 0.1f;// Update step, can be either dynamic or fixed

        // Debug
        [SerializeField] private bool draw_bounds = false;
        [SerializeField] private bool velocity_debug = false;
        [SerializeField] private uint velocity_debug_resolution = 10;
        [SerializeField] private float velocity_debug_colour_threshold = 0.5f;
        [SerializeField] private bool velocity_debug_normalise = false;


        protected virtual void InitSim()
        {
            SetSimulationTransform();
            CalculateSize();
            CalculateThreadCount();
            CreateGridSets();
            if (sim_params.simulation_bounds)
                SetBoundary();
            CreateOutputTexture();
        }


        public Transform SimulationTransform
        {
            set
            {
                simulation_transform = value;
                if (sim_transform_change_event != null)
                    sim_transform_change_event.Invoke();// Notify listeners
            }
            get { return simulation_transform; }
        }


        public UnityEvent SimTransformChange
        {
            get { return sim_transform_change_event; }
        }


        public void SetSimulationTransform()
        {
            simulation_transform = transform;
        }


        public virtual void ResetSim()
        {
            OnDestroy();// In case of reset
            InitSim();
        }


        protected void CalculateSize()
        {
            ValidateDimensions();
            size = new Vector3(sim_params.width, sim_params.height,
                sim_params.depth);// Record size so it can't be changed at runtime
        }


        protected void ValidateDimensions()
        {
            sim_params.width = Mathf.ClosestPowerOfTwo(sim_params.width);// Power of two optimal for memory storage
            sim_params.height = Mathf.ClosestPowerOfTwo(sim_params.height);
            sim_params.depth = Mathf.ClosestPowerOfTwo(sim_params.depth);
        }


        protected void CalculateThreadCount()
        {
            thread_count.x = (int)(sim_params.width / THREAD_GROUP_COUNT);// Compute shaders use thread groups 
            thread_count.y = (int)(sim_params.height / THREAD_GROUP_COUNT);// Divide by the amount of groups to get required thread count for grid size
            thread_count.z = (int)(sim_params.depth / THREAD_GROUP_COUNT);
        }


        protected void CreateOutputTexture()
        {
            volume_output = new RenderTexture(sim_params.width, sim_params.height, sim_params.depth)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,// Is a 3d texture
                volumeDepth = sim_params.depth,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true// Must be set before creation
            };

            volume_output.Create();
        }


        protected void CreateGridSets()
        {
            int buffer_size = sim_params.width * sim_params.height * sim_params.depth;

            CreateVelocityGrids(buffer_size);
            CreateTemperatureGrids(buffer_size);
            CreatePressureGrids(buffer_size);

            divergence_grid = new ComputeBuffer(buffer_size, sizeof(float));
            obstacle_grid = new ComputeBuffer(buffer_size, sizeof(float));
        }


        protected void CreateVelocityGrids(int _buffer_size)
        {
            velocity_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float) * 3);// Will store float3
            velocity_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float) * 3);
        }


        protected void CreateTemperatureGrids(int _buffer_size)
        {
            temperature_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            temperature_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));
        }


        protected void CreatePressureGrids(int _buffer_size)
        {
            pressure_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            pressure_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));
        }


        protected void SetBoundary()
        {
            obstacle_module.SetBoundary(size, obstacle_grid, thread_count);// Sets solid edges in the sim bounds
        }


        protected virtual void Update()
        {
            sim_dt = sim_params.dynamic_time_step ?
                Time.deltaTime * sim_params.simulation_speed : sim_params.fixed_time_step;// If dynamic use dt else use fixed step

            CreateObstacles();
        }

        
        protected virtual void MoveStage()
        {
            advection_module.ApplyAdvection(sim_dt, size, sim_params.temperature_dissipation,
                temperature_grids, velocity_grids, obstacle_grid, thread_count);// Move temperature according to velocity

            advection_module.ApplyAdvectionVelocity(sim_dt, size, sim_params.velocity_dissipation,
                velocity_grids, obstacle_grid, thread_count);// Move velocity according to velocity
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


        protected void CreateObstacles()
        {
            obstacle_module.ClearObstacles(obstacle_grid);
            if (sim_params.simulation_bounds)
                SetBoundary();// Add boundary back in
            AddSphereObstacles();// Add dynamic colliders on top
        }


        private void AddSphereObstacles()
        {
            for (int i = 0; i < sphere_colliders.Count; ++i)
            {
                if (sphere_colliders[i] == null)
                {
                    sphere_colliders.RemoveAt(i);
                    continue;// Skip null
                }

                if (!sphere_colliders[i].enabled)// Only add enabled colliders
                    continue;

                if (!sphere_colliders[i].gameObject.activeInHierarchy)// Only add if an active game object
                    continue;


                bool is_container = CheckContainer(sphere_colliders[i]);// Check if its marked as a container
                float scale_conversion = sphere_colliders[i].radius * sphere_colliders[i].transform.localScale.x;// Scale radius according to object scale
                Vector3 position_conversion = ConvertPositionToGridSpace(sphere_colliders[i].transform.position);// Convert it into simulation space

                obstacle_module.AddSphereObstacle(size, position_conversion, scale_conversion,
                    is_container, obstacle_grid, thread_count);// Voxelise sphere to obstacle grid
            }
        }


        public bool CheckContainer(SphereCollider _collider)
        {
            FluidContainer container = _collider.GetComponent<FluidContainer>();
            return container != null && container.isActiveAndEnabled;
        }


        protected void MassConservationStage()
        {
            jacobi_module.CalculatePressure(size, divergence_grid, obstacle_grid,
                sim_params.jacobi_iterations, pressure_grids, thread_count);// Pressure relax gradient

            projection_module.CalculateProjection(size, pressure_grids,
                obstacle_grid, velocity_grids, thread_count);// Subtract gradient (Hodge Decomposition) mass conserving field = any velocity field - gradient field
        }


        protected void UpdateVolumeRenderer()
        {
            if (output_renderers == null)
                return;

            if (output_renderers.Count <= 0)
                return;

            RenderTexture output = ConvertGridToVolume(grid_to_output);// This could be overriden to return a different render tex
            simulation_transform.localScale = new Vector3(simulation_transform.localScale.x,
                simulation_transform.localScale.x, simulation_transform.localScale.x);// Scale must be uniform

            foreach (VolumeRenderer output_renderer in output_renderers)
            {
                if (output_renderer == null)
                    continue;

                output_renderer.size = size;
                output_renderer.texture = output;
            }
        }


        protected virtual RenderTexture ConvertGridToVolume(GridType _grid_type)
        {
            switch (_grid_type)
            {
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

            return volume_output;
        }


        protected void ApplyImpulse(float _amount, float _radius, ComputeBuffer[] _grids, Vector3 _position)
        {
            impulse_module.ApplyImpulse(sim_dt, size, _amount, _radius,
                ConvertPositionToGridSpace(_position), _grids, thread_count);
        }


        public Vector3 ConvertPositionToGridSpace(Vector3 _pos)
        {
            return ConvertToGridScale(_pos - simulation_transform.localPosition);// Get relative position then factor in scale
        }


        private Vector3 ConvertToGridScale(Vector3 _pos)
        {
            Vector3 scale_convert = (_pos + simulation_transform.localScale * 0.5f);

            return new Vector3(scale_convert.x / simulation_transform.localScale.x,
                scale_convert.y / simulation_transform.localScale.y, scale_convert.z /
                simulation_transform.localScale.z);// TODO should I do grid scaling here? or keep it in compute?
        }


        protected void CalculateDivergence()
        {
            divergence_module.CalculateDivergence(size, divergence_grid,
                velocity_grids, obstacle_grid, thread_count);
        }


        protected virtual void OnDestroy()
        {
            velocity_grids[READ].Release();// Release memory for all base grids
            velocity_grids[WRITE].Release();

            temperature_grids[READ].Release();
            temperature_grids[WRITE].Release();

            pressure_grids[READ].Release();
            pressure_grids[WRITE].Release();

            obstacle_grid.Release();
            divergence_grid.Release();
        }


        // For Debug
        protected void DrawVelocityField()
        {
            Vector3[] velocities = new Vector3[velocity_grids[READ].count];
            velocity_grids[READ].GetData(velocities);
            velocity_debug_resolution = (uint)Mathf.Max(5, velocity_debug_resolution);// Should be 5 minimum

            for (uint x = 0; x < size.x; x += velocity_debug_resolution)
            {
                for (uint y = 0; y < size.y; y += velocity_debug_resolution)
                {
                    for (uint z = 0; z < size.z; z += velocity_debug_resolution)
                    {
                        int index = Mathf.FloorToInt(x + y * size.x + z * size.x * size.y);

                        if (index > size.x * size.y * size.z)
                            continue;

                        const float debug_scale = 0.1f;
                        Vector3 grid_pos = new Vector3(x, y, z) * debug_scale;// Scale it down so its not enormous
                        grid_pos += transform.localPosition;

                        Vector3 velocity = velocity_debug_normalise ? velocities[index].normalized : velocities[index];// Determine normalised
                        Gizmos.color = velocity.magnitude > velocity_debug_colour_threshold ? Color.red : Color.yellow;// Determine colour
                        Gizmos.DrawLine(grid_pos, grid_pos + (velocity * debug_scale));// Draw velocity vector
                    }
                }
            }
        }


        protected virtual void OnDrawGizmos()
        {
            if (draw_bounds)
            {
                Gizmos.color = Color.cyan;
                if (simulation_transform != null)
                {
                    Gizmos.DrawWireCube(simulation_transform.position, simulation_transform.localScale);
                }
                else
                {
                    Gizmos.DrawWireCube(transform.position, transform.localScale);
                }
            }

            if (velocity_debug && velocity_grids[READ] != null)
            {
                DrawVelocityField();
            }
        }
    }
}
