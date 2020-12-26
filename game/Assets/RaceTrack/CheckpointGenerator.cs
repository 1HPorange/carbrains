using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class CheckpointGenerator : MonoBehaviour
{
    [SerializeField]
    private RaceTrackGenerator _raceTrackGenerator = default;

    public const int NUM_CHECKPOINTS = 150;

    [SerializeField] private float _scale = 1f;

    private readonly List<GameObject> _checkpoints = new List<GameObject>();

    private void OnEnable()
    {
        Assert.IsTrue(NUM_CHECKPOINTS > 0);

        _raceTrackGenerator.RaceTrackGenerated.AddListener(GenerateCheckpoints);
    }

    private void OnDisable()
    {
        _raceTrackGenerator.RaceTrackGenerated.RemoveListener(GenerateCheckpoints);
    }

    private void GenerateCheckpoints(ILoopedSpline spline)
    {
        const float DELTA = 0.001f;

        DeleteAllCheckpoints();

        for (int i = 0; i < NUM_CHECKPOINTS; i++)
        {
            var t = (float) i / (float)NUM_CHECKPOINTS;
            var tBefore = (t - DELTA + 1f) % 1f;
            var tAfter = (t + DELTA) % 1f;

            var trackDirection = (spline.EvaluateAt(tAfter) - spline.EvaluateAt(tBefore)).normalized;
            var cpDirection = new Vector2(-trackDirection.y, trackDirection.x);

            var cp = new GameObject();
            cp.name = (i + 1).ToString();
            cp.transform.parent = transform;
            cp.transform.position = spline.EvaluateAt(t);
            cp.layer = gameObject.layer;

            var ec = cp.AddComponent<EdgeCollider2D>();
            ec.points = new[]
            {
                -cpDirection * _scale,
                cpDirection * _scale
            };
            ec.isTrigger = true;

            _checkpoints.Add(cp);
        }
    }

    private void DeleteAllCheckpoints()
    {
        _checkpoints.ForEach(Destroy);
        _checkpoints.Clear();
    }
}
