using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class FluidExplosion3DParams
{
    [Space]
    [Header("Fuse")]
    public Vector3 fuse_position = Vector3.zero;
    public float fuse_radius = 0.02f;

    [Space]
    [Header("Fuel Particles")]
    public int particle_count = 200;
    public float mass = 1;
    public float soot_mass = 2;
    public float thermal_mass = 3;
    public float particle_radius = 0.01f;
    public float particle_drag = 1;
    public float fluid_drag_effect = 2;
    public float ignition_temperature = 4;
    public float burn_rate = 2;
    public float fuel_burn_amount = 1;
    public float fuel_divergence_amount = 2;
    public float generated_soot_amount = 2;
    public float soot_threshold = 2;
}
