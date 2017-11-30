﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FluidSim2DParams
{
    [Header("Visuals")]
    public Color fluid_colour = Color.white;

    [Space] [Header("Simulation")]
    public int width = 512;
    public int height = 512;
    public float jacobi_iterations = 40;
    public float cell_size = 1;
    public float gradient_scale = 1;

    [Header("Smoke")]
    public float smoke_buoyancy = 1.0f;
    public float smoke_weight = 0.05f;
    [Space]

    [Header("Dissipations")]
    public float velocity_dissipation = 0.99f;
    public float temperature_dissipation = 0.99f;
    public float density_dissipation = 0.9999f;
    [Space]

    [Header("Temperatures")]
    public float ambient_temperature = 0.0f;
}
