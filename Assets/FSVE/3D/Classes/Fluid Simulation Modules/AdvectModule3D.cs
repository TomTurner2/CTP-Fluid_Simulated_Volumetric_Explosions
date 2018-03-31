using System;
using UnityEngine;


namespace FSVE
{
    [Serializable]// These modules simply interface with the compute shader, look at compute shaders to see functionality
    public class AdvectModule3D : FluidSimModule
    {
        public void ApplyAdvection(float _dt, Vector3 _size, float _dissipation,
            ComputeBuffer[] _grids, ComputeBuffer[] _velocity_grids, ComputeBuffer _obstacle_grid, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            compute_shader.SetFloat("dt", _dt);
            compute_shader.SetFloat("dissipation", _dissipation);

            int kernel_id = compute_shader.FindKernel("Advect");
            compute_shader.SetBuffer(kernel_id, "read_R", _grids[READ]);
            compute_shader.SetBuffer(kernel_id, "write_R", _grids[WRITE]);
            compute_shader.SetBuffer(kernel_id, "velocity", _velocity_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "obstacles", _obstacle_grid);

            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
            Swap(_grids);
        }


        public void ApplyAdvectionVelocity(float _dt, Vector3 _size, float _velocity_dissipation,
            ComputeBuffer[] _velocity_grids, ComputeBuffer _obstacle_grid, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            compute_shader.SetFloat("dt", _dt);
            compute_shader.SetFloat("dissipation", _velocity_dissipation);
            compute_shader.SetFloat("forward", 1.0f);
            compute_shader.SetFloat("decay", _velocity_dissipation);

            int kernel_id = compute_shader.FindKernel("AdvectVelocity");
            compute_shader.SetBuffer(kernel_id, "read_RGB", _velocity_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "write_RGB", _velocity_grids[WRITE]);
            compute_shader.SetBuffer(kernel_id, "velocity", _velocity_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "obstacles", _obstacle_grid);

            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
            Swap(_velocity_grids);
        }
    }
}
