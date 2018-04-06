using UnityEngine;


namespace FSVE
{    
    [DisallowMultipleComponent]
    public class FluidCollisionInteractor : MonoBehaviour
    {
        [SerializeField] FluidSmoke3D fluid_simulation = null;
        private BoxCollider simulation_collider = null;


        void Start()
        {
            if (fluid_simulation == null)
                fluid_simulation = GetComponent<FluidSmoke3D>();
            UpdateCollisionVolume();// Create invisible collider to detect collisions
        }


        void Update()
        {
            UpdateCollisionVolume();// Just in case collider is somehow destroyed through code
        }


        void UpdateCollisionVolume()
        {
            if (fluid_simulation == null)
                return;
    
            if (simulation_collider == null )
                simulation_collider = gameObject.AddComponent<BoxCollider>();

            if (simulation_collider == null)
                return;

            simulation_collider.hideFlags = HideFlags.HideInInspector;// Prevent gameplay programmers fiddling with the volume
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
                fluid_simulation.SphereColliders.Add(sphere_collider);// If hit by a phere collider, add it to the simulation
        }


        private void OnTriggerStay(Collider _collision)
        {
            if (fluid_simulation == null)
                return;

            SphereCollider sphere_collider = _collision.gameObject.GetComponent<SphereCollider>();

            if (sphere_collider != null && !fluid_simulation.SphereColliders.Contains(sphere_collider))
                fluid_simulation.SphereColliders.Add(sphere_collider);// If hit by a phere collider, add it to the simulation
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
