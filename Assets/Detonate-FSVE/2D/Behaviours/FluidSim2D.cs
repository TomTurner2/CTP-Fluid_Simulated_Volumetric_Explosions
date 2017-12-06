using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSim2D : MonoBehaviour
{
    [SerializeField] FluidSim2DParams sim_params = new FluidSim2DParams();

    //Compute shaders
    [SerializeField] ComputeShader jacobi = null;
    [SerializeField] ComputeShader advect = null;
    [SerializeField] ComputeShader buoyancy = null;
    [SerializeField] ComputeShader divergence = null;
    [SerializeField] ComputeShader obstacles = null;

    //Render textures used for simulation (grids)
    private RenderTexture[] velocity_grids = new RenderTexture[2];
    private RenderTexture[] density_grids = new RenderTexture[2];
    private RenderTexture[] temperature_grids = new RenderTexture[2];
    private RenderTexture[] preassure_grids = new RenderTexture[2];
    private RenderTexture obstacle_grid;//will only read
    private RenderTexture temp_grid;//used for storing temporary grid states

    private Vector2 inverse_size = Vector2.zero;
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
        CalculateInverseSize();
        CalculateThreadCount();
        CreateGridSets();//creates render texture grid sets
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


    private void CalculateInverseSize()
    {
        inverse_size = new Vector2(1.0f / sim_params.width,
            1.0f / sim_params.height);
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
        advect.SetFloat("dt", Time.deltaTime);
        advect.SetFloat("forward", _forward);
        advect.SetFloat("dissipation", _dissipation);
        advect.SetFloat("decay", _decay);

        //set texture grids
        int kernel_id = advect.FindKernel("Advect");
        advect.SetTexture(kernel_id, "write_R", _grid[WRITE]);//ony 1 channel
        advect.SetTexture(kernel_id, "read_R", _grid[READ]);
        advect.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
        advect.SetTexture(kernel_id, "obstacles", obstacle_grid);

        //run calculation on GPU
        advect.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
        Swap(_grid);//swap read and write grids
    }


    private void ApplyAdvectionVelocity()
    {
        //set compute vars
        advect.SetFloat("dt", Time.deltaTime);
        advect.SetFloat("dissipation", sim_params.velocity_dissipation);
        advect.SetFloat("forward", 1.0f);
        advect.SetFloat("decay", 0.0f);

        //set texture grids
        int kernel_id = advect.FindKernel("AdvectVelocity");
        advect.SetTexture(kernel_id, "read_RG", velocity_grids[READ]);//two channels to represent vector components
        advect.SetTexture(kernel_id, "write_RG", velocity_grids[WRITE]);
        advect.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
        advect.SetTexture(kernel_id, "obstacles", obstacle_grid);

        //run calculation on GPU
        advect.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
        Swap(velocity_grids);//swap read and write velocity grids
    }


    private void ApplyBuoyency()
    {
        //set compute vars
        buoyancy.SetFloat("dt", Time.deltaTime);
        buoyancy.SetVector("up", new Vector4(0,1,0,0));//y is up
        buoyancy.SetFloat("weight", sim_params.smoke_weight);
        buoyancy.SetFloat("buoyancy", sim_params.smoke_buoyancy);
        buoyancy.SetFloat("ambient_temperature", sim_params.ambient_temperature);

        //set texture grids
        int kernel_id = buoyancy.FindKernel("ApplyBuoyancy");
        buoyancy.SetTexture(kernel_id, "temperature", temperature_grids[READ]);
        buoyancy.SetTexture(kernel_id, "write_RG",  velocity_grids[WRITE]);
        buoyancy.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
        buoyancy.SetTexture(kernel_id, "density", density_grids[READ]);
        
        //run calculation on GPU
        buoyancy.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
        Swap(velocity_grids);
    }


    private void CalculateDivergence()
    {
        //set texture grids
        int kernel_id = divergence.FindKernel("Divergence");
        divergence.SetTexture(kernel_id, "write_RG", temp_grid);
        divergence.SetTexture(kernel_id, "velocity", velocity_grids[READ]);
        divergence.SetTexture(kernel_id, "obstacles", obstacle_grid);

        //run calculation on GPU
        divergence.Dispatch(kernel_id, x_thread_count, y_thread_count, 1);
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
        CreateGridSet(ref velocity_grids, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
        CreateGridSet(ref density_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
        CreateGridSet(ref temperature_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);
        CreateGridSet(ref preassure_grids, RenderTextureFormat.RFloat, FilterMode.Bilinear);

        //Obstacles grid will only be read, so only one grid needed
        CreateGrid(ref obstacle_grid, RenderTextureFormat.RFloat, FilterMode.Bilinear);
        CreateGrid(ref temp_grid, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
    }


    //Create a read and write grid for the given grid set.
    private void CreateGridSet(ref RenderTexture[] _grid, RenderTextureFormat _format,
        FilterMode _filter)
    {
        CreateGrid(ref _grid[READ], _format, _filter);
        CreateGrid(ref _grid[WRITE], _format, _filter);
    }


    //Create and intialise the grid render texture.
    private void CreateGrid(ref RenderTexture _grid, RenderTextureFormat _format, FilterMode _filter)
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
