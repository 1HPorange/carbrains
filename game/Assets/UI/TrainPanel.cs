using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

public class TrainPanel : MonoBehaviour
{
    // Events

    [Serializable]
    public class SpeedupEvent : UnityEvent<float> { }

    public SpeedupEvent OnSpeedupChanged;

    [Serializable]
    public class ToggleEvent : UnityEvent<bool> { }

    public ToggleEvent OnSaveAllToggled;

    [Serializable]
    public class CountEvent : UnityEvent<ulong> { }

    public CountEvent OnSaveTopToggled;

    public UnityEvent OnCancelled;

    public UnityEvent OnTrackSkipped;

    public UnityEvent OnStart;

    public UnityEvent OnStop;

    // UI Elements

    [SerializeField] private Text _speedupLabel = default;

    [SerializeField] private Button _speedupIncrButton = default;

    [SerializeField] private Button _speedupDecrButton = default;

    [SerializeField] private Toggle _saveAllToggle = default;

    [SerializeField] private Toggle _saveTopToggle = default;

    [SerializeField] private Button _cancelButton = default;

    [SerializeField] private Button _skipButton = default;

    [SerializeField] private Button _startStopButton = default;

    // Internals

    private int _speedup;

    private bool _isRunning;

    public void ResetToggles()
    {
        _saveAllToggle.isOn = false;
        _saveTopToggle.isOn = false;
    }

    public void Freeze()
    {
        GetComponent<CanvasGroup>().interactable = false;
    }

    public void Unfreeze()
    {
        GetComponent<CanvasGroup>().interactable = true;
    }

    private void Awake()
    {
        _speedup = 1;
        _speedupLabel.text = _speedup.ToString();

        _isRunning = false;

        _speedupIncrButton.onClick.AddListener(() =>
        {
            _speedup++;
            _speedupLabel.text = _speedup.ToString();
            OnSpeedupChanged.Invoke(_speedup);
        });

        _speedupDecrButton.onClick.AddListener(() =>
        {
            _speedup = Mathf.Max(1, _speedup - 1);
            _speedupLabel.text = _speedup.ToString();
            OnSpeedupChanged.Invoke(_speedup);
        });

        _saveAllToggle.onValueChanged.AddListener(v => OnSaveAllToggled.Invoke(v));

        _saveTopToggle.onValueChanged.AddListener(v => OnSaveTopToggled.Invoke(5));

        _cancelButton.onClick.AddListener(() =>
        {
            Freeze();

            if (_isRunning)
            {
                _startStopButton.GetComponentInChildren<Text>().text = "Start";
                OnStop.Invoke();

                _isRunning = false;
            }

            OnCancelled.Invoke();
        });

        _skipButton.onClick.AddListener(() => OnTrackSkipped.Invoke());

        _startStopButton.onClick.AddListener(() =>
        {
            _isRunning = !_isRunning;

            if (_isRunning)
            {
                _startStopButton.GetComponentInChildren<Text>().text = "Stop";
                OnStart.Invoke();
            }
            else
            {
                _startStopButton.GetComponentInChildren<Text>().text = "Start";
                OnStop.Invoke();
            }
        });

        Freeze();
    }
}
