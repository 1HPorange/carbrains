using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Car
{
    public interface ICarInputSource
    {
        bool IsActive { get; }

        double ThrottleBreak { get; }

        double Steering { get; }
    }

    class DummyCarInputSource : ICarInputSource
    {
        public bool IsActive => false;

        public double ThrottleBreak => 0;

        public double Steering => 0;
    }
}
