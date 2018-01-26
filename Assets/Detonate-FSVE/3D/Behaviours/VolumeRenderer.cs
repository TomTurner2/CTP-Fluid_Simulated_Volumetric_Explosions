using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeRenderer : MonoBehaviour
{
    public RenderTexture texture = null;

    private Renderer renderer;
    private Vector4 size;


    void Start()
    {
        renderer = GetComponent<Renderer>();
    }


    void Update ()
    {
        if (texture == null)//exit if there is no volume to render
            return;
        Debug.Log("updating");
        transform.rotation = Quaternion.identity;
        renderer.material.SetVector("_translation", transform.localPosition);
        renderer.material.SetVector("_scale", transform.localScale);
        renderer.material.SetTexture("_density", texture);
        renderer.material.SetVector("_size", new Vector4(texture.width, texture.height, texture.volumeDepth));
    }
}
