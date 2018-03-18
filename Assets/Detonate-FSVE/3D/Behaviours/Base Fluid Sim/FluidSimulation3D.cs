using System.Collections.Generic;
using UnityEngine;


namespace Detonate
{
    abstract public class FluidSimulation3D : MonoBehaviour
    {
        [SerializeField] protected FluidSim3DParams sim_params = new FluidSim3DParams();

        //base modules all simulations will use (base requirements for a fluid sim)
        [SerializeField] protected AdvectModule3D advection_module = new AdvectModule3D();
        [SerializeField] protected DivergenceModule3D divergence_module = new DivergenceModule3D();
        [SerializeField] protected JacobiModule3D jacobi_module = new JacobiModule3D();
        [SerializeField] protected ImpulseModule3D impulse_module = new ImpulseModule3D();
        [SerializeField] protected ProjectionModule3D projection_module = new ProjectionModule3D();
        [SerializeField] protected ObstacleModule3D obstacle_module = new ObstacleModule3D();

        protected enum GridType
        {
            DENSITY,
            OBSTACLE,
            TEMPERATURE,
            PRESSURE,
            VELOCITY
        }

        [SerializeField] protected OutputModule3D output_module = new OutputModule3D();
        [SerializeField] protected VolumeRenderer output_renderer = null;
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

        protected const uint READ = 0;//for accessing grid sets
        protected const uint WRITE = 1;
        protected const uint THREAD_GROUP_COUNT = 8;//threads used by compute shader
        protected float sim_dt = 0.1f;//simulation blows up with large time steps?

        [SerializeField] private bool draw_bounds = false;
        [SerializeField] private bool velocity_debug = false;
        [SerializeField] private uint velocity_debug_resolution = 10;
        [SerializeField] private float velocity_debug_colour_threshold = 0.5f;
        [SerializeField] private bool velocity_debug_normalise = false;


        protected virtual void Start()
        {
            InitSim();
        }


        protected virtual void InitSim()
        {
            CalculateSize();
            CalculateThreadCount();
            CreateGridSets();
            SetBoundary();
            CreateOutputTexture();
        }


        public virtual void ResetSim()
        {
            OnDestroy();//in case of reset
            InitSim();
        }


        protected void CalculateSize()
        {
            ValidateDimensions();
            size = new Vector3(sim_params.width, sim_params.height,
                sim_params.depth);//record size so it can't be changed at runtime
        }


        protected void ValidateDimensions()
        {
            sim_params.width = Mathf.ClosestPowerOfTwo(sim_params.width);//power of two optimal for memory storage
            sim_params.height = Mathf.ClosestPowerOfTwo(sim_params.height);
            sim_params.depth = Mathf.ClosestPowerOfTwo(sim_params.depth);
        }


        protected void CalculateThreadCount()
        {
            thread_count.x = (int)(sim_params.width / THREAD_GROUP_COUNT);//compute shaders use thread groups 
            thread_count.y = (int)(sim_params.height / THREAD_GROUP_COUNT);//divide by the amount of groups to get required thread count for grid size
            thread_count.z = (int)(sim_params.depth / THREAD_GROUP_COUNT);
        }


        protected void CreateOutputTexture()
        {
            volume_output = new RenderTexture(sim_params.width, sim_params.height, sim_params.depth)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,//is a 3d texture
                volumeDepth = sim_params.depth,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true//must be set before creation
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
            velocity_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float) * 3);//will store float 3
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
            obstacle_module.SetBoundary(size, obstacle_grid, thread_count);//sets solid edges in the sim bounds
        }


        protected virtual void Update()
        {
            if (sim_params.dynamic_time_step)
            {
                sim_dt = Time.deltaTime * sim_params.simulation_speed;
            }
            else
            {   
                sim_dt = sim_params.fixed_time_step;
            }
        }

        
        protected virtual void MoveStage()
        {
            advection_module.ApplyAdvection(sim_dt, size, sim_params.temperature_dissipation,
                temperature_grids, velocity_grids, obstacle_grid, thread_count);//move temperature according to velocity

            advection_module.ApplyAdvectionVelocity(sim_dt, size, sim_params.velocity_dissipation,
                velocity_grids, obstacle_grid, thread_count);//move velocity according to velocity
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
            SetBoundary();//add boundary back in
            AddSphereObstacles();//add dynamic colliders on top
        }


        private void AddSphereObstacles()
        {
            for (int i = 0; i < sphere_colliders.Count; ++i)
            {
                if (sphere_colliders[i] == null)
                {
                    sphere_colliders.RemoveAt(i);
                    continue;//skip null
                }

                if (!sphere_colliders[i].enabled)//only add enabled colliders
                    continue;

                if (!sphere_colliders[i].gameObject.activeInHierarchy)//only add if an active game object
                    continue;

                obstacle_module.AddSphereObstacle(size, ConvertPositionToGridSpace(sphere_colliders[i].transform.position),
                    sphere_colliders[i].radius * sphere_colliders[i].transform.localScale.x, obstacle_grid, thread_count);//voxelise sphere to obstacle grid
            }
        }


        protected void MassConservationStage()
        {
            jacobi_module.CalculatePressure(size, divergence_grid, obstacle_grid,
                sim_params.jacobi_iterations, pressure_grids, thread_count);//pressure relax gradiant

            projection_module.CalculateProjection(size, pressure_grids,
                obstacle_grid, velocity_grids, thread_count);//subtract gradiant (Hodge Decomposition)
        }


        protected void UpdateVolumeRenderer()
        {
            if (output_renderer == null)
                return;

            ConvertGridToVolume(grid_to_output);
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x, transform.localScale.x);//scale must be uniform
            output_renderer.size = size;
            output_renderer.texture = volume_output;
        }


        protected virtual void ConvertGridToVolume(GridType _grid_type)
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
        }


        protected void ApplyImpulse(float _amount, float _radius, ComputeBuffer[] _grids, Vector3 _position)
        {
            impulse_module.ApplyImpulse(sim_dt, size, _amount, _radius,
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
                scale_convert.y / transform.localScale.y, scale_convert.z / transform.localScale.z);//TODO should I do grid scaling here? or keep it in compute?
        }


        protected void CalculateDivergence()
        {
            divergence_module.CalculateDivergence(size, divergence_grid,
                velocity_grids, obstacle_grid, thread_count);
        }


        protected virtual void OnDestroy()
        {
            velocity_grids[READ].Release();//release memory for all base grids
            velocity_grids[WRITE].Release();

            temperature_grids[READ].Release();
            temperature_grids[WRITE].Release();

            pressure_grids[READ].Release();
            pressure_grids[WRITE].Release();

            obstacle_grid.Release();
            divergence_grid.Release();
        }


        protected void DrawVelocityField()
        {
            Vector3[] velocities = new Vector3[velocity_grids[READ].count];
            velocity_grids[READ].GetData(velocities);

            velocity_debug_resolution = (uint)Mathf.Max(5, velocity_debug_resolution);//should be 5 minimum

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
                        Vector3 grid_pos = new Vector3(x, y, z) * debug_scale;//scale it down so its not enormous
                        grid_pos += transform.localPosition;

                        Vector3 velocity = velocity_debug_normalise ? velocities[index].normalized : velocities[index];


                        Gizmos.color = velocity.magnitude > velocity_debug_colour_threshold ? Color.red : Color.yellow;

                        Gizmos.DrawLine(grid_pos, grid_pos + (velocity * 0.1f));
                    }
                }
            }
        }


        protected void OnDrawGizmos()
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
