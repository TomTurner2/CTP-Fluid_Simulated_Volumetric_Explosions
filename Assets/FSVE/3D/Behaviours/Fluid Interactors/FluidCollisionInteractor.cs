using UnityEngine;


namespace FSVE
{    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FluidSimulation3D))]
    public class FluidCollisionInteractor : MonoBehaviour
    {
        [SerializeField] FluidSimulation3D fluid_simulation = null;
        private BoxCollider simulation_collider = null;
        private CollisionForwarder active_collision_forwarder = null;

        public FluidSimulation3D FluidSimulation
        {
            get { return fluid_simulation; }
            set { fluid_simulation = value; }
        }
         

        void Start()
        {
            if (fluid_simulation == null)
                fluid_simulation = GetComponent<FluidSimulation3D>();
            UpdateCollisionVolume();// Create invisible collider to detect collisions
        }


        public void UpdateCollisionVolumeLocation()
        {
            Destroy(simulation_collider);// Remove old collider
            UpdateCollisionVolume();
            ClearTrackedColliders();
            HandleCollisionForwarding();                 
        }


        private void ClearTrackedColliders()
        {
            if (fluid_simulation == null)
                return;

            fluid_simulation.SphereColliders.Clear();
        }


        void HandleCollisionForwarding()
        {
            if (fluid_simulation == null)
                return;

            if (active_collision_forwarder)
                Destroy(active_collision_forwarder);


            Transform simulation_transform = fluid_simulation.SimulationTransform;
            if (simulation_transform == transform)
                return;

            // Add a collision event forwarder so that this script can recieve collision event from the simulation target
            active_collision_forwarder = simulation_transform.gameObject.AddComponent<CollisionForwarder>();
            active_collision_forwarder.hideFlags = HideFlags.HideInInspector;

            active_collision_forwarder.on_trigger_enter.AddListener(OnTriggerEnter);
            active_collision_forwarder.on_trigger_stay.AddListener(OnTriggerStay);
            active_collision_forwarder.on_trigger_exit.AddListener(OnTriggerExit);
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
                simulation_collider = fluid_simulation.SimulationTransform.gameObject.AddComponent<BoxCollider>();

            if (simulation_collider == null)
                return;

            simulation_collider.hideFlags = HideFlags.HideInInspector;// Prevent gameplay programmers fiddling with the volume
            simulation_collider.center = Vector3.zero;
            simulation_collider.size = Vector3.one;
            simulation_collider.isTrigger = true;
        }


        public void OnTriggerEnter(Collider _collision)
        {
            if (fluid_simulation == null)
                return;

            SphereCollider sphere_collider = _collision.gameObject.GetComponent<SphereCollider>();

            if (sphere_collider != null)
                fluid_simulation.SphereColliders.Add(sphere_collider);// If hit by a phere collider, add it to the simulation
        }


        public void OnTriggerStay(Collider _collision)
        {
            if (fluid_simulation == null)
                return;

            SphereCollider sphere_collider = _collision.gameObject.GetComponent<SphereCollider>();

            if (sphere_collider != null && !fluid_simulation.SphereColliders.Contains(sphere_collider))
                fluid_simulation.SphereColliders.Add(sphere_collider);// If hit by a phere collider, add it to the simulation
        }


        public void OnTriggerExit(Collider _collision)
        {
            if (fluid_simulation == null)
                return;

            SphereCollider sphere_collider = _collision.gameObject.GetComponent<SphereCollider>();

            if (sphere_collider != null)
                fluid_simulation.SphereColliders.Remove(sphere_collider);
        }

    }
}
