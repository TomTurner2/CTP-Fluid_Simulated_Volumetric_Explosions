using System;
using System.Collections.Generic;
using UnityEngine;


namespace FSVE
{
    [Serializable]
    public class SmokeEmitter : MonoBehaviour
    {
        [SerializeField] bool draw_debug = true;
        [SerializeField] bool emit = true;
        [SerializeField] float emission_radius = 0.04f;
        [SerializeField] float density_amount = 6.0f;
        [SerializeField] float temperature_amount = 10.0f;

        public static List<SmokeEmitter> emitters_in_scene = null;
        const uint MAX_EMITTERS_ALLOWED_IN_SCENE = 50;


        void Start()
        {
            if (emitters_in_scene == null)
                emitters_in_scene = new List<SmokeEmitter>();

            if (emitters_in_scene.Count > MAX_EMITTERS_ALLOWED_IN_SCENE)
            {
                Debug.LogWarning("Max emitters per scene exceeded");
                Destroy(this);
            }

            emitters_in_scene.Add(this);
        }


        public bool Emit
        {
            get
            {
                return emit;
            }
            set
            {
                emit = value;
            }
        }


        public float EmissionRadius
        {
            get
            {
                return emission_radius;
            }
            set
            {
                emission_radius = value;
            }
        }


        public float DenisityAmount
        {
            get
            {
                return density_amount;
            }
            set
            {
                density_amount = value;
            }
        }


        public float TemperatureAmount
        {
            get
            {
                return temperature_amount;
            }
            set
            {
                temperature_amount = value;
            }
        }


        private void OnDestroy()
        {
            emitters_in_scene.Remove(this);
        }


        private void OnDrawGizmosSelected()
        {
            if (!draw_debug)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, emission_radius);
        }
    }
}
