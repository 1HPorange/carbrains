using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

[RequireComponent(typeof(RaceTrackRenderer))]
public class RaceTrackShapeRenderer : MonoBehaviour
{
    [Serializable]
    public class TextureUnityEvent : UnityEvent<Texture2D> { }

    public TextureUnityEvent OnTextureGenerated;

    [SerializeField]
    private LayerMask _renderLayers = default;

    [SerializeField] private Shader _shader = default;

    [SerializeField] private int _textureHeight = 1024;

    [SerializeField]
    private Camera _raceTrackCamera = default;

    private bool _renderScheduled = false;

    [SerializeField]
    private RenderTexture _bufferTexture;

    private Texture2D _outputTexture;

    private void OnEnable()
    {
        Assert.IsNotNull(_shader);
        Assert.IsNotNull(_raceTrackCamera);

        GetComponent<RaceTrackRenderer>().OnRaceTrackRendered.AddListener(RenderShapeToTexture);
    }

    private void OnDisable()
    {
        GetComponent<RaceTrackRenderer>().OnRaceTrackRendered.RemoveListener(RenderShapeToTexture);
    }

    private void RenderShapeToTexture()
    {
        _renderScheduled = true;
    }

    private void Update()
    {
        if (!_renderScheduled)
        {
            return;
        }
        _renderScheduled = false;

        var oldBgColor = _raceTrackCamera.backgroundColor;
        var oldCullingMask = _raceTrackCamera.cullingMask;
        var oldTargetTexture = _raceTrackCamera.targetTexture;

        EnsureTextures(_raceTrackCamera);

        _raceTrackCamera.backgroundColor = Color.clear;
        _raceTrackCamera.cullingMask = _renderLayers;
        _raceTrackCamera.targetTexture = _bufferTexture;

        _raceTrackCamera.RenderWithShader(_shader, null);

        _raceTrackCamera.backgroundColor = oldBgColor;
        _raceTrackCamera.cullingMask = oldCullingMask;
        _raceTrackCamera.targetTexture = oldTargetTexture;

        CopyTextureFromGpu(_raceTrackCamera);

        OnTextureGenerated.Invoke(_outputTexture);
    }

    private void EnsureTextures(Camera cam)
    {
        if (null == _bufferTexture)
        {
            _bufferTexture = new RenderTexture(Mathf.RoundToInt(cam.aspect * _textureHeight), _textureHeight, 0);
        }

        if (null == _outputTexture)
        {
            _outputTexture = new Texture2D(Mathf.RoundToInt(cam.aspect * _textureHeight), _textureHeight, TextureFormat.R8, false);
        }
    }

    private void CopyTextureFromGpu(Camera cam)
    {
        var trt = RenderTexture.active;
        RenderTexture.active = _bufferTexture;

        _outputTexture.ReadPixels(new Rect(0f, 0f, Mathf.Round(cam.aspect * _textureHeight), _textureHeight), 0, 0);
        _outputTexture.Apply();

        RenderTexture.active = trt;
    }
}
