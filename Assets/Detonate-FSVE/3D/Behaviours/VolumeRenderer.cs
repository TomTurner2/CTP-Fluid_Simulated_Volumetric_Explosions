using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VolumeRenderer : MonoBehaviour
{
    [HideInInspector] public RenderTexture texture = null;
    [HideInInspector] public Vector4 size;

    private Renderer volume_renderer;
    

    void Start()
    {
        volume_renderer = GetComponent<Renderer>();
    }


    void Update ()
    {
        if (texture == null)//exit if there is no volume to render
            return;

        transform.rotation = Quaternion.identity;
        volume_renderer.material.SetVector("_translation", transform.localPosition);
        volume_renderer.material.SetVector("_scale", transform.localScale);
        volume_renderer.material.SetTexture("_density", texture);
        volume_renderer.material.SetVector("_size", size);
    }
}
