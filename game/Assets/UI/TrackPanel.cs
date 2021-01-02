using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TrackPanel : MonoBehaviour
{
    // Events

    [Serializable]
    public class TrackSeedEvent : UnityEvent<int?> { }

    public TrackSeedEvent OnGenerateTrackRequest;

    public UnityEvent OnCancelled;

    [Serializable]
    public class TrackSeedCollectionEvent : UnityEvent<int?[]> { }

    public TrackSeedCollectionEvent OnTrackSeedsSelected;

    // UI Elements

    [SerializeField] private Transform _trackItemParent = default;

    [SerializeField] private GameObject _trackItemPrefab = default;

    [SerializeField] private InputField _seedInput = default;

    [SerializeField] private Button _generateButton = default;

    [SerializeField] private Button _addButton = default;

    [SerializeField] private Button _cancelButton = default;

    [SerializeField] private Button _proceedButton = default;

    [SerializeField] private Button _addRandomButton = default;

    // Internals

    private List<TrackSeedItem> _trackSeeds;

    public void Unfreeze()
    {
        GetComponent<CanvasGroup>().interactable = true;

        _trackSeeds.ForEach(s => s.GetComponent<CanvasGroup>().interactable = true);

        GenerateNewTrack();
    }

    public void Freeze()
    {
        GetComponent<CanvasGroup>().interactable = false;

        _trackSeeds.ForEach(s => s.GetComponent<CanvasGroup>().interactable = false);
    }

    public void RemoveTrackItem(int siblingIndex)
    {
        try
        {
            var item = _trackSeeds.SingleOrDefault(s => s.transform.GetSiblingIndex() == siblingIndex);
            Destroy(item.gameObject);
            _trackSeeds.Remove(item);
        }
        catch 
        {

        }
    }

    public void ViewTrack(int? seed)
    {
        OnGenerateTrackRequest.Invoke(seed);
    }

    public void GenerateNewTrack()
    {
        Random.InitState(Environment.TickCount);
        var seed = Random.Range(0, int.MaxValue);
        _seedInput.text = seed.ToString();
    }

    private void Awake()
    {
        _trackSeeds = new List<TrackSeedItem>();

        _seedInput.onValueChanged.AddListener(s =>
        {
            try
            {
                var seed = int.Parse(s);
                OnGenerateTrackRequest.Invoke(seed);
            }
            catch { }
        });

        _generateButton.onClick.AddListener(GenerateNewTrack);

        _addButton.onClick.AddListener(() =>
        {
            try
            {
                var seed = int.Parse(_seedInput.text);
                AddTrackItem(seed);

                GenerateNewTrack();
            }
            catch { }
        });

        _addRandomButton.onClick.AddListener(() =>
        {
            AddTrackItem(null);
        });

        _cancelButton.onClick.AddListener(() =>
        {
            Freeze();
            OnCancelled.Invoke();
        });

        _proceedButton.onClick.AddListener(() =>
        {
            Freeze();
            OnTrackSeedsSelected.Invoke(_trackSeeds.Select(item => item.TrackSeed).ToArray());
        });

        Freeze();
    }

    private void Update()
    {
        _proceedButton.interactable = _trackSeeds.Count > 0;
    }

    private void AddTrackItem(int? seed)
    {
        _trackItemPrefab.SetActive(false);
        var itemGameObject = Instantiate(_trackItemPrefab, _trackItemParent);
        var item = itemGameObject.GetComponent<TrackSeedItem>();
        item.TrackSeed = seed;
        item.TrackPanel = this;

        _trackSeeds.Add(item);

        // Move item to correct sibling index
        itemGameObject.transform.SetSiblingIndex(transform.GetSiblingIndex() + _trackSeeds.Count);

        itemGameObject.SetActive(true);
    }
}
