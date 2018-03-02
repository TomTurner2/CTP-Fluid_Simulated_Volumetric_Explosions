using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Detonate
{
    [Serializable]
    public class OutputModule3D : FluidSimModule
    {
        public void ConvertToVolume(Vector3 _size, ComputeBuffer[] _grid, RenderTexture _target, intVector3 _thread_count)
        {
            //convert structured buffer to 3d volume texture using gpu
            int kernel_id = compute_shader.FindKernel("ConvertToVolume");
            compute_shader.SetBuffer(kernel_id, "read_R", _grid[READ]);
            compute_shader.SetTexture(kernel_id, "write_R", _target);
            compute_shader.SetVector("size", _size);
            compute_shader.Dispatch(kernel_id, _thread_count.x, _thread_count.y, _thread_count.z);
        }

    }
}
