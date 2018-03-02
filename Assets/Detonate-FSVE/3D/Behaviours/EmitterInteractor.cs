using UnityEngine;


namespace Detonate
{ 
    [RequireComponent(typeof(FluidSim3D))]
    public class FluidEmitterInteractor : MonoBehaviour
    {
        FluidSim3D fluid_simulation = null;

        void Start()
        {
            fluid_simulation = GetComponent<FluidSim3D>();
        }


        void Update()
        {
            if (fluid_simulation == null)
                return;

            AddEmitters();
            RemoveEmitters();
        }


        private void AddEmitters()
        {
            foreach (FluidEmitter emitter in FluidEmitter.emitters_in_scene)//for every emitter in the scene
            {
                if (fluid_simulation.Emitters.Contains(emitter))
                    continue;

                if (AABBCollisionCheck(emitter.transform.position))//add the emitter if it is within the simulation grid
                    fluid_simulation.Emitters.Add(emitter);
            }
        }


        private void RemoveEmitters()
        {
            for (int i = 0; i < fluid_simulation.Emitters.Count; ++i)
            {
                if (!AABBCollisionCheck(fluid_simulation.Emitters[i].transform.position))
                    fluid_simulation.Emitters.RemoveAt(i);//remove any emitters outside of simulation
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

            if (_emitter_position.z < fluid_simulation.transform.position.z + fluid_simulation.transform.localScale.z * 0.5f)
                return true;

            return false;
        }

    }
}
