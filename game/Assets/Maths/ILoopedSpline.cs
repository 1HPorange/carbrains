using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILoopedSpline
{
    /// <summary>
    /// Gets an interpolated position along the spline.
    /// *d distance / d t* is constant.
    /// <see cref="EvaluateAt"/>(0f) == <see cref="EvaluateAt"/>(1f).
    /// Throws when t not in [0;1] (inclusive)
    /// </summary>
    Vector2 EvaluateAt(float t);

    /// <summary>
    /// If the spline can be represented as a list of corners (= only consists of straight lines),
    /// this method returns the corners. Otherwise it returns null.
    /// </summary>
    /// <param name="detailHint"></param>
    List<Vector2> GetCorners(int detailHint = 40);
}
