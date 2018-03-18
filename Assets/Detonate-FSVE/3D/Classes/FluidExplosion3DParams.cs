using UnityEngine;

namespace Detonate
{
    [System.Serializable]
    public class FluidExplosion3DParams
    {
        [Space]
        [Header("Fuse")]
        public Vector3 fuse_position = Vector3.zero;
        public float fuse_radius = 0.02f;

        [Space]
        [Header("Particles")]
        public uint particle_count = 200;
        public float particle_radius = 0.1f;
        public float particle_drag = 950.0f;

        [Header("Masses")]
        public float mass = 0.7f;
        public float soot_mass = 0.001f;
        public float thermal_mass = 1.6f;

        [Header("Fluid Effect")]
        public float fuel_divergence_amount = 40;
        public float fluid_drag_effect = 0.001f;

        [Header("Thermals")]
        public float ignition_temperature = 50;
        public float burn_rate = 0.67f;
        public float fuel_burn_amount = 40;
        public float generated_soot_amount = 260;
        public float soot_threshold = 1.0f;
    }
}