using System.Linq;
using UnityEngine;


namespace Detonate
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
                if (i < soot_insert_index)//regular fuel particle init
                {
                    float random_radius = Random.Range(-explosion_params.fuse_radius, explosion_params.fuse_radius);//random radius within fuse radius
                    initial_fuel_particles[i].position = Random.insideUnitSphere * random_radius + explosion_params.fuse_position;//random position in circle
                    initial_fuel_particles[i].mass = explosion_params.mass;
                    //initial_fuel_particles[i].velocity = Vector3.up;//test
                }
                else//init soot particle
                {
                    initial_fuel_particles[i].position = Vector3.zero;
                    initial_fuel_particles[i].mass = explosion_params.soot_mass;
                }
            }
            
            fuel_particles_buffer.SetData(initial_fuel_particles);
        }


        protected override void Update()
        {
            base.Update();

            FluidSimulationUpdate();
            ParticleSimulationUpdate();
            UpdateVolumeRenderer();
        }


        private void FluidSimulationUpdate()
        {
            MoveStage();
            AddForcesStage();
            CalculateDivergence();//i.e. fluid diffusion
            MassConservationStage();
        }


        private void ParticleSimulationUpdate()
        {
            fuel_particle_module.UpdateParticleVelocity(fuel_particles_buffer, temperature_grids[READ], velocity_grids[READ],
                explosion_params.particle_drag, explosion_params.particle_radius, explosion_params.thermal_mass, particle_count, sim_dt, size);
            fuel_particle_module.UpdateParticlePositions(fuel_particles_buffer, particle_count, sim_dt);
        }


        private void AddForcesStage()
        {
            ApplyBuoyancy();
        }


        private void ApplyBuoyancy()
        {
            buoyancy_module.ApplyBuoyancySimple(sim_dt, size, explosion_params.fluid_buoyancy,
                explosion_params.fluid_weight, sim_params.ambient_temperature,
                velocity_grids, temperature_grids, thread_count);
        }

 
        //all buffers should be released on destruction
        protected override void OnDestroy()
        {
            base.OnDestroy();
            fuel_particles_buffer.Release();
        }


        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();//draw base sim gizmos
            DrawFuelParticlesGizmos();
        }


        private void DrawFuelParticlesGizmos()
        {
            if (fuel_particles_buffer == null)
                return;

            FuelParticle[] particles = new FuelParticle[fuel_particles_buffer.count];
            fuel_particles_buffer.GetData(particles);//get particles from buffer

            uint i = 0;
            foreach (FuelParticle particle in particles)
            {
                ++i;
                Gizmos.color = i <= particle_count * 0.5f ? Color.red : Color.grey;//half the particles are soot, colour them grey 
                Gizmos.DrawSphere(transform.localPosition + particle.position, explosion_params.particle_radius);
            }
        }
       
    }
}
