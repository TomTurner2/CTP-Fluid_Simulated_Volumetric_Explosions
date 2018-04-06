using System.Collections.Generic;
using System.Linq;
using FSVE;
using UnityEngine;


namespace FSVE
{
    public class FluidStreamer : MonoBehaviour
    {
        [SerializeField] FluidSimulation3D target_simulation = null;
        [SerializeField] List<VolumeRenderer> renderers_in_scene = new List<VolumeRenderer>();
        private Transform closest_transform = null;


        void Update()
        {
            if (renderers_in_scene.Count <= 0 || target_simulation == null)
                return;

            renderers_in_scene = renderers_in_scene.OrderBy(renderer => Vector3.Distance(transform.position,
                renderer.transform.position)).ToList();

            if (closest_transform != renderers_in_scene[0].transform)
            {
                target_simulation.SimulationTransform = renderers_in_scene[0].transform;
                target_simulation.SphereColliders.Clear();
            }

            closest_transform = renderers_in_scene[0].transform;

        }
    }
}
