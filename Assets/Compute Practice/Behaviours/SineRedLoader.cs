using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class TexUpdateEvent : UnityEvent<RenderTexture>  {};

public class SineRedLoader : MonoBehaviour
{
    [SerializeField] ComputeShader compute_shader;
    [SerializeField] TexUpdateEvent tex_update;

    private int kernel_handle;
    private RenderTexture tex;
    private Renderer rend;


    private void Start ()
	{
	    kernel_handle = compute_shader.FindKernel("CSMain");
        CreateRenderTexture();
        AssignTexture();
        UpdateTexture();
	}


    private void CreateRenderTexture()
    {
        tex = new RenderTexture(256, 256, 24);
        tex.enableRandomWrite = true;//must be set before creation
        tex.Create();

        rend = GetComponent<Renderer>();
        rend.material.SetTexture("_MainTex", tex);
    }


    private void AssignTexture()
    {
        compute_shader.SetTexture(kernel_handle, "Result", tex);
    }


    private void UpdateTexture()
    {
        compute_shader.SetFloat("dt", Time.time);
        compute_shader.Dispatch(kernel_handle, 256/8, 256/8, 1);
        rend.material.SetTexture("_MainTex", tex);
        tex_update.Invoke(tex);
    }


    private void Update()
    {
        UpdateTexture();
    }

}