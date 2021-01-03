using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LoopedPolygonSpline : ILoopedSpline
{
    /// <summary>
    /// The first corner is duplicated and appended to the end of this collection.
    /// </summary>
    private List<Vector2> _corners;

    /// <summary>
    /// <see cref="_distances"/>[0] is the distance between corner 0 and corner 1
    /// </summary>
    private List<float> _distances;

    private float _circumference;

    /// <summary>
    /// Corners must not coincide, including the first and last one.
    /// The loop is closed internally.
    /// </summary>
    public LoopedPolygonSpline(List<Vector2> corners)
    {
        if (corners.Count < 3)
        {
            throw new ArgumentException($"{nameof(corners)} has less than 3 members");
        }

        _corners = corners.Append(corners[0]).ToList();

        _distances = new List<float>();
        _circumference = 0f;

        for (int i = 0; i < _corners.Count - 1; i++)
        {
            var distance = Vector2.Distance(_corners[i], _corners[i + 1]);

            _distances.Add(distance);
            _circumference += distance;
        }
    }

    public Vector2 EvaluateAt(float t)
    {
        if (t < 0f || t > 1f)
        {
            throw new ArgumentOutOfRangeException($"{nameof(t)} must be between 0 and 1 (inclusive)");
        }

        var targetDistance = t * _circumference;

        var distanceIndex = 0;
        var distanceSoFar = 0f;
        for (; distanceIndex < _distances.Count; distanceIndex++)
        {
            var nextCornerAt = distanceSoFar + _distances[distanceIndex];
            if (nextCornerAt > targetDistance)
            {
                distanceSoFar = nextCornerAt;
            }
            else
            {
                break;
            }
        }

        var sectorT = (targetDistance - distanceSoFar) / _distances[distanceIndex];

        return Vector2.Lerp(_corners[distanceIndex], _corners[distanceIndex + 1], sectorT);
    }

    /// <summary>
    /// Only returns the first point once, not completing the loop
    /// </summary>
    /// <param name="detailHint"></param>
    public List<Vector2> GetCorners(int detailHint)
    {
        return new List<Vector2>(_corners);
    }
}
