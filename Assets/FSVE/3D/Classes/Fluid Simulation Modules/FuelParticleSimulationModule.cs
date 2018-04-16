using System;
using UnityEngine;


namespace FSVE
{
    [Serializable]
    public class FuelParticleSimulationModule
    {
        [SerializeField] ComputeShader compute_shader = null;


        public void UpdateParticlePositions(ComputeBuffer _particles, uint _particle_count, float _dt)
        {
            if (compute_shader == null)
                return;

            compute_shader.SetFloat("dt", _dt);

            int kernel_id = compute_shader.FindKernel("ApplyParticlesVelocities");
            compute_shader.SetBuffer(kernel_id, "particles", _particles);
            const int thread_group_count = 8;
            compute_shader.Dispatch(kernel_id, (int)(_particle_count / thread_group_count), 1, 1);// Scale threads according to count
        }


        public void UpdateParticleVelocity(ComputeBuffer _particles, ComputeBuffer _fluid_temperature, ComputeBuffer _fluid_velocity,
            float _particle_drag, float _particle_radius, float _thermal_mass, uint _particle_count, float _dt, Vector3 _size)
        {
            if (compute_shader == null)
                return;

            compute_shader.SetFloat("dt", _dt);
            compute_shader.SetFloat("particle_drag", _particle_drag);
            compute_shader.SetFloat("particle_radius", _particle_radius);
            compute_shader.SetFloat("thermal_mass", _thermal_mass);
            compute_shader.SetVector("size", _size);


            int kernel_id = compute_shader.FindKernel("CalculateParticlesVelocity");
            compute_shader.SetBuffer(kernel_id, "particles", _particles);
            compute_shader.SetBuffer(kernel_id, "velocity", _fluid_velocity);
            compute_shader.SetBuffer(kernel_id, "temperature", _fluid_temperature);
            compute_shader.Dispatch(kernel_id, (int)(_particle_count / 8), 1, 1);
        }


        public void BurnParticles(ComputeBuffer _particles, ComputeBuffer _fluid_temperature,
            ComputeBuffer _fluid_divergence, float _burn_rate, float _produced_heat, float _burn_threshold,
            float _divergence_amount, uint _particles_count, float _dt, Vector3 _size)
        {
            if (compute_shader == null)
                return;

            compute_shader.SetFloat("dt", _dt);
            compute_shader.SetFloat("burn_rate", _burn_rate);
            compute_shader.SetFloat("produced_heat", _produced_heat);
            compute_shader.SetFloat("divergence_amount", _divergence_amount);
            compute_shader.SetFloat("burn_threshold", _burn_threshold);
            compute_shader.SetVector("size", _size);

            int kernel_id = compute_shader.FindKernel("BurnParticle");
            compute_shader.SetBuffer(kernel_id, "particles", _particles);
            compute_shader.SetBuffer(kernel_id, "temperature_write", _fluid_temperature);
            compute_shader.SetBuffer(kernel_id, "divergence", _fluid_divergence);
            compute_shader.Dispatch(kernel_id, (int)_particles_count/8, 1, 1);
        }

    }
}
