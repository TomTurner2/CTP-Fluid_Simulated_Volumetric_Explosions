using UnityEngine;


namespace Detonate
{ 
    [RequireComponent(typeof(FluidSmoke3D))]
    [DisallowMultipleComponent]
    public class FluidSmokeEmitterInteractor : MonoBehaviour
    {
        private FluidSmoke3D fluid_simulation = null;


        void Start()
        {
            fluid_simulation = GetComponent<FluidSmoke3D>();
        }


        void Update()
        {
            if (fluid_simulation == null)
                return;

            AddEmitters();
            RemoveEmitters();
        }


        private void OnDisable()
        {
            if (fluid_simulation == null)
                return;

            fluid_simulation.SmokeEmitters.Clear();
        }


        private void AddEmitters()
        {
            foreach (SmokeEmitter emitter in SmokeEmitter.emitters_in_scene)//for every emitter in the scene
            {
                if (fluid_simulation.SmokeEmitters.Contains(emitter))
                    continue;

                if (AABBCollisionCheck(emitter.transform.position))//add the emitter if it is within the simulation grid
                    fluid_simulation.SmokeEmitters.Add(emitter);
            }
        }


        private void RemoveEmitters()
        {
            for (int i = fluid_simulation.SmokeEmitters.Count-1; i >= 0; --i)
            {
                if (fluid_simulation.SmokeEmitters[i] == null)
                {
                    fluid_simulation.SmokeEmitters.RemoveAt(i);
                    continue;
                }

                if (!AABBCollisionCheck(fluid_simulation.SmokeEmitters[i].transform.position))
                    fluid_simulation.SmokeEmitters.RemoveAt(i);//remove any emitters outside of simulation
            }
        }


        private bool AABBCollisionCheck(Vector3 _emitter_position)
        {
            if (!(_emitter_position.x > fluid_simulation.transform.position.x -
                  fluid_simulation.transform.localScale.x * 0.5f))
                return false;//early return more efficent

            if (!(_emitter_position.x < fluid_simulation.transform.position.x +
                  fluid_simulation.transform.localScale.x * 0.5f))
                return false;

            if (!(_emitter_position.y > fluid_simulation.transform.position.y -
                  fluid_simulation.transform.localScale.y * 0.5f))
                return false;

            if (!(_emitter_position.y < fluid_simulation.transform.position.y +
                  fluid_simulation.transform.localScale.y * 0.5f))
                return false;

            if (!(_emitter_position.z > fluid_simulation.transform.position.z -
                  fluid_simulation.transform.localScale.z * 0.5f))
                return false;

            if (_emitter_position.z < fluid_simulation.transform.position.z +
                fluid_simulation.transform.localScale.z * 0.5f)
                return true;

            return false;
        }

    }
}
