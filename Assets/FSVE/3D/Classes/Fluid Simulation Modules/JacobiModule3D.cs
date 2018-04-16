using System;
using UnityEngine;


namespace FSVE
{
    [Serializable]
    public class JacobiModule3D : FluidSimModule
    {
        public void CalculatePressure(Vector3 _size, ComputeBuffer _divergence_grid,
            ComputeBuffer _obstacle_grid, uint _jacobi_iterations,
            ComputeBuffer[] _pressure_grids, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            int kernel_id = compute_shader.FindKernel("Jacobi");
            compute_shader.SetBuffer(kernel_id, "divergence", _divergence_grid);
            compute_shader.SetBuffer(kernel_id, "obstacles", _obstacle_grid);
            
            for (int i = 0; i < _jacobi_iterations; ++i)// Pressure gradient is calculated iteratively, most expensive part
            {
                compute_shader.SetBuffer(kernel_id, "write_R", _pressure_grids[WRITE]);
                compute_shader.SetBuffer(kernel_id, "pressure", _pressure_grids[READ]);
                compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
                Swap(_pressure_grids);
            }
        }

    }
}
