using UnityEngine;


namespace FSVE
{
    [System.Serializable]
    public class FluidExplosion3DParams
    {
        [Space]
        [Header("Fuse")]
        public Transform fuse_transform = null;
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
        public float divergence_effect = 40;//how much divergence the fuel causes
        public float fluid_drag_effect = 0.001f;
        public float fluid_weight = 0.02f;
        public float fluid_buoyancy = 9;

        [Header("Thermals")]
        public float burn_threshold = 50;
        public float burn_rate = 0.67f;
        public float heat_emission = 40;
        
    }
}