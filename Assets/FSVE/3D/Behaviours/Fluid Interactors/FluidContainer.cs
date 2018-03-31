using UnityEngine;


namespace FSVE
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]// Ensure sphere collider
    public class FluidContainer : MonoBehaviour
    {
        /*
         * Whats this? an empty class? Sacrilege!
         * There isn't a way to extend standard collider components and fluid containers
         * are essentially just a sphere collider that has been somehow marked as a container.
         * This class simply ensures that when this component is added, it has a corresponding
         * sphere collider the collision interactor can grab.
         * I then check if this component exists to determine how it will be applied to the simulation.
         */
    }
}
