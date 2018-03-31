using System.Linq;
using UnityEngine;


namespace FSVE
{
    public class FluidExplosion3D : FluidSimulation3D
    { 
        [SerializeField] FluidExplosion3DParams explosion_params = new FluidExplosion3DParams();
        [SerializeField] FuelParticleSimulationModule fuel_particle_module = new FuelParticleSimulationModule();


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


        protected void Start()
        {
            InitSim();
        }


        public override void ResetSim()
        {
            base.ResetSim();
            OnDestroy();//in case of reset
            InitSim();
        }


        protected override void InitSim()
        {
            base.InitSim();
            CreateParticleBuffer();
            NoiseVelocityGrids();
        }


        private void NoiseVelocityGrids()
        {
            int buffer_size = sim_params.width * sim_params.height * sim_params.depth;
            Vector3[] noise = GetRandomVelocities(buffer_size);//get random velocities
            velocity_grids[READ].SetData(noise);//set velocites in compute buffer to random ones
            velocity_grids[WRITE].SetData(noise);       
        }


        private static Vector3[] GetRandomVelocities(int _buffer_size, float _scalar = 1)
        {
            Vector3[] velocities = new Vector3[_buffer_size];
            for(int i = 0; i < velocities.Length; ++i)
            {
                velocities[i] = RandomNormalisedVector() * _scalar;//scalar can be used to control magnitude
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
            return random_vector;//return random normalised vector
        }


        private void CreateParticleBuffer()
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
                Vector3 start_pos = explosion_params.fuse_transform == null ? transform.position : explosion_params.fuse_transform.position;
                
                if (i < soot_insert_index)//regular fuel particle init
                {
                    float random_radius = Random.Range(-explosion_params.fuse_radius, explosion_params.fuse_radius);//random radius within fuse radius
                    initial_fuel_particles[i].position = ConvertPositionToGridSpace(Random.insideUnitSphere * random_radius + start_pos);//random position in circle
                    initial_fuel_particles[i].mass = explosion_params.mass;
                }
                else//init soot particle
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
            CalculateDivergence();//i.e. fluid diffusion
            //MassConservationStage();
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


        private void ApplyThermoDynamics()// Probably not an accurate name but sounds cool ;)
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


        protected override void ConvertGridToVolume(GridType _grid_type)
        {
            if (_grid_type == GridType.DENSITY)
            {
                output_module.FuelParticleToVolume(size, fuel_particles_buffer,
                    volume_output, particle_count);
                return;
            }

            base.ConvertGridToVolume(_grid_type);
        }


        // All buffers should be released on destruction
        protected override void OnDestroy()
        {
            base.OnDestroy();
            fuel_particles_buffer.Release();
        }
       
    }
}
