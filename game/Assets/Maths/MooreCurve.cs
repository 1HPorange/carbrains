using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MooreCurve
{
    private enum Direction
    {
        Up,
        Right,
        Down,
        Left,
    }

    private List<Vector2> _points;

    private MooreCurve(List<Vector2> points)
    {
        if (null == points)
        {
            throw new ArgumentNullException(nameof(points));
        }

        _points = points;
    }

    public List<Vector2> Points => _points;

    public Vector2 Evaluate(float t)
    {
        t = Mathf.Clamp01(t);

        var fracIdx = t * (_points.Count - 1);
        var ceilIdx = Mathf.CeilToInt(fracIdx);

        if (ceilIdx > 0)
        {
            var floor = _points[ceilIdx - 1];
            var ceil = _points[ceilIdx];

            return Vector2.Lerp(floor, ceil, fracIdx % 1);
        }
        else
        {
            return _points[0];
        }
    }

    public Vector2 EvaluateLooped(float t)
    {
        t = Mathf.Clamp01(t);

        var fracIdx = t * _points.Count;
        var ceilIdx = Mathf.CeilToInt(fracIdx);

        if (ceilIdx == _points.Count)
        {
            var floor = _points.Last();
            var ceil = _points.First();

            return Vector2.Lerp(floor, ceil, fracIdx % 1);
        }
        else if (ceilIdx > 0)
        {
            var floor = _points[ceilIdx - 1];
            var ceil = _points[ceilIdx];

            return Vector2.Lerp(floor, ceil, fracIdx % 1);
        }
        else
        {
            return _points[0];
        }
    }

    public static MooreCurve Generate(uint degree)
    {
        var points = Axiom(degree);

        Rect bounds = new Rect();
        foreach (var point in points)
        {
            bounds.xMin = Mathf.Min(bounds.xMin, point.x);
            bounds.xMax = Mathf.Max(bounds.xMax, point.x);
            bounds.yMin = Mathf.Min(bounds.yMin, point.y);
            bounds.yMax = Mathf.Max(bounds.yMax, point.y);
        }
        var scale = new Vector2(bounds.width, bounds.height);
        var offset = new Vector2(bounds.xMin + bounds.xMax, bounds.yMin + bounds.yMax) / 2f;

        var pointsFloat = points.Select(v => new Vector2((v.x - offset.x) / scale.x, (v.y - offset.y) / scale.y)).ToList();

        return new MooreCurve(pointsFloat);
    }

    // via https://en.wikipedia.org/wiki/Moore_curve
    private static List<Vector2Int> Axiom(uint level)
    {
        var shouldRecurse = level > 0;

        var direction = Direction.Up;
        var points = new List<Vector2Int>();

        // Add starting position
        var cursor = new Vector2Int(0, 0);
        points.Add(cursor);

        // L
        if (shouldRecurse)
        {
            LRule(points, level - 1, ref direction);
        }
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // L
        if (shouldRecurse)
        {
            LRule(points, level - 1, ref direction);
        }
        // Turn right
        TurnRight(ref direction);
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // Turn right
        TurnRight(ref direction);
        // L
        if (shouldRecurse)
        {
            LRule(points, level - 1, ref direction);
        }
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // L
        if (shouldRecurse)
        {
            LRule(points, level - 1, ref direction);
        }

        return points;
    }

    private static void LRule(List<Vector2Int> points, uint level, ref Direction direction)
    {
        var shouldRecurse = level > 0;

        // Turn left
        TurnLeft(ref direction);
        // R
        if (shouldRecurse)
        {
            RRule(points, level - 1, ref direction);
        }
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // Turn right
        TurnRight(ref direction);
        // L
        if (shouldRecurse)
        {
            LRule(points, level - 1, ref direction);
        }
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // L
        if (shouldRecurse)
        {
            LRule(points, level - 1, ref direction);
        }
        // Turn right
        TurnRight(ref direction);
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // R
        if (shouldRecurse)
        {
            RRule(points, level - 1, ref direction);
        }
        // Turn Left
        TurnLeft(ref direction);
    }

    private static void RRule(List<Vector2Int> points, uint level, ref Direction direction)
    {
        var shouldRecurse = level > 0;

        // Turn right
        TurnRight(ref direction);
        // L
        if (shouldRecurse)
        {
            LRule(points, level - 1, ref direction);
        }
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // Turn left
        TurnLeft(ref direction);
        // R
        if (shouldRecurse)
        {
            RRule(points, level - 1, ref direction);
        }
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // R
        if (shouldRecurse)
        {
            RRule(points, level - 1, ref direction);
        }
        // Turn left
        TurnLeft(ref direction);
        // F
        points.Add(points.Last() + DirectionVector(direction));
        // L
        if (shouldRecurse)
        {
            LRule(points, level - 1, ref direction);
        }
        // Turn right
        TurnRight(ref direction);
    }

    private static Vector2Int DirectionVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return Vector2Int.up;
            case Direction.Right:
                return Vector2Int.right;
            case Direction.Down:
                return Vector2Int.down;
            case Direction.Left:
                return Vector2Int.left;
        }

        throw new ArgumentException();
    }

    private static void TurnRight(ref Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                direction = Direction.Right;
                break;
            case Direction.Right:
                direction = Direction.Down;
                break;
            case Direction.Down:
                direction = Direction.Left;
                break;
            case Direction.Left:
                direction = Direction.Up;
                break;
        }
    }

    private static void TurnLeft(ref Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                direction = Direction.Left;
                break;
            case Direction.Right:
                direction = Direction.Up;
                break;
            case Direction.Down:
                direction = Direction.Right;
                break;
            case Direction.Left:
                direction = Direction.Down;
                break;
        }
    }
}
