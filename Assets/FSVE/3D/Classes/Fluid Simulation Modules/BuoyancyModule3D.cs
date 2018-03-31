using System;
using UnityEngine;

namespace FSVE
{
    [Serializable]
    public class BuoyancyModule3D : FluidSimModule
    {
        public void ApplyBuoyancy(float _dt, Vector3 _size, float _buoyancy, float _particle_weight, float _ambient_temperature,
            ComputeBuffer[] _velocity_grids, ComputeBuffer[] _density_grids, ComputeBuffer[] _temperature_grids, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            compute_shader.SetVector("up", new Vector4(0, 1, 0, 0));// Up is up, normally
            compute_shader.SetFloat("buoyancy", _buoyancy);
            compute_shader.SetFloat("weight", _particle_weight);
            compute_shader.SetFloat("ambient_temperature", _ambient_temperature);
            compute_shader.SetFloat("dt", _dt);

            int kernel_id = compute_shader.FindKernel("ApplyBuoyancy");
            compute_shader.SetBuffer(kernel_id, "write_RGB", _velocity_grids[WRITE]);
            compute_shader.SetBuffer(kernel_id, "velocity", _velocity_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "density", _density_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "temperature", _temperature_grids[READ]);

            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
            Swap(_velocity_grids);
        }


        // Basically the same but with no dependancy on a density grid
        public void ApplyBuoyancySimple(float _dt, Vector3 _size, float _buoyancy, float _particle_weight, float _ambient_temperature,
            ComputeBuffer[] _velocity_grids, ComputeBuffer[] _temperature_grids, intVector3 _thread_count)
        {
            compute_shader.SetVector("size", _size);
            compute_shader.SetVector("up", new Vector4(0, 1, 0, 0));
            compute_shader.SetFloat("buoyancy", _buoyancy);
            compute_shader.SetFloat("ambient_temperature", _ambient_temperature);
            compute_shader.SetFloat("dt", _dt);

            int kernel_id = compute_shader.FindKernel("ApplyBuoyancySimple");
            compute_shader.SetBuffer(kernel_id, "write_RGB", _velocity_grids[WRITE]);
            compute_shader.SetBuffer(kernel_id, "velocity", _velocity_grids[READ]);
            compute_shader.SetBuffer(kernel_id, "temperature", _temperature_grids[READ]);

            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
            Swap(_velocity_grids);
        }

    }
}
