using System.Collections.Generic;
using UnityEngine;


namespace FSVE
{
    public class FluidSmoke3D : FluidSimulation3D
    {
        [SerializeField] FluidSmoke3DParams smoke_params = new FluidSmoke3DParams();       
        [SerializeField] List<SmokeEmitter> smoke_emitters = new List<SmokeEmitter>();

        private ComputeBuffer[] density_grids = new ComputeBuffer[2];// Smoke simulates movement of density


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
            int buffer_size = sim_params.width * sim_params.height * sim_params.depth;
            CreateDensityGrids(buffer_size);
        }


        private void CreateDensityGrids(int _buffer_size)
        {
            density_grids[READ] = new ComputeBuffer(_buffer_size, sizeof(float));
            density_grids[WRITE] = new ComputeBuffer(_buffer_size, sizeof(float));
        }


        protected override void Update()
        {
            base.Update();// Determines which time step to use

            MoveStage();
            AddForcesStage();
            CalculateDivergence();// Fluid diffusion
            MassConservationStage();
            CreateObstacles();
            UpdateVolumeRenderer();
        }


        protected override void MoveStage()
        {
            advection_module.ApplyAdvection(sim_dt, size, smoke_params.density_dissipation,
                density_grids, velocity_grids, obstacle_grid, thread_count);// Advect density

            base.MoveStage();// Advect base grids
        }


        private void AddForcesStage()
        {
            ApplyBuoyancy();
            ApplyEmitters();// Smoke emitters unique to this sim
        }


        private void ApplyBuoyancy()
        {
            buoyancy_module.ApplyBuoyancy(sim_dt, size, smoke_params.smoke_buoyancy,
                smoke_params.smoke_weight, sim_params.ambient_temperature,
                velocity_grids, density_grids, temperature_grids, thread_count);
        }


        public List<SmokeEmitter> SmokeEmitters
        {
            get
            {
                return smoke_emitters;
            }
            set
            {
                smoke_emitters = value;
            }
        }


        private void ApplyEmitters()
        {
            for(int i = 0; i < smoke_emitters.Count; ++i)
            {
                if (smoke_emitters[i] == null)
                {
                    smoke_emitters.RemoveAt(i);// Remove nulls
                    continue;
                }

                if (!smoke_emitters[i].isActiveAndEnabled)
                    continue;

                if (!smoke_emitters[i].gameObject.activeInHierarchy)
                    continue;

                if (!smoke_emitters[i].Emit)
                    continue;

                ApplyImpulse(smoke_emitters[i].DenisityAmount, smoke_emitters[i].EmissionRadius,
                    density_grids, smoke_emitters[i].transform.position);// Add density at emitter position
                ApplyImpulse(smoke_emitters[i].TemperatureAmount, smoke_emitters[i].EmissionRadius,
                    temperature_grids, smoke_emitters[i].transform.position);// Add temperature at emitter position
            }
        }


        protected override RenderTexture ConvertGridToVolume(GridType _grid_type)
        {
            if (_grid_type == GridType.DENSITY)
            {
                output_module.ConvertToVolume(size, density_grids[READ],
                    volume_output, thread_count);// Output density grid
                return volume_output;
            }

            return base.ConvertGridToVolume(_grid_type);// Let base handle other conversions
        }
     

        // All buffers should be released on destruction
        protected override void OnDestroy()
        {
            base.OnDestroy();
            density_grids[READ].Release();// Unique to this sim
            density_grids[WRITE].Release();        
        }
    }
}
