using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

[RequireComponent(typeof(RaceTrackGenerator), typeof(MeshFilter), typeof(MeshRenderer))]
public class RaceTrackRenderer : MonoBehaviour
{
    public UnityEvent OnRaceTrackRendered;

    [SerializeField]
    private float _width = 1f;

    [SerializeField] private int _cornerDetail = 16;

    [SerializeField] private float _borderWidth = 0.1f;

    [SerializeField] private int _detail = default;

    [SerializeField] private Material _asphalt = default;

    [SerializeField] private Material _border = default;

    private MeshFilter _trackBackground;

    private void OnEnable()
    {
        GetComponent<RaceTrackGenerator>().RaceTrackGenerated.AddListener(UpdateTrackVisuals);

        GetComponent<MeshRenderer>().material = _asphalt;

        var bg = transform.childCount == 0 ? new GameObject() : transform.GetChild(0).gameObject;
        bg.name = "Track background";
        bg.transform.parent = transform;
        bg.transform.position = transform.position + Vector3.forward * 0.1f;

        _trackBackground = bg.AddComponent<MeshFilter>();
        var mr = bg.AddComponent<MeshRenderer>();
        mr.material = _border;
    }

    private void OnDisable()
    {
        GetComponent<RaceTrackGenerator>().RaceTrackGenerated.RemoveListener(UpdateTrackVisuals);

{        if (null != _trackBackground)
        
            Destroy(_trackBackground);
            _trackBackground = null;
        }
    }

    private void UpdateTrackVisuals(ILoopedSpline spline)
    {
        var corners = spline.GetCorners(_detail);

        var foreground = CalculateRoadMesh(corners, _width);
        GetComponent<MeshFilter>().mesh = foreground;

        var background = CalculateRoadMesh(corners, _width + _borderWidth);
        _trackBackground.mesh = background;

        OnRaceTrackRendered.Invoke();
    }

    private Mesh CalculateRoadMesh(List<Vector2> corners, float width)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        foreach (var corner in corners)
        {
            AddCornerFan(vertices, triangles, corner, width);
        }

        foreach (var segment in corners.Zip(corners.Skip(1).Append(corners[0]), (c0, c1) => new Tuple<Vector2, Vector2>(c0, c1)))
        {
            AddStraight(vertices, triangles, segment.Item1, segment.Item2, width);
        }

        return new Mesh {vertices = vertices.ToArray(), triangles = triangles.ToArray()};
    }

    private void AddCornerFan(List<Vector3> vertices, List<int> triangles, Vector2 corner, float width)
    {
        // Center of the fan
        var centerIdx = vertices.Count;
        vertices.Add(corner);

        // Fan vertices
        var fanVertexAngleDelta = 360f / (float)_cornerDetail;
        var angle = 0f;
        for (int i = 0; i < _cornerDetail; i++)
        {
            vertices.Add((Vector3)corner + Quaternion.Euler(0f, 0f, angle) * Vector3.up * width);
            angle += fanVertexAngleDelta;
        }

        // Fan triangles
        for (int i = 1; i < _cornerDetail; i++)
        {
            triangles.Add(centerIdx + i + 1);
            triangles.Add(centerIdx + i);
            triangles.Add(centerIdx);
        }

        triangles.Add(centerIdx + 1);
        triangles.Add(centerIdx + _cornerDetail);
        triangles.Add(centerIdx);
    }

    private void AddStraight(List<Vector3> vertices, List<int> triangles, Vector2 start, Vector2 end, float width)
    {
        var direction = (end - start).normalized;
        var left = new Vector2(-direction.y, direction.x) * width;

        vertices.Add(start + left);
        vertices.Add(start - left);
        vertices.Add(end + left);
        vertices.Add(end - left);

        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 4);

        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);
    }
}
