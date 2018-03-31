using UnityEngine;


namespace FSVE
{
    public class VolumeRenderTest : MonoBehaviour// Messy test script
    {
        [SerializeField] ComputeShader texture_manipulator;
        [SerializeField] Vector3 size = new Vector3(256, 256, 256);
        [SerializeField] RenderTexture texture;

        private int x, y, z;
        private Renderer volume_renderer;


        void Start()
        {
            volume_renderer = GetComponent<Renderer>();

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

            // Must be set before creation
            texture.Create();

            texture_manipulator.SetTexture(0, "Result", texture);
            texture_manipulator.Dispatch(0, (int)(x / 8), (int)(y / 8), (int)(z / 8));
        }

        // Update is called once per frame
        void Update()
        {
            transform.rotation = Quaternion.identity;
            volume_renderer.material.SetVector("_translation", transform.localPosition);
            volume_renderer.material.SetVector("_scale", transform.localScale);
            volume_renderer.material.SetTexture("_density", texture);
            volume_renderer.material.SetVector("_size", new Vector4(x, y, z));
        }
    }
}
