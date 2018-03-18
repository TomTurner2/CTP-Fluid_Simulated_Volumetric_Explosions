using UnityEngine;


namespace Detonate
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

        [Header("Dissipations")]
        public float velocity_dissipation = 0.995f;
        public float temperature_dissipation = 0.99f;

        [Space]

        [Header("Temperatures")]
        public float ambient_temperature = 0.0f;
    }
}
