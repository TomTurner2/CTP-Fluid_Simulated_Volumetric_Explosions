using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FluidSim3DParams
{
    [Space]
    [Header("Simulation")]
    public int width = 128;
    public int height = 128;
    public int depth = 128;
    public uint jacobi_iterations = 10;

    [Header("Smoke")]
    public float smoke_buoyancy = 1.0f;
    public float smoke_weight = 0.0125f;
    [Space]

    [Header("Dissipations")]
    public float velocity_dissipation = 0.995f;
    public float temperature_dissipation = 0.99f;
    public float density_dissipation = 0.999f;

    [Space]

    [Header("Temperatures")]
    public float ambient_temperature = 0.0f;
}
