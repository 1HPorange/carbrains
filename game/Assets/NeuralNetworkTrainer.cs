﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Car;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Random = UnityEngine.Random;

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

    // Internals

    private Population _population;

    private bool _evolveAfterRound;

    private List<NeuralCarInputSource> _neuralCars;

    private DateTime? _lapStart;

    // Publics

    public TimeSpan? CurrentLapTime => CalcCurrentLapTime();

    public TimeSpan? FastestLapTime => CalcFastestLapTime();

    public int Generation { get; private set; }

    public int TrackIndex { get; private set; }

    public int TrackCount => TrackSeeds == null ? 1 : TrackSeeds.Length;

    public float NextRoundSpeedup { get; set; }

    public bool SaveAllAfterRound { get; set; }

    public ulong SaveBestAfterRound { get; set; }

    public bool SkipTrack { get; set; }

    public int[] TrackSeeds { get; set; }

    public void CreatePopulation(string configPath, string populationPath)
    {
        RemovePopulation(true);

        try
        {
            Assert.IsTrue(configPath != null || populationPath != null);

            if (populationPath == null)
            {
                _population = Population.GenerateFromConfig(configPath);
                _evolveAfterRound = true;
            }
            else
            {
                _population = Population.LoadFromFile(populationPath, configPath);
                _evolveAfterRound = false;
            }

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
        Assert.IsNotNull(_population);

        _neuralCars = SpawnCars();

        SkipTrack = false;
        Generation = 1;
        TrackIndex = 0;

        StartCoroutine(TrainingRoutine(_population, _neuralCars, TrackSeeds, _evolveAfterRound));
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

    private IEnumerator TrainingRoutine(Population population, List<NeuralCarInputSource> cars, int[] trackSeeds, bool evolve)
    {
        double[] fitness = new double[_population.Size];

        while (true)
        {
            for (TrackIndex = 0; TrackIndex < trackSeeds.Length; TrackIndex++)
            {
                _trackGen.Generate(trackSeeds[TrackIndex]);

                cars.ForEach(car => car.ResetRun());

                _lapStart = DateTime.Now;

                Time.timeScale = Mathf.Max(1f, NextRoundSpeedup);

                OnTrackSwitch.Invoke();

                // Start driving
                cars.ForEach(car => car.ActivateAndStartDriving());

                yield return new WaitUntil(() =>
                    cars.All(car => !car.IsActive) ||
                    Input.GetKeyDown(KeyCode.N) ||
                    SkipTrack);

                cars.ForEach(car => car.DeactivateAndStall());

                CalculateFitness(cars, fitness);

                // Prevent pressing N triggering a track skip multiple times
                yield return null;

                // Prevent skip track from triggering multiple times
                SkipTrack = false;
            }

            // Square fitness for faster learning
            for (int  i = 0;  i < fitness.Length;  i++)
            {
                fitness[i] *= fitness[i];
            }

            if (SaveBestAfterRound > 0)
            {
                var bestFolder = GameFolders.EnsureGameFolder(GameFolders.BEST);
                var filePath = Path.Combine(bestFolder,
                    $"Best-{string.Join("-", trackSeeds)}-{DateTime.Now.Ticks}.json");

                _population.SaveTopN(filePath, fitness, SaveBestAfterRound);

                SaveBestAfterRound = 0;
            }

            if (SaveAllAfterRound)
            {
                var populationFolder = GameFolders.EnsureGameFolder(GameFolders.POPULATIONS);
                var filePath = Path.Combine(populationFolder,
                    $"{string.Join("-", trackSeeds)}-{DateTime.Now.Ticks}.json");

                _population.SaveAll(filePath);

                SaveAllAfterRound = false;
            }

            if (evolve)
            {
                _population.Evolve(fitness);
            }

            Generation++;

            // Reset fitness
            for (int i = 0; i < fitness.Length; i++)
            {
                fitness[i] = 0.0;
            }

            OnRoundSwitch.Invoke();
        }
    }

    private void CalculateFitness(List<NeuralCarInputSource> cars, double[] fitness)
    {
        var fastestFinish = DateTime.MinValue;
        var slowestFinish = DateTime.MinValue;
        var fastestSlowestDelta = 0.0;

        try
        {
            fastestFinish = cars.Where(car => car.LapFinishTime.HasValue).Min(car => car.LapFinishTime.Value);
            slowestFinish = cars.Where(car => car.LapFinishTime.HasValue).Max(car => car.LapFinishTime.Value);
            fastestSlowestDelta = (slowestFinish - fastestFinish).TotalSeconds * Time.timeScale;
        }
        catch { }

        var rateFinishTime = fastestSlowestDelta > 0.0 && // TODO: Make the 0.1f below configurable
                             cars.Count(car => car.LapFinishTime.HasValue) > Mathf.RoundToInt(0.1f * _population.Size);

        for (int i = 0; i < cars.Count; i++)
        {
            fitness[i] += (double)cars[i].Checkpoint / (double)CheckpointGenerator.NUM_CHECKPOINTS;

            if (cars[i].LapFinishTime.HasValue && rateFinishTime)
            {
                fitness[i] += 1.0 - (cars[i].LapFinishTime.Value - fastestFinish).TotalSeconds * Time.timeScale / fastestSlowestDelta;
            }
        }
    }

    private List<NeuralCarInputSource> SpawnCars()
    {
        _neuralCarPrefab.SetActive(false);
        var cars = Enumerable.Range(0, (int)_population.Size).Select(idx =>
        {
            var go = Instantiate(_neuralCarPrefab);

            var inputSource = go.GetComponent<NeuralCarInputSource>();
            inputSource.Initialize(_population, (ulong)idx);

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
            return TimeSpan.FromSeconds((DateTime.Now - _lapStart.Value).TotalSeconds * Time.timeScale);
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
                return TimeSpan.FromSeconds((bestLapTime - _lapStart.Value).TotalSeconds * Time.timeScale);
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

    private void RemovePopulation(bool dispose)
    {
        _neuralCars?.ForEach(car => Destroy(car.gameObject));
        _neuralCars = null;

        if (dispose)
        {
            _population?.Dispose();
            _population = null;
        }
    }

    private void OnDestroy()
    {
        _population?.Dispose();
        _population = null;
    }
}
