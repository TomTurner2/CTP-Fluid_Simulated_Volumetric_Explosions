using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class VolumeRenderTest : MonoBehaviour
{
    [SerializeField] private ComputeShader texture_manipulator;
    [SerializeField] private Vector3 size = new Vector3(256,256,256);
    [SerializeField] private RenderTexture texture;
    private int x, y, z;
    private Renderer renderer;

	// Use this for initialization
	void Start ()
	{
	    renderer = GetComponent<Renderer>();

        x = Mathf.ClosestPowerOfTwo((int)size.x);
	    y = Mathf.ClosestPowerOfTwo((int)size.y);
	    z = Mathf.ClosestPowerOfTwo((int)size.z);

	    texture = new RenderTexture(x, y, z)
	    {
	        dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
	        volumeDepth = z,
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true
	    };

	    //must be set before creation
	    texture.Create();

        texture_manipulator.SetTexture(0, "Result", texture);
        texture_manipulator.Dispatch(0, (int)(x / 8), (int)(y / 8), (int)(z / 8));
    }
	
	// Update is called once per frame
	void Update ()
    {
        transform.rotation = Quaternion.identity;
        renderer.material.SetVector("_translation", transform.localPosition);
        renderer.material.SetVector("_scale", transform.localScale);
        renderer.material.SetTexture("_density", texture);
        renderer.material.SetVector("_size", new Vector4(x,y,z));
    }
}
