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
    }
}
