using UnityEngine;


namespace FSVE
{
    [System.Serializable]
    public class FluidSim3DParams
    {
        [Space]
        [Header("Simulation")]
        public int width = 128;
        public int height = 128;
        public int depth = 128;
        public uint jacobi_iterations = 10;
        public bool dynamic_time_step = false;
        public float simulation_speed = 1.5f;
        public float fixed_time_step = 0.1f;
        public bool simulation_bounds = true;


        [Header("Dissipations")]
        public float velocity_dissipation = 0.995f;
        public float temperature_dissipation = 0.99f;

        [Space]

        [Header("Temperatures")]
        public float ambient_temperature = 0.0f;
    }
}
