using UnityEngine;


namespace FSVE
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VolumeRenderer : MonoBehaviour
    {
        [SerializeField] public bool randomise_colour = false;
        [SerializeField] public RenderTexture texture = null;
        [HideInInspector] public Vector4 size;

        private Renderer volume_renderer;


        void Start()
        {
            volume_renderer = GetComponent<Renderer>();

            if (randomise_colour)
                RandomiseColour();
        }


        void Update()
        {
            if (texture == null)//exit if there is no volume to render
                return;

            transform.rotation = Quaternion.identity;
            volume_renderer.material.SetVector("_translation", transform.localPosition);
            volume_renderer.material.SetVector("_scale", transform.localScale);
            volume_renderer.material.SetTexture("_density", texture); 
            volume_renderer.material.SetVector("_size", size);
        }


        public void RandomiseColour()
        {
            volume_renderer.material.SetColor("_Colour",
                Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f));
        }
    }
}