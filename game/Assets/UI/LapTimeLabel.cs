using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LapTimeLabel : MonoBehaviour
{
    [SerializeField] private Text _label = default;

    [SerializeField] private NeuralNetworkTrainer _trainer = default;

    private void Update()
    {
        _label.text = $"{FormatLapTime(_trainer.CurrentLapTime)} / <color=#285DFF>{FormatLapTime(_trainer.FastestLapTime)}</color>";
    }

    private string FormatLapTime(TimeSpan? lapTime)
    {
        if (lapTime.HasValue)
        {
            var t = lapTime.Value;
            return $"{t.Minutes.ToString("D2")}:{t.Seconds.ToString("D2")}:{t.Milliseconds.ToString("D3")}";
        }
        else
        {
            return "--:--:---";
        }
        
    }
}
