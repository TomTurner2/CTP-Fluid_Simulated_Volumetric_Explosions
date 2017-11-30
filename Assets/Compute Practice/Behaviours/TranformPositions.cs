using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranformPositions : MonoBehaviour
{
    [SerializeField] ComputeShader compute_shader;
    [SerializeField] VectorMatrixPair[] data = new VectorMatrixPair[2];//create the data to run the calcs on
    [SerializeField] VectorMatrixPair[] output = new VectorMatrixPair[2];


    [System.Serializable]
    struct VectorMatrixPair
    {
        public Vector3 point;
        public Matrix4x4 matrix;
    }


    private void Start()
    {
        if (compute_shader == null)
            return;

        RunShader();
    }


    private void RunShader()
    {
        data[0].matrix = Matrix4x4.identity;//shouldn't change the vector
        data[0].point = Vector3.up;

        data[1].matrix = Matrix4x4.Perspective(80, Screen.width / Screen.height, 0.1f, 1000);
        data[1].point = Vector3.one;

        int data_size = 76;//need to define byte size of struct, can't use size_of because unity parses into c++
        ComputeBuffer buffer = new ComputeBuffer(data.Length, data_size);//create the buffer passing in the size

        int kernel = compute_shader.FindKernel("Multiply");//get entry point function id
        compute_shader.SetBuffer(kernel, "data_buffer", buffer);
        compute_shader.Dispatch(kernel, data.Length, 1, 1);
    
        buffer.GetData(output);//get the data back out of input

        buffer.Release();
    }
}
