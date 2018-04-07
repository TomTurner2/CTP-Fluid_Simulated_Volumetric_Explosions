using UnityEngine;


namespace FSVE
{
    public class FluidExplosion3D : FluidSimulation3D
    { 
        [SerializeField] FluidExplosion3DParams explosion_params = new FluidExplosion3DParams();
        [SerializeField] FuelParticleSimulationModule fuel_particle_module = new FuelParticleSimulationModule();
        [SerializeField] intVector3 output_resolution = new intVector3(128, 128, 128);

        private RenderTexture custom_volume_output = null;// This simulation can output a volume with a custom resolution
        private const int MAX_RESOLUTION = 512;

        struct FuelParticle
        {
            public Vector3 position;
            public Vector3 velocity;
            public float temperature;
            public float mass;
        }


        // Particle sim buffers
        private ComputeBuffer fuel_particles_buffer = null;
        private uint particle_count = 0;// Use this so count isn't changed at runtime


        protected void Start()
        {
            InitSim();
        }


        public override void ResetSim()
        {
            base.ResetSim();
            OnDestroy();// In case of reset
            InitSim();
        }


        protected override void InitSim()
        {
            base.InitSim();
            CreateParticleBuffer();
            NoiseVelocityGrids();
            ValidateCustomResolution();
            CreateCustomResolutionOutputTexture();
        }


        private void ValidateCustomResolution()
        {
            output_resolution.x = Mathf.ClosestPowerOfTwo(Mathf.Min(output_resolution.x, MAX_RESOLUTION));
            output_resolution.y = Mathf.ClosestPowerOfTwo(Mathf.Min(output_resolution.y, MAX_RESOLUTION));
            output_resolution.z = Mathf.ClosestPowerOfTwo(Mathf.Min(output_resolution.z, MAX_RESOLUTION));      
        }


        private void CreateCustomResolutionOutputTexture()
        {
            custom_volume_output = new RenderTexture(output_resolution.x, output_resolution.y, output_resolution.z)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,// Is a 3d texture
                volumeDepth = output_resolution.z,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true// Must be set before creation
            };

            custom_volume_output.Create();
        }


        private void NoiseVelocityGrids()
        {
            int buffer_size = sim_params.width * sim_params.height * sim_params.depth;
            Vector3[] noise = GetRandomVelocities(buffer_size, explosion_params.starting_noise);// Get random velocities
            velocity_grids[READ].SetData(noise);// Set velocites in compute buffer to random ones
            velocity_grids[WRITE].SetData(noise);       
        }


        private static Vector3[] GetRandomVelocities(int _buffer_size, float _scalar = 1)
        {
            Vector3[] velocities = new Vector3[_buffer_size];
            for(int i = 0; i < velocities.Length; ++i)
            {
                velocities[i] = RandomNormalisedVector() * _scalar;// Scalar can be used to control magnitude
            }

            return velocities;
        }


        private static Vector3 RandomNormalisedVector()
        {
            Vector3 random_vector = new Vector3
            {
                x = Random.Range(-1, 1),
                y = Random.Range(-1, 1),
                z = Random.Range(-1, 1)
            };
            return random_vector;// Return random normalised vector
        }


        private void CreateParticleBuffer()
        {
            particle_count = explosion_params.particle_count;
            const int float_count = 8;// Can't use sizeof for custom types in Unity -_-
            fuel_particles_buffer = new ComputeBuffer((int)particle_count, sizeof(float) * float_count, ComputeBufferType.Append);
            InitParticles();
        }


        private void InitParticles()
        {
            int soot_insert_index = Mathf.FloorToInt(particle_count * 0.5f);
            FuelParticle[] initial_fuel_particles = new FuelParticle[particle_count];

            for (int i = 0; i < particle_count; ++i)
            {
                Vector3 start_pos = explosion_params.fuse_transform == null ? transform.position : explosion_params.fuse_transform.position;
                
                if (i < soot_insert_index)// Regular fuel particle init
                {
                    float random_radius = Random.Range(-explosion_params.fuse_radius, explosion_params.fuse_radius);// Random radius within fuse radius
                    initial_fuel_particles[i].position = ConvertPositionToGridSpace(Random.insideUnitSphere * random_radius + start_pos);// Random position in circle
                    initial_fuel_particles[i].mass = explosion_params.mass;
                }
                else// Init soot particle
                {
                    initial_fuel_particles[i].position = ConvertPositionToGridSpace(start_pos);
                    initial_fuel_particles[i].mass = explosion_params.soot_mass;
                }
            }
            
            fuel_particles_buffer.SetData(initial_fuel_particles);
        }


        protected override void Update()
        {
            base.Update();
            FluidSimulationUpdate();
            ParticleSimulationUpdate();// Before mass conservation so it can effect divergence
            MassConservationStage();        
            UpdateVolumeRenderer();
        }


        private void FluidSimulationUpdate()
        {
            MoveStage();
            AddForcesStage();
            CalculateDivergence();// Fluid diffusion
        }


        private void ParticleSimulationUpdate()
        {
            CalculateParticleVelocity();
            UpdateParticlePhysics();
            ApplyThermoDynamics();
        }


        private void CalculateParticleVelocity()
        {
            fuel_particle_module.UpdateParticleVelocity(fuel_particles_buffer, temperature_grids[READ],
                velocity_grids[READ], explosion_params.particle_drag, explosion_params.particle_radius,
                explosion_params.thermal_mass, particle_count, sim_dt, size);
        }


        private void UpdateParticlePhysics()
        {
            fuel_particle_module.UpdateParticlePositions(fuel_particles_buffer, particle_count, sim_dt);
        }


        private void ApplyThermoDynamics()// Probably not an entirely accurate name but sounds cool ;)
        {
            fuel_particle_module.BurnParticles(fuel_particles_buffer, temperature_grids[READ],
                divergence_grid, explosion_params.burn_rate, explosion_params.heat_emission,
                explosion_params.burn_threshold, explosion_params.divergence_effect,
                particle_count, sim_dt, size);
        }


        private void AddForcesStage()
        {
            ApplyBuoyancy();// For now only buoyancy is applied
        }


        private void ApplyBuoyancy()
        {
            buoyancy_module.ApplyBuoyancySimple(sim_dt, size, explosion_params.fluid_buoyancy,
                explosion_params.fluid_weight, sim_params.ambient_temperature,
                velocity_grids, temperature_grids, thread_count);
        }


        protected override RenderTexture ConvertGridToVolume(GridType _grid_type)
        {
            if (_grid_type != GridType.DENSITY)
                return base.ConvertGridToVolume(_grid_type); // Let base handle other output conversions

            intVector3 custom_thread_count = CustomOutputThreadCount();
            output_module.FuelParticleToVolume(output_resolution, fuel_particles_buffer,
                custom_volume_output, particle_count, explosion_params.trace_particles, custom_thread_count);

            return custom_volume_output;
        }


        private intVector3 CustomOutputThreadCount()
        {
            intVector3 custom_thread_count = intVector3.Zero;
            custom_thread_count.x = (int)(output_resolution.x / THREAD_GROUP_COUNT);// Compute shaders use thread groups 
            custom_thread_count.y = (int)(output_resolution.y / THREAD_GROUP_COUNT);// Divide by the amount of groups to get required thread count for grid size
            custom_thread_count.z = (int)(output_resolution.z / THREAD_GROUP_COUNT);

            return custom_thread_count;
        }


        // All buffers should be released on destruction
        protected override void OnDestroy()
        {
            base.OnDestroy();
            fuel_particles_buffer.Release();
        }
       
    }
}
