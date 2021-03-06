﻿using System;
using UnityEngine;


namespace FSVE
{
    [Serializable]
    public class OutputModule3D : FluidSimModule
    {
        public void ConvertToVolume(Vector3 _size, ComputeBuffer _grid, RenderTexture _target, intVector3 _thread_count)
        {
            // Convert structured buffer to 3d volume texture using gpu
            int kernel_id = compute_shader.FindKernel("ConvertToVolume");
            compute_shader.SetBuffer(kernel_id, "read_R", _grid);
            compute_shader.SetTexture(kernel_id, "write_tex", _target);
            compute_shader.SetVector("size", _size);
            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
        }


        public void FuelParticleToVolume(Vector3 _size, ComputeBuffer _particles, RenderTexture _target, uint _particle_count, bool _trace_particles, intVector3 _thread_count)
        {
            // Convert particles buffer to 3d volume texture using gpu
            int kernel_id = compute_shader.FindKernel("ParticleToVolume");
            compute_shader.SetBuffer(kernel_id, "particles", _particles);
            compute_shader.SetTexture(kernel_id, "write_tex", _target);
            compute_shader.SetVector("size", _size);

            if (!_trace_particles)// If not tracing particles I have to clear the volume each frame
            {
                int clear_kernel = compute_shader.FindKernel("ClearVolume");// GL.Clear doesn't seem to work for 3D RenderTexture
                compute_shader.SetTexture(clear_kernel, "write_tex", _target);
                compute_shader.Dispatch(clear_kernel, _thread_count.x, _thread_count.y, _thread_count.z);
            }

            compute_shader.Dispatch(kernel_id, (int)_particle_count/8, 1, 1);
        }

    }
}
