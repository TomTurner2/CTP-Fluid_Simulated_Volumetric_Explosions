using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSim2D : MonoBehaviour
{
    [SerializeField] FluidSim2DParams sim_params = new FluidSim2DParams();

    //Compute shaders
    [SerializeField] ComputeShader jacobi = null;
    [SerializeField] ComputeShader advect = null;
    [SerializeField] ComputeShader apply_advect = null;
    [SerializeField] ComputeShader buoyancy = null;
    [SerializeField] ComputeShader divergence = null;
    [SerializeField] ComputeShader obstacles = null;

    //Render textures used for simulation (grids)
    private RenderTexture[] velocity_grids = new RenderTexture[2];
    private RenderTexture[] density_grids = new RenderTexture[2];
    private RenderTexture[] temperature_grids = new RenderTexture[2];
    private RenderTexture[] preassure_grids = new RenderTexture[2];
    private RenderTexture obstacle_grid;

    private Vector4 size = Vector4.zero;//vector4 to easily map to compute buffer

    //Number of threads required based on texture size
    private int x_thread_count = 0;
    private int y_thread_count = 0;

    //Constants
    private const uint READ = 0;//for accessing grid sets
    private const uint WRITE = 1;
    private const uint THREAD_COUNT = 8;//threads used by compute shader


    private void Start()
    {
        ValidateTextureDimensions();
        CalculateSize();
        CalculateThreadCount();
        CreateGridSets();
        SetBoundary();
    }


    private void ValidateTextureDimensions()
    {
        sim_params.width = Mathf.ClosestPowerOfTwo(sim_params.width);
        sim_params.height = Mathf.ClosestPowerOfTwo(sim_params.height);
    }


    private void CalculateSize()
    {
        size = new Vector4(sim_params.width, sim_params.height, 0, 0);
    }


    private void CalculateThreadCount()
    {
        x_thread_count = (int)(sim_params.width / THREAD_COUNT);
        y_thread_count = (int) (sim_params.height / THREAD_COUNT);
    }


    private void Swap(RenderTexture[] _grid)
    {
        RenderTexture temp = _grid[READ];
        _grid[READ] = _grid[WRITE];
        _grid[WRITE] = temp;
    }


    private void Update()
    {
        //advect grids with quantities
        ApplyAdvection(sim_params.temperature_dissipation, 0.0f, temperature_grids);
        ApplyAdvection(sim_params.density_dissipation, 0.0f, density_grids);
        //TODO advect reaction
        
        //apply advections
        ApplyAdvectionVelocity();
        ApplyBuoyency();

        //apply any impulses
        //TODO apply reaction impulse
        //TODO apply temperature impulse

        //extinguisment
        //vorticity

        CalculateDivergence();
        CalculatePressure();
        CalculateProjection();

        transform.rotation = Quaternion.identity;
        //update renderer
    }


    private void ApplyAdvection(float _dissipation, float _decay, RenderTexture[] _grid,
        float _forward = 1.0f)
    {
        //set compute vars
        apply_advect.SetFloat("dt", Time.deltaTime);
        apply_advect.SetVector("size", size);
        apply_advect.SetFloat("forward", _forward);
        apply_advect.SetFloat("dissipation", _dissipation);
        apply_advect.SetFloat("decay", _decay);

        //set texture grids
        int kernel_id = apply_advect.FindKernel("Advect");
        apply_advect.SetTexture(kernel_id, "write_RG", _grid[WRITE]);
        apply_advect.SetTexture(kernel_id, "read_RG", _grid[READ]);
        apply_advect.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
        apply_advect.SetTexture(kernel_id, "obstacles", obstacle_grid);

        //run calculation on GPU
        apply_advect.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
        Swap(_grid);//swap read and write grids
    }


    private void ApplyAdvectionVelocity()
    {
        
    }


    private void ApplyBuoyency()
    {
        
    }


    private void CalculateDivergence()
    {
        
    }


    private void CalculatePressure()
    {
        
    }


    private void CalculateProjection()
    {
        
    }


    private void SetBoundary()
    {
        
    }


    #region Render Texture "Grid" Creation
    //Create all the required grids.
    private void CreateGridSets()
    {
        CreateGridSet(velocity_grids, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
        CreateGridSet(density_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
        CreateGridSet(temperature_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
        CreateGridSet(preassure_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
    }


    //Create a read and write grid for the given grid set.
    private void CreateGridSet(RenderTexture[] _grid, RenderTextureFormat _format,
        FilterMode _filter)
    {
        CreateGrid(_grid[READ], _format, _filter);
        CreateGrid(_grid[WRITE], _format, _filter);
    }


    //Create and intialise the grid render texture.
    private void CreateGrid(RenderTexture _grid, RenderTextureFormat _format, FilterMode _filter)
    {
        _grid = new RenderTexture(sim_params.width, sim_params.height,
            0, _format, RenderTextureReadWrite.Linear)
        {
            filterMode = _filter,
            wrapMode = TextureWrapMode.Clamp
        };
        _grid.Create();
    }
#endregion

}
