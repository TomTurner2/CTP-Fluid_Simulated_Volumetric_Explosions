using UnityEngine;


namespace FSVE
{
    public class FluidSimModule
    {
        protected const uint READ = 0; //for accessing grid sets
        protected const uint WRITE = 1;

        [SerializeField] protected ComputeShader compute_shader;

        public FluidSimModule() { }


        public FluidSimModule(ComputeShader _shader)
        {
            compute_shader = _shader;
        }



        protected void Swap(ComputeBuffer[] _grid)
        {
            ComputeBuffer temp = _grid[READ];
            _grid[READ] = _grid[WRITE];
            _grid[WRITE] = temp;
        }
    }
}
