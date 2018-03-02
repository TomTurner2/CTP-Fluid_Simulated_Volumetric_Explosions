using System;
using UnityEngine;


namespace Detonate
{
    [Serializable]
    public class ProjectionModule3D : FluidSimModule
    {
        public void CalculateProjection(Vector3 _size, ComputeBuffer[] _pressure_grids,
            ComputeBuffer _obstacle_grid, ComputeBuffer[] _velocity_grids, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            int kernel_id = compute_shader.FindKernel("Projection");
            compute_shader.SetBuffer(kernel_id, "obstacles", _obstacle_grid);
            compute_shader.SetBuffer(kernel_id, "pressure", _pressure_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "velocity", _velocity_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "write_RGB", _velocity_grids[WRITE]);

            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
            Swap(_velocity_grids);
        }

    }
}
