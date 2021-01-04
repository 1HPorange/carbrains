using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Maths;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class RaceTrackGenerator : MonoBehaviour
{
    public const float RADIUS = 5f;
    public const float DIAMETER = 2f * RADIUS;

    [Serializable]
    public class RaceTrackGeneratedEvent : UnityEvent<ILoopedSpline> { }

    public RaceTrackGeneratedEvent RaceTrackGenerated;

    [SerializeField]
    private int _mooreDegree = 2;

    [SerializeField]
    private int _minSkip = 3;

    [SerializeField]
    private int _maxSkip = 8;

    [SerializeField]
    private int _splineDegree = 2;

    [SerializeField]
    private float _minNodeWeight = 0.5f;

    [SerializeField]
    private float _maxNodeWeight = 1f;

    [SerializeField] private bool _skipMoorePoints = true;

    [SerializeField] private bool _makeSpline = true;

    [SerializeField] private bool _randomNodeWeights = true;

    private ILoopedSpline _racetrack;

    [SerializeField]
    private bool _regenerate = false;

    private void Update()
    {
        if (_regenerate)
        {
            GenerateInternal(Environment.TickCount);

            RaceTrackGenerated.Invoke(_racetrack);

            _regenerate = false;
        }
    }

    public void Generate(int? seedHint)
    {
        var seed = 0;

        if (seedHint.HasValue)
        {
            seed = seedHint.Value;
        }
        else
        {
            Random.InitState(Environment.TickCount);
            seed = Random.Range(int.MinValue, int.MaxValue);
        }

        GenerateInternal(seed);

        RaceTrackGenerated.Invoke(_racetrack);
    }

    private void GenerateInternal(int seed)
    {
        Assert.IsTrue(_mooreDegree >= 0);
        Assert.IsTrue(_minSkip >= 1 && _minSkip <= _maxSkip);
        Assert.IsTrue(_maxSkip < int.MaxValue);
        Assert.IsTrue(_minNodeWeight > 0f);
        Assert.IsTrue(_minNodeWeight <= _maxNodeWeight);
        Assert.IsTrue(_splineDegree >= 0);
        
        Random.InitState(seed);
        var moorePoints = MooreCurve.Generate((uint)_mooreDegree).Points;

        if (_skipMoorePoints)
        {
            Random.InitState(seed);
            var pointsWithSkips = new List<Vector2>();

            // First point is always added, as it should be
            for (int i = 0; i < moorePoints.Count; i += Random.Range(_minSkip, _maxSkip + 1))
            {
                pointsWithSkips.Add(moorePoints[i]);
            }

            moorePoints = pointsWithSkips;
        }

        for (int i = 0; i < moorePoints.Count; i++)
        {
            moorePoints[i] *= DIAMETER;
        }

        if (!_makeSpline)
        {
            _racetrack = new LoopedPolygonSpline(moorePoints);
            return;
        }

        Random.InitState(seed);
        var controlPointWeights = Enumerable.Range(0, moorePoints.Count)
            .Select(_ => _randomNodeWeights ? Random.Range(_minNodeWeight, _maxNodeWeight) : 1f)
            .ToList();
        
        _racetrack = NurbsCurve.Generate(moorePoints, controlPointWeights, _splineDegree, (seed & 1) == 1);
    }
}
