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

    public UnityEvent OnRoundSwitch;

    public UnityEvent OnPopulationCreated;

    public UnityEvent OnPopulationCreateFail;

    // Inspector

    [SerializeField] private RaceTrackGenerator _trackGen = default;

    [SerializeField] private GameObject _neuralCarPrefab = default;

    [SerializeField] private CheckpointGenerator _checkpointGenerator = default;

    [SerializeField] private LongRunningTimer _timer = default;

    [SerializeField] private RaceTrackBoundaryGenerator _boundaryGenerator = default;

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

    public double Leniency { get; private set; }

    public int Generation { get; private set; }

    public int TrackIndex { get; private set; }

    public int TrackCount => TrackSeeds == null ? 1 : TrackSeeds.Length;

    public float? NextRoundSpeedup { get; set; }

    public bool SaveAllAfterRound { get; set; }

    public ulong SaveBestAfterRound { get; set; }

    /// <summary>
    /// Note that speed bonuses are squared regardless of this setting
    /// </summary>
    public bool SquareFitnessAfterTrack { get; set; }

    public bool SquareFitnessAfterSet { get; set; }

    public bool SkipTrack { get; set; }

    private int?[] _trackSeeds;

    public int?[] TrackSeeds
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
        Assert.IsNotNull(_boundaryGenerator);

        _neuralCars = SpawnCars();

        SkipTrack = false;
        TrackIndex = 0;

        Physics2D.autoSimulation = false;

        StartCoroutine(TrainingRoutine());
    }

    public void StopTraining()
    {
        StopAllCoroutines();

        Physics2D.autoSimulation = true;

        RemovePopulation(false);

        _lapStart = null;
    }

    private void Awake()
    {
        Generation = 1;
        NextRoundSpeedup = 1;
        _lapStart = null;
        Leniency = 1.0;
        SquareFitnessAfterTrack = true;
        SquareFitnessAfterSet = true;
    }

    private IEnumerator TrainingRoutine()
    {
        // Wait one frame so all cars are properly spawned in and initialized
        yield return null;

        double[] fitness = new double[Population.Size];
        double[] speedBonusFitness = new double[Population.Size];
        int[] tracksFinished = new int[Population.Size];

        while (true)
        {
            for (TrackIndex = 0; TrackIndex < _trackSeeds.Length; TrackIndex++)
            {
                // Prepate track and cars
                // -------------------------------------------------------------------------
                _trackGen.Generate(_trackSeeds[TrackIndex]);

                _timer.Reset();
                _lapStart = _timer.Now;

                ReculculateLeniency();

                // Give the colliders some time to settle
                yield return new WaitUntil(() => _boundaryGenerator.HasSettled);

                _neuralCars.ForEach(car => car.ResetRun());

                // Run the simulation for the current track
                // -------------------------------------------------------------------------
                _neuralCars.ForEach(car => car.ActivateAndStartDriving());

                var realtimeWithoutPhysicsFrame = 0f;
                while (!(_neuralCars.All(car => !car.IsActive) ||
                         Input.GetKeyDown(KeyCode.N) ||
                         SkipTrack))
                {
                    if (NextRoundSpeedup.HasValue)
                    {
                        // Calculate physics based on real time
                        while (realtimeWithoutPhysicsFrame >= Time.fixedDeltaTime)
                        {
                            AdvanceTimestep();

                            realtimeWithoutPhysicsFrame -= Time.fixedUnscaledDeltaTime;
                        }
                    }
                    else
                    {
                        // Calculate physics as fast as we can render
                        AdvanceTimestep();
                    }

                    // Advance frame
                    yield return null;
                    realtimeWithoutPhysicsFrame += Time.unscaledDeltaTime * (NextRoundSpeedup ?? 1f);
                }

                _neuralCars.ForEach(car => car.DeactivateAndStall());

                // Evaluate the simulation for the current track
                // -------------------------------------------------------------------------
                AddFitnessRatings(fitness, speedBonusFitness, tracksFinished);

                // Prevent pressing N triggering a track skip multiple times
                yield return null;

                // Prevent skip track from triggering multiple times
                SkipTrack = false;

                // Give the user a bit of time to see the results of the round
                yield return new WaitForSecondsRealtime(0.5f);

                UpdateTrackRecord();
            }

            // Post-process fitness after a complete set of tracks
            var minTracksFinishedForBonus = (TrackSeeds.Length + 1) / 2; // TODO: Make configurable
            for (int i = 0; i < _neuralCars.Count; i++)
            {
                // Apply speed-bonus fitness if applicable
                if (tracksFinished[i] >= minTracksFinishedForBonus)
                {
                    fitness[i] += speedBonusFitness[i];
                }

                if (SquareFitnessAfterSet)
                {
                    fitness[i] *= fitness[i];
                }
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
                speedBonusFitness[i] = 0.0;
                tracksFinished[i] = 0;
            }

            OnRoundSwitch.Invoke();
        }
    }

    private void AdvanceTimestep()
    {
        _neuralCars.ForEach(car => car.AdvanceTimestep());
        Physics2D.Simulate(Time.fixedDeltaTime);
        _timer.AdvanceTimestep();
        ReculculateLeniency();
    }

    private void AddFitnessRatings(double[] fitness, double[] speedBonusFitness, int[] tracksFinished)
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

        // Only rate finish times if a minimum amount of cars finished with different times
        var rateFinishTime = fastestSlowestDelta > 0.0 && // TODO: Make the 0.1f below configurable
                             _neuralCars.Count(car => car.LapFinishTime.HasValue) > Mathf.RoundToInt(0.1f * Population.Size);

        for (int i = 0; i < _neuralCars.Count; i++)
        {
            var added = (double)_neuralCars[i].Checkpoint / (double)CheckpointGenerator.NUM_CHECKPOINTS;

            if (_neuralCars[i].LapFinishTime.HasValue)
            {
                if (rateFinishTime)
                {
                    // TODO: Make this formula configurable

                    // Range: 0-1 depending on how fast you were (linear)
                    var speedBonusStrength =
                        1.0 - (_neuralCars[i].LapFinishTime.Value - fastestFinish) / fastestSlowestDelta;

                    // We square it so small time saves get bigger bonuses
                    speedBonusFitness[i] += 0.5 * speedBonusStrength * speedBonusStrength;
                }

                tracksFinished[i]++;
                added += _flatFinishBonus;
            }

            if (SquareFitnessAfterTrack)
            {
                added *= added;
            }

            fitness[i] += added;
        }
    }

    private void UpdateTrackRecord()
    {
        if (FastestLapTime.HasValue && FastestLapTime.Value < (_trackRecords[TrackIndex] ?? TimeSpan.MaxValue) 
                                    && _trackSeeds?[TrackIndex] != null) // Fastest laps only make sense for non-random tracks
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

    private void ReculculateLeniency()
    {
        if (null != _neuralCars)
        {
            // TODO: Make this formula configurable
            Leniency = Math.Max(0.0, 1.0 - (double)_neuralCars.Count(car => car.LapFinishTime.HasValue) / ((double)_neuralCars.Count * 0.75f));
        }
        else
        {
            Leniency = 1.0;
        }
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

    private void OnDestroy()
    {
        Population?.Dispose();
        Population = null;
    }
}
