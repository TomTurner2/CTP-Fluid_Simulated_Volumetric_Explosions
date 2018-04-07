using UnityEngine;
using UnityEngine.Events;


namespace FSVE
{
    [System.Serializable]
    public class ColourChangeEvent : UnityEvent<Color> { }// Allow user to get new colour through event

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VolumeRenderer : MonoBehaviour
    {
        [SerializeField] public RenderTexture texture = null;
        [SerializeField] public bool randomise_colour = false;// TODO make private with getters and setters
        [SerializeField] public ColourChangeEvent on_colour_change = new ColourChangeEvent();
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
            if (texture == null)// Exit if there is no volume to render
                return;

            transform.rotation = Quaternion.identity;
            volume_renderer.material.SetVector("_translation", transform.localPosition);
            volume_renderer.material.SetVector("_scale", transform.localScale);
            volume_renderer.material.SetTexture("_density", texture); 
            volume_renderer.material.SetVector("_size", size);
        }


        public void RandomiseColour()
        {
            Color random_colour = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
            volume_renderer.material.SetColor("_Colour", random_colour);
            on_colour_change.Invoke(random_colour);
        }


        public void SetColour(Color _colour)
        {
            volume_renderer.material.SetColor("_Colour", _colour);
            on_colour_change.Invoke(_colour);
        }
    }
}