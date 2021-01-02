using Assets.Car;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;

public class NeuralNetworkTrainer : MonoBehaviour
{
    // Events

    public UnityEvent OnTrackSwitch;

    public UnityEvent OnRoundSwitch;

    public UnityEvent OnPopulationCreated;

    public UnityEvent OnPopulationCreateFail;

    // Inspector

    [SerializeField] private RaceTrackGenerator _trackGen = default;

    [SerializeField] private GameObject _neuralCarPrefab = default;

    [SerializeField] private CheckpointGenerator _checkpointGenerator = default;

    [SerializeField] private LongRunningTimer _timer = default;

    [SerializeField] private double _flatFinishBonus = 0.2;

    // Internals

    private bool _evolveAfterRound;

    private List<NeuralCarInputSource> _neuralCars;

    private double? _lapStart;

    private TimeSpan?[] _trackRecords;

    // Publics

    public Population Population { get; private set; }

    public TimeSpan? CurrentLapTime => CalcCurrentLapTime();

    public TimeSpan? FastestLapTime => CalcFastestLapTime();

    public double Leniency => CalcLeniency();

    public int Generation { get; private set; }

    public int TrackIndex { get; private set; }

    public int TrackCount => TrackSeeds == null ? 1 : TrackSeeds.Length;

    public float NextRoundSpeedup { get; set; }

    public bool SaveAllAfterRound { get; set; }

    public ulong SaveBestAfterRound { get; set; }

    public bool SkipTrack { get; set; }

    private int[] _trackSeeds;

    public int[] TrackSeeds
    {
        get => _trackSeeds;
        set {
            _trackRecords = new TimeSpan?[value.Length];
            _trackSeeds = value;
        }
    }

    public TimeSpan? CurrentTrackRecord => _trackRecords?[TrackIndex];

    public void CreatePopulation(string configPath, string populationPath)
    {
        RemovePopulation(true);

        try
        {
            Assert.IsTrue(configPath != null || populationPath != null);

            if (populationPath == null)
            {
                Population = Population.GenerateFromConfig(configPath);
                _evolveAfterRound = true;
            }
            else
            {
                Population = Population.LoadFromFile(populationPath, configPath);
                _evolveAfterRound = null != configPath;
            }

            Generation = 1;

            OnPopulationCreated.Invoke();
        }
        catch
        {
            OnPopulationCreateFail.Invoke();
            throw;
        }
    }

    public void StartTraining()
    {
        StopTraining();

        Assert.IsNotNull(_trackGen);
        Assert.IsNotNull(_neuralCarPrefab);
        Assert.IsNotNull(TrackSeeds);
        Assert.IsTrue(TrackSeeds.Length > 0);
        Assert.IsNotNull(Population);
        Assert.IsNotNull(_timer);

        _neuralCars = SpawnCars();

        SkipTrack = false;
        TrackIndex = 0;

        StartCoroutine(TrainingRoutine());
    }

    public void StopTraining()
    {
        StopAllCoroutines();

        RemovePopulation(false);

        Time.timeScale = 1f;

        _lapStart = null;
    }

    private void Awake()
    {
        StopTraining();

        Generation = 1;
    }

    private IEnumerator TrainingRoutine()
    {
        double[] fitness = new double[Population.Size];

        while (true)
        {
            for (TrackIndex = 0; TrackIndex < _trackSeeds.Length; TrackIndex++)
            {
                _trackGen.Generate(_trackSeeds[TrackIndex]);

                _neuralCars.ForEach(car => car.ResetRun());

                _timer.Reset();
                _lapStart = _timer.Now;

                Time.timeScale = Mathf.Max(1f, NextRoundSpeedup);

                OnTrackSwitch.Invoke();

                // Start driving only on fixed update frames; maybe that does something good :)
                yield return new WaitForFixedUpdate();

                _neuralCars.ForEach(car => car.ActivateAndStartDriving());

                yield return new WaitUntil(() =>
                    _neuralCars.All(car => !car.IsActive) ||
                    Input.GetKeyDown(KeyCode.N) ||
                    SkipTrack);

                _neuralCars.ForEach(car => car.DeactivateAndStall());

                AddFitnessRatings(fitness);

                // Prevent pressing N triggering a track skip multiple times
                yield return null;

                // Prevent skip track from triggering multiple times
                SkipTrack = false;

                // Give the user a bit of time to see the results of the round
                yield return new WaitForSecondsRealtime(3f);

                UpdateTrackRecord();
            }

            // Square fitness
            for (int i = 0; i < fitness.Length; i++)
            {
                fitness[i] *= fitness[i];
            }

            if (SaveBestAfterRound > 0)
            {
                var bestFolder = GameFolders.EnsureGameFolder(GameFolders.POPULATIONS);
                var filePath = Path.Combine(bestFolder,
                    $"Best-{DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss")}-{string.Join("-", _trackSeeds)}.json");

                Population.SaveTopN(filePath, fitness, SaveBestAfterRound);

                SaveBestAfterRound = 0;
            }

            if (SaveAllAfterRound)
            {
                var populationFolder = GameFolders.EnsureGameFolder(GameFolders.POPULATIONS);
                var filePath = Path.Combine(populationFolder,
                    $"All-{DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss")}-{string.Join("-", _trackSeeds)}.json");

                Population.SaveAll(filePath);

                SaveAllAfterRound = false;
            }

            if (_evolveAfterRound)
            {
                if (fitness.All(f => f == 0.0))
                {
                    Debug.LogWarning("Fitness was zero for the enitre population. Skipping evolution");
                }
                else
                {
                    Population.Evolve(fitness);
                    Generation++;
                }
            }

            // Reset fitness
            for (int i = 0; i < fitness.Length; i++)
            {
                fitness[i] = 0.0;
            }

            OnRoundSwitch.Invoke();
        }
    }

    private void AddFitnessRatings(double[] fitness)
    {
        var fastestFinish = 0.0;
        var slowestFinish = 0.0;
        var fastestSlowestDelta = 0.0;

        try
        {
            fastestFinish = _neuralCars.Where(car => car.LapFinishTime.HasValue).Min(car => car.LapFinishTime.Value);
            slowestFinish = _neuralCars.Where(car => car.LapFinishTime.HasValue).Max(car => car.LapFinishTime.Value);
            fastestSlowestDelta = slowestFinish - fastestFinish;
        }
        catch { }

        var rateFinishTime = fastestSlowestDelta > 0.0 && // TODO: Make the 0.1f below configurable
                             _neuralCars.Count(car => car.LapFinishTime.HasValue) > Mathf.RoundToInt(0.1f * Population.Size);

        for (int i = 0; i < _neuralCars.Count; i++)
        {
            var added = (double)_neuralCars[i].Checkpoint / (double)CheckpointGenerator.NUM_CHECKPOINTS;

            if (_neuralCars[i].LapFinishTime.HasValue)
            {
                if (rateFinishTime)
                {
                    added += 0.5 * (1.0 - (_neuralCars[i].LapFinishTime.Value - fastestFinish) / fastestSlowestDelta);
                }

                added += _flatFinishBonus;
            }

            // Square fitness (per track) for faster learning
            added *= added;

            fitness[i] += added;
        }
    }

    private void UpdateTrackRecord()
    {
        if (FastestLapTime.HasValue && FastestLapTime.Value < (_trackRecords[TrackIndex] ?? TimeSpan.MaxValue))
        {
            _trackRecords[TrackIndex] = FastestLapTime;
        }
    }

    private List<NeuralCarInputSource> SpawnCars()
    {
        _neuralCarPrefab.SetActive(false);
        var cars = Enumerable.Range(0, (int)Population.Size).Select(idx =>
        {
            var go = Instantiate(_neuralCarPrefab, transform);

            var inputSource = go.GetComponent<NeuralCarInputSource>();
            inputSource.Initialize(this, (ulong)idx, _checkpointGenerator, _timer);

            var respawner = go.GetComponent<SpawnOnStartLine>();
            respawner.TrackGen = _trackGen;

            go.SetActive(true);

            return inputSource;
        }).ToList();
        return cars;
    }

    private TimeSpan? CalcCurrentLapTime()
    {
        if (_lapStart.HasValue)
        {
            return TimeSpan.FromSeconds(_timer.Now - _lapStart.Value);
        }
        else
        {
            return null;
        }
    }

    private TimeSpan? CalcFastestLapTime()
    {
        if (_lapStart.HasValue)
        {
            try
            {
                var bestLapTime = _neuralCars.Where(car => car.LapFinishTime.HasValue).Min(car => car.LapFinishTime.Value);
                return TimeSpan.FromSeconds(bestLapTime - _lapStart.Value);
            }
            catch
            {
                return null;
            }

        }
        else
        {
            return null;
        }
    }

    private bool _recalculateLeniency = true;

    private double _leniency = 1.0;

    private double CalcLeniency()
    {
        if (_recalculateLeniency)
        {
            if (null != _neuralCars)
            {
                // TODO: Make this formula configurable
                _leniency = Math.Max(0.0, 1.0 - (4.0 * (double)_neuralCars.Count(car => car.LapFinishTime.HasValue)) / (double)_neuralCars.Count);
            }
            else
            {
                _leniency = 1.0;
            }

            _recalculateLeniency = false;
        }

        return _leniency;
    }

    private void RemovePopulation(bool dispose)
    {
        _neuralCars?.ForEach(car => Destroy(car.gameObject));
        _neuralCars = null;

        if (dispose)
        {
            Population?.Dispose();
            Population = null;
        }
    }

    private void FixedUpdate()
    {
        _recalculateLeniency = true;
    }

    private void OnDestroy()
    {
        Population?.Dispose();
        Population = null;
    }
}
