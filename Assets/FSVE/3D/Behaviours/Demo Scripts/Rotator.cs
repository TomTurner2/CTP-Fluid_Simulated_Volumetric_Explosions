using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FSVE
{
    namespace Demo
    {
        public class Rotator : MonoBehaviour
        {
            public float rotation_speed = -50;
            public Vector3 rotation_axis = Vector3.up;

            private bool rotate = true;

            void Update()
            {
                if (rotate)
                    transform.Rotate(rotation_axis.normalized * Time.deltaTime * rotation_speed);
            }


            public void ToggleActive()
            {
                rotate = !rotate;
            }
        }
    }
}
