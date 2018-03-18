using UnityEngine;


namespace Detonate
{
    [RequireComponent(typeof(FluidSmoke3D))]
    [DisallowMultipleComponent]
    public class FluidCollisionInteractor : MonoBehaviour
    {
        FluidSmoke3D fluid_simulation = null;
        BoxCollider simulation_collider = null;


        void Start()
        {
            fluid_simulation = GetComponent<FluidSmoke3D>();
            UpdateCollisionVolume();
        }


        void Update()
        {
            UpdateCollisionVolume();
        }


        void UpdateCollisionVolume()
        {
            if (simulation_collider == null)
                simulation_collider = gameObject.AddComponent<BoxCollider>();

            simulation_collider.hideFlags = HideFlags.HideInInspector;//prevent dumb gameplay programmers fiddling with the volume
            simulation_collider.center = Vector3.zero;
            simulation_collider.size = Vector3.one;
            simulation_collider.isTrigger = true;
        }


        private void OnTriggerEnter(Collider _collision)
        {
            if (fluid_simulation == null)
                return;

            SphereCollider sphere_collider = _collision.gameObject.GetComponent<SphereCollider>();

            if (sphere_collider != null)
                fluid_simulation.SphereColliders.Add(sphere_collider);
        }


        private void OnTriggerExit(Collider _collision)
        {
            if (fluid_simulation == null)
                return;

            SphereCollider sphere_collider = _collision.gameObject.GetComponent<SphereCollider>();

            if (sphere_collider != null)
                fluid_simulation.SphereColliders.Remove(sphere_collider);
        }

    }
}
