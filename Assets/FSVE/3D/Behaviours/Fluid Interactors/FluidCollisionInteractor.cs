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


        private void HandleCollisionForwarding()
        {
            if (fluid_simulation == null)
                return;

            if (active_collision_forwarder)
                Destroy(active_collision_forwarder);


            Transform simulation_transform = fluid_simulation.SimulationTransform;
            if (simulation_transform == transform)
                return;

            // Add a collision event forwarder so that this script can recieve collision events from the simulation target
            active_collision_forwarder = simulation_transform.gameObject.AddComponent<CollisionForwarder>();
            active_collision_forwarder.hideFlags = HideFlags.HideInInspector;

            active_collision_forwarder.on_trigger_enter.AddListener(OnTriggerEnter);
            active_collision_forwarder.on_trigger_stay.AddListener(OnTriggerStay);
            active_collision_forwarder.on_trigger_exit.AddListener(OnTriggerExit);
        }


        private void Update()
        {
            UpdateCollisionVolume();// Just in case collider is somehow destroyed through code
        }


        private void UpdateCollisionVolume()
        {
            if (fluid_simulation == null)
                return;

            if (simulation_collider == null && fluid_simulation != null)
            {
                if (fluid_simulation.SimulationTransform != null)// Just in case of script execution order
                {
                    simulation_collider = fluid_simulation.SimulationTransform.gameObject.AddComponent<BoxCollider>();
                }                
            }

            if (simulation_collider == null)
                return;

            simulation_collider.hideFlags = HideFlags.HideInInspector;// Prevent gameplay programmers fiddling with the volume
            simulation_collider.center = Vector3.zero;
            simulation_collider.size = Vector3.one;
            simulation_collider.isTrigger = true;
        }


        public void OnTriggerEnter(Collider _collision)
        {
            AddToSimulation(_collision);
        }


        public void OnTriggerStay(Collider _collision)
        {
            AddToSimulation(_collision);
        }


        public void OnTriggerExit(Collider _collision)
        {
            AddToSimulation(_collision);
        }


        private void AddToSimulation(Collider _collision)
        {
            if (fluid_simulation == null)
                return;

            SphereCollider sphere_collider = _collision.gameObject.GetComponent<SphereCollider>();

            if (sphere_collider == null)
                return;

            if (!fluid_simulation.SphereColliders.Contains(sphere_collider))
                fluid_simulation.SphereColliders.Add(sphere_collider);// If hit by a sphere collider, add it to the simulation
        }

    }
}
