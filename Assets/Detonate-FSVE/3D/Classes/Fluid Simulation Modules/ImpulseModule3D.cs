using System;
using UnityEngine;


namespace FSVE
{
    [Serializable]
    public class ImpulseModule3D : FluidSimModule
    {
        public void ApplyImpulse(float _dt, Vector3 _size, float _amount, float _impulse_radius,
            Vector3 _impulse_position, ComputeBuffer[] _grids, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            compute_shader.SetFloat("radius", _impulse_radius);
            compute_shader.SetFloat("source_amount", _amount);
            compute_shader.SetFloat("dt", _dt);
            compute_shader.SetVector("source_pos", _impulse_position);

            int kernel_id = compute_shader.FindKernel("Impulse");
            compute_shader.SetBuffer(kernel_id, "read_R", _grids[READ]);
            compute_shader.SetBuffer(kernel_id, "write_R", _grids[WRITE]);
            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
            Swap(_grids);
        }

    }
}
