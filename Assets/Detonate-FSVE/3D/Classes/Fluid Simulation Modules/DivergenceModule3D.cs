using System;
using UnityEngine;


namespace Detonate
{
    [Serializable]
    public class DivergenceModule3D : FluidSimModule
    {
        public void CalculateDivergence(Vector3 _size, ComputeBuffer _divergence_grid,
            ComputeBuffer[] _velocity_grids, ComputeBuffer _obstacle_grid, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            int kernel_id = compute_shader.FindKernel("Divergence");
            compute_shader.SetBuffer(kernel_id, "write_R", _divergence_grid);
            compute_shader.SetBuffer(kernel_id, "velocity", _velocity_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "obstacles", _obstacle_grid);
            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
        }
    }
}
