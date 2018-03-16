using System;
using UnityEngine;


namespace Detonate
{
    [Serializable]
    public class FuelParticleSimulationModule
    {
        [SerializeField] ComputeShader compute_shader = null;


        public void UpdateParticlePhysics(ComputeBuffer _particles, uint _particle_count, float _dt)
        {
            if (compute_shader == null)
                return;

            compute_shader.SetFloat("dt", _dt);
            compute_shader.SetInt("num_particles", (int)_particle_count);

            int kernel_id = compute_shader.FindKernel("ApplyParticlesVelocities");
            compute_shader.SetBuffer(kernel_id, "particles", _particles);
            compute_shader.Dispatch(kernel_id, (int)(_particle_count / 8), 1, 1);//single dimension array, scale thread group according to particle count
        }
    }
}
