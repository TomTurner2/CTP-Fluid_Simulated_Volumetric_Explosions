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
        private FluidCollisionInteractor collision_interactor = null;


        void Start()
        {
            if (target_simulation != null)
                collision_interactor = target_simulation.GetComponent<FluidCollisionInteractor>();
        }


        void Update()
        {
            if (renderers_in_scene.Count <= 0 || target_simulation == null)
                return;

            renderers_in_scene = renderers_in_scene.OrderBy(renderer => Vector3.Distance(transform.position,
                renderer.transform.position)).ToList();

            Transform new_closest_transform = renderers_in_scene[0].transform;

            if (closest_transform != new_closest_transform)
            {
                target_simulation.SimulationTransform = renderers_in_scene[0].transform;
                
                if (collision_interactor != null)
                    collision_interactor.UpdateCollisionVolumeLocation();
            }

            closest_transform = new_closest_transform;

        }



        private void OnDestroy()
        {
            if (target_simulation == null)
                return;

            target_simulation.SimulationTransform = target_simulation.transform;

            if (collision_interactor != null)
                collision_interactor.UpdateCollisionVolumeLocation();
        }

    }
}
