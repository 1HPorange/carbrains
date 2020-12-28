using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GenerationLabel : MonoBehaviour
{
    [SerializeField] private Text _label = default;

    [SerializeField] private NeuralNetworkTrainer _trainer = default;

    private void Update()
    {
        _label.text = $"Gen {_trainer.Generation} ({_trainer.TrackIndex + 1}/{_trainer.TrackCount})";
    }
}
