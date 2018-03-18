using UnityEngine;


namespace Detonate
{
    [System.Serializable]
    public class FluidSmoke3DParams
    {
        [Header("Smoke")]
        public float smoke_buoyancy = 9.0f;
        public float smoke_weight = 0.02f;
        public float density_dissipation = 0.999f;
    }
}
