using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Maths
{
    /// <summary>
    /// Implemented via https://en.wikipedia.org/wiki/Non-uniform_rational_B-spline
    /// </summary>
    public class NurbsCurve : ILoopedSpline
    {
        /// <summary>
        /// Control points
        /// </summary>
        private Vector2[] _p;

        /// <summary>
        /// Control point weights
        /// </summary>
        private float[] _w;

        /// <summary>
        /// Knots
        /// </summary>
        private float[] _k;

        private int _n;

        private NurbsCurve(Vector2[] p, float[] w, float[] k, int n)
        {
            _p = p;
            _w = w;
            _k = k;
            _n = n;
        }

        public static NurbsCurve Generate(IList<Vector2> controlPoints, IList<float> weights, int degree)
        {
            if (controlPoints.Count < 2)
            {
                throw new ArgumentOutOfRangeException($"{nameof(controlPoints)} must have at least 2 members");
            }

            if (controlPoints.Count != weights.Count)
            {
                throw new ArgumentException($"{nameof(controlPoints)} and {nameof(weights)} must be of equal length");
            }

            if (degree < 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(degree)} must be >= 0");
            }

            if (weights.Any(w => Math.Abs(w) < float.Epsilon))
            {
                throw new ArgumentException("No entry of the weight vector can be 0");
            }

            var cp = controlPoints.Skip(controlPoints.Count - degree).Concat(controlPoints.Concat(controlPoints.Take(degree))).ToArray();
            var wg = weights.Skip(weights.Count - degree).Concat(weights.Concat(weights.Take(degree))).ToArray();

            var knotCount = cp.Length + degree + 1;
            var knots = Enumerable.Range(0, knotCount).Select(i => (float) i / (float) (knotCount - 1)).ToArray();

            return new NurbsCurve(cp, wg, knots, degree);
        }

        public Vector2 EvaluateAt(float u)
        {
            if (u < 0f || u > 1f)
            {
                throw new ArgumentOutOfRangeException($"{nameof(u)} must be in [0;1], was {u}");
            }

            // Shorten endpoints so there is no overlap
            var offset = (float)(_n + 1) / (float)(_k.Length - 1);
            u = Mathf.Lerp(offset, 1f - offset, u);

            var fraction = Enumerable.Range(0, _p.Length).Select(i =>
            {
                var b = N(i, _n, u) * _w[i];
                return new Tuple<Vector2, float>(_p[i] * b, b);
            }).Aggregate(new Tuple<Vector2, float>(Vector2.zero, 0f), (t1, t2) => new Tuple<Vector2, float>(t1.Item1 + t2.Item1, t1.Item2 + t2.Item2));

            return fraction.Item1 / fraction.Item2;
        }

        public List<Vector2> GetCorners(int detailHint)
        {
            Assert.IsTrue(detailHint > 0);

            return Enumerable.Range(0, detailHint)
                .Select(i => (float) i / (float) detailHint)
                .Select(EvaluateAt)
                .ToList();
        }

        private float N(int i, int n, float u)
        {
            if (n == 0)
            {
                return u >= _k[i] && u <= _k[i + 1] ? 1f : 0f;
            }
            else
            {
                return f(i, n, u) * N(i, n - 1, u) + g(i + 1, n, u) * N(i + 1, n - 1, u);
            }
        }

        private float f(int i, int n, float u)
        {
            return (u - _k[i]) / (_k[i + n] - _k[i]);
        }

        private float g(int i, int n, float u)
        {
            return (_k[i + n] - u) / (_k[i + n] - _k[i]);
        }
    }
}
