using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vefects.RainbowFlare
{
    public class Demo_TurningAround : MonoBehaviour
    {
        public float rotSpeed_X;
        public float rotSpeed_Y;
        public float rotSpeed_Z;

        public float globalSpeed = 1f;

        void Update()
        {
            transform.Rotate(new Vector3(rotSpeed_X, rotSpeed_Y, rotSpeed_Z) * globalSpeed * Time.deltaTime);
        }
    }
}
