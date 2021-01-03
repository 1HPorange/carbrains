using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnOnStartLine : MonoBehaviour
{
    public RaceTrackGenerator TrackGen;

    private Vector2 _startPos;
    private Quaternion _orientation;

    private void OnEnable()
    {
        TrackGen.RaceTrackGenerated.AddListener(OnTrackGenerated);
    }

    private void OnDisable()
    {
        TrackGen.RaceTrackGenerated.RemoveListener(OnTrackGenerated);
    }

    private void OnTrackGenerated(ILoopedSpline spline)
    {
        _startPos = spline.EvaluateAt(0f);

        var direction = (spline.EvaluateAt(0.001f) - _startPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _orientation = Quaternion.AngleAxis(angle, Vector3.forward) * Quaternion.Euler(Vector3.back * 90f);

        Respawn();
    }

    public void Respawn()
    {
        transform.position = _startPos;
        transform.rotation = _orientation;
    }
}
