using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Detonate
{
    public class FluidExplosion3D : MonoBehaviour
    {
        [SerializeField] FluidSim3DParams sim_params = new FluidSim3DParams();
        [SerializeField] FluidExplosion3DParams explosion_params = new FluidExplosion3DParams();

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

        //output
        [SerializeField] OutputModule3D output_module = new OutputModule3D();
        [SerializeField] GridType grid_to_output = GridType.DENSITY;
        [SerializeField] VolumeRenderer output_renderer = null;
        private RenderTexture volume_output;

        //fluid sim buffers
        private ComputeBuffer[] velocity_grids = new ComputeBuffer[2];
        private ComputeBuffer[] temperature_grids = new ComputeBuffer[2];
        private ComputeBuffer[] pressure_grids = new ComputeBuffer[2];
        private ComputeBuffer divergence_grid;
        private ComputeBuffer obstacle_grid;


        struct FuelParticle
        {
            public Vector3 position;
            public Vector3 velocity;
            public float temperature;
            public float mass;
            public float soot_accumulation;
        }


        //particle sim buffers
        private ComputeBuffer fuel_particles_buffer = null;
        private uint particle_count = 0;//use this so count isn't changed at runtime


        private Vector3 size = Vector3.zero;
        private intVector3 thread_count = intVector3.Zero;

        private const uint READ = 0; //for accessing grid sets
        private const uint WRITE = 1;
        private const uint THREAD_COUNT = 8; //threads used by compute shader
        private const float DT = 0.1f;//simulation blows up with large time steps?

        [SerializeField] private bool draw_bounds = false;


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
            CreateParticleBuffers();
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

            CreateVelocityGrids(buffer_size);
            CreateTemperatureGrids(buffer_size);
            CreatePressureGrids(buffer_size);
            
            divergence_grid = new ComputeBuffer(buffer_size, sizeof(float) * 3);
            obstacle_grid = new ComputeBuffer(buffer_size, sizeof(float));
        }


        private void CreateVelocityGrids(int _buffer_size)
        {
            velocity_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float) * 3);//will store float 3
            velocity_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float) * 3);

            Vector3[] noise = GenerateNoiseArray();
            velocity_grids[READ].SetData(noise);
            velocity_grids[WRITE].SetData(noise);       
        }


        private Vector3[] GenerateNoiseArray()
        {
            Vector3[] noise = new Vector3[(int)(size.x * size.y * size.z)];

            for (int y = 0; y < size.y; ++y)
            {
                for (int x = 0; x < size.x; ++x)
                {
                    for (int z = 0; z < size.z; ++z)
                    {
                        float noise_value = PerlinNoise3D(x, y, z);

                        int index = (int)(x + y * size.x + z * size.x * size.y);
                        noise[index].x = noise_value;
                        noise[index].y = noise_value;
                        noise[index].z = noise_value;
                    }
                }
            }

            return noise;
        }


        private float PerlinNoise3D(int _x, int _y, int _z)
        {
            float ab = Mathf.PerlinNoise(_x, _y);//get permutations
            float bc = Mathf.PerlinNoise(_y, _z);
            float ac = Mathf.PerlinNoise(_x, _z);

            float ba = Mathf.PerlinNoise(_y, _x);//get reverse permutation
            float cb = Mathf.PerlinNoise(_z, _y);
            float ca = Mathf.PerlinNoise(_z, _x);

            return (ab + bc + ac + ba + cb + ca) / 6.0f;//return average
        }


        private void CreateTemperatureGrids(int _buffer_size)
        {
            temperature_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            temperature_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));

            temperature_grids[READ].SetData(Enumerable.Repeat(sim_params.ambient_temperature,
                temperature_grids[READ].count).ToArray());// set all values to ambient temperature

            temperature_grids[WRITE].SetData(Enumerable.Repeat(sim_params.ambient_temperature,
                temperature_grids[WRITE].count).ToArray());// set all values to ambient temperature
        }


        private void CreatePressureGrids(int _buffer_size)
        {
            pressure_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            pressure_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));
        }


        private void CreateParticleBuffers()
        {
            particle_count = explosion_params.particle_count;
            const int float_count = 9;//Can't use sizeof for custom types in Unity -_-
            fuel_particles_buffer = new ComputeBuffer((int)particle_count, sizeof(float) * float_count, ComputeBufferType.Append);
            InitParticles();
        }


        private void InitParticles()
        {
            int soot_insert_index = Mathf.FloorToInt(particle_count * 0.5f);
            FuelParticle[] initial_fuel_particles = new FuelParticle[particle_count];

            for (int i = 0; i < particle_count; ++i)
            {
                if (i < soot_insert_index)//regular fuel particle init
                {
                    float random_radius = Random.Range(-explosion_params.fuse_radius, explosion_params.fuse_radius);//random radius within fuse radius
                    initial_fuel_particles[i].position = Random.insideUnitSphere * random_radius + explosion_params.fuse_position;//random position in circle
                    initial_fuel_particles[i].mass = explosion_params.mass;
                }
                else//init soot particle
                {
                    initial_fuel_particles[i].position = Vector3.zero;
                    initial_fuel_particles[i].mass = explosion_params.soot_mass;
                }
            }
            
            fuel_particles_buffer.SetData(initial_fuel_particles);
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
            UpdateVolumeRenderer();
        }


        private void MoveStage()
        {
            advection_module.ApplyAdvection(DT, size, sim_params.temperature_dissipation,
                temperature_grids, velocity_grids, obstacle_grid, thread_count);

            advection_module.ApplyAdvectionVelocity(DT, size, sim_params.velocity_dissipation,
                velocity_grids, obstacle_grid, thread_count);
        }


        private void AddForcesStage()
        {
            ApplyBuoyancy();
        }


        private void ApplyBuoyancy()
        {
            buoyancy_module.ApplyBuoyancySimple(DT, size, sim_params.smoke_buoyancy, sim_params.smoke_weight, sim_params.ambient_temperature,
                velocity_grids, temperature_grids, thread_count);
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
            output_renderer.size = size;
            output_renderer.texture = volume_output;
        }


        private void ConvertGridToVolume(GridType _grid_type)
        {
            switch (_grid_type)
            {
                case GridType.DENSITY:
                   //output_module.ConvertToVolume(size, density_grids[READ], volume_output, thread_count);
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
            ReleaseFluidSimBuffers();
            fuel_particles_buffer.Release();
        }


        private void ReleaseFluidSimBuffers()
        {
            //density_grids[READ].Release();
            //density_grids[WRITE].Release();

            velocity_grids[READ].Release();
            velocity_grids[WRITE].Release();

            temperature_grids[READ].Release();
            temperature_grids[WRITE].Release();

            pressure_grids[READ].Release();
            pressure_grids[WRITE].Release();

            obstacle_grid.Release();
            divergence_grid.Release();
        }


        public bool DrawBounds
        {
            get
            {
                return draw_bounds;
            }
            set
            {
                draw_bounds = value;
            }
        }


        private void OnDrawGizmos()
        {
            DrawBoundGizmos();
            DrawFuelParticlesGizmos();
        }


        private void DrawBoundGizmos()
        {
            if (!draw_bounds)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }


        private void DrawFuelParticlesGizmos()
        {
            if (fuel_particles_buffer == null)
                return;
            FuelParticle[] particles = new FuelParticle[fuel_particles_buffer.count];
            fuel_particles_buffer.GetData(particles);

            int i = 0;
            foreach (FuelParticle particle in particles)
            {
                ++i;
                Gizmos.color = Color.gray;

                if (i <= particle_count * 0.5f)
                    Gizmos.color = Color.red;

                Gizmos.DrawSphere(transform.localPosition + particle.position, explosion_params.particle_radius);
            }
        }
       
    }
}
