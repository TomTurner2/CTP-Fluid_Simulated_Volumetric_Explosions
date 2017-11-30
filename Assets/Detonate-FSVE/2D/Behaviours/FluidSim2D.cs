using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSim2D : MonoBehaviour
{
    [SerializeField] FluidSim2DParams sim_params = new FluidSim2DParams();

    //Compute shaders
    [SerializeField] ComputeShader jacobi = null;
    [SerializeField] ComputeShader advect = null;
    [SerializeField] ComputeShader divergence = null;

    //Render textures used for simulation (grids)
    private RenderTexture result;//end texture
    private RenderTexture[] velocity = new RenderTexture[2];
    private RenderTexture[] density = new RenderTexture[2];
    private RenderTexture[] temperature = new RenderTexture[2];
    private RenderTexture[] preassure = new RenderTexture[2];

    private Vector2 inverse_size = Vector2.zero;

    //Array access helpers
    private const uint READ = 0;
    private const uint WRITE = 1;


    private void Start()
    {
        CalculateInverseSize();
        CreateGrids();
    }


    void CalculateInverseSize()
    {
        inverse_size = new Vector2(1.0f / sim_params.width,
            1.0f / sim_params.height);
    }


#region Render Texture "Grid" Creation
    void CreateGrids()
    {
        CreateGridsReadWrite(velocity, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
        CreateGridsReadWrite(density, RenderTextureFormat.RFloat, FilterMode.Bilinear);
        CreateGridsReadWrite(temperature, RenderTextureFormat.RFloat, FilterMode.Bilinear);
        CreateGridsReadWrite(preassure, RenderTextureFormat.RFloat, FilterMode.Bilinear);
    }


    void CreateGridsReadWrite(RenderTexture[] _grid, RenderTextureFormat _format,
        FilterMode _filter)
    {
       CreateGrid(_grid[READ], _format, _filter);
        CreateGrid(_grid[WRITE], _format, _filter);
    }


    void CreateGrid(RenderTexture _grid, RenderTextureFormat _format, FilterMode _filter)
    {
        _grid = new RenderTexture(sim_params.width, sim_params.height,
            0, _format, RenderTextureReadWrite.Linear);
        _grid.filterMode = _filter;
        _grid.wrapMode = TextureWrapMode.Clamp;
        _grid.Create();
    }
#endregion

}
