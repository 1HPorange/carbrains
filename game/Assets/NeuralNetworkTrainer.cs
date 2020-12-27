using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Car;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class NeuralNetworkTrainer : MonoBehaviour
{
    [SerializeField]
    private double _lapTimeLimitSeconds = 60.0;

    [SerializeField]
    private RaceTrackGenerator _trackGen = default;

    [SerializeField]
    private GameObject _neuralCarPrefab = default ;

    private Population _population;

    [SerializeField]
    private ulong _membersToSave = 5;

    private void Start()
    {
        StartTraining("active_config.json", 3);

        Time.timeScale = 3f;
    }

    public void StartTraining(string configPath, int tracks)
    {
        StopAllCoroutines();

        Assert.IsNotNull(_trackGen);
        Assert.IsNotNull(_neuralCarPrefab);
        Assert.IsTrue(_lapTimeLimitSeconds > 0.0);
        Assert.IsTrue(tracks > 0);
        Assert.IsNotNull(configPath);

        // Create native population of neural networks
        _population = Population.GenerateFromConfig(configPath);

        // Spawn neural cars
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

        // Generate track seeds
        var trackSeeds = Enumerable.Range(0, tracks).Select(_ => Random.Range(0, int.MaxValue)).ToList();

        StartCoroutine(TrainingRoutine(cars, trackSeeds));
    }

    private IEnumerator TrainingRoutine(List<NeuralCarInputSource> cars, List<int> trackSeeds)
    {
        double[] fitness = new double[_population.Size];

        while (true)
        {
            foreach (var seed in trackSeeds)
            {
                _trackGen.Generate(seed);

                cars.ForEach(car => car.ResetRun());

                //yield return new WaitForSeconds(3);

                var lapStart = DateTime.Now;

                cars.ForEach(car => car.ActivateAndStartDriving());

                yield return new WaitUntil(() => 
                    cars.All(car => !car.IsActive) || 
                    (DateTime.Now - lapStart).TotalSeconds >= _lapTimeLimitSeconds ||
                    Input.GetKeyDown(KeyCode.N));

                for (int i = 0; i < cars.Count; i++)
                {
                    cars[i].DeactivateAndStall();

                    fitness[i] += (double) cars[i].Checkpoint / (double) CheckpointGenerator.NUM_CHECKPOINTS;

                    if (cars[i].LapFinishTime.HasValue)
                    {
                        fitness[i] += 1.0 - (cars[i].LapFinishTime.Value - lapStart).TotalSeconds / _lapTimeLimitSeconds;
                    }
                }

                // Prevent pressing N triggering a track skip multiple times
                yield return null;
            }

            if (_membersToSave > 0)
            {
                _population.SaveTopN($"Best-{_membersToSave}-{DateTime.Now.Ticks}.json", fitness, _membersToSave);
            }
            
            _population.Evolve(fitness);

            for (int i = 0; i < fitness.Length; i++)
            {
                fitness[i] = 0.0;
            }
        }
    }

    private void OnDestroy()
    {
        _population?.Dispose();
    }
}
