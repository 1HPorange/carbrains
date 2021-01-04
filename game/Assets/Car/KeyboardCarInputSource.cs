using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Car
{
    public class KeyboardCarInputSource : MonoBehaviour, ICarInputSource
    {
        public bool IsActive => true;

        public double ThrottleBreak => Input.GetAxis("Vertical");

        public double Steering => Input.GetAxis("Horizontal");

        private float maxSpeed = 0f;

        // Hacky fix to get around CarController not using FixedUpdate (due to our manual physics sim)
        public void FixedUpdate()
        {
            GetComponent<CarController>().AddForces();
            maxSpeed = Mathf.Max(maxSpeed, GetComponent<Rigidbody2D>().velocity.magnitude);
            print(maxSpeed);
        }
    }
}
