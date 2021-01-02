using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackSeedItem : MonoBehaviour
{
    public int? TrackSeed { get; set; }

    public TrackPanel TrackPanel { get; set; }

    [SerializeField]
    private Text _indexLabel = default;

    [SerializeField]
    private Text _seedLabel = default;

    [SerializeField]
    private Button _copyButton = default;

    [SerializeField]
    private Button _viewButton = default;

    [SerializeField]
    private Button _removeButton = default;

    private void Start()
    {
        _seedLabel.text = TrackSeed.HasValue ? TrackSeed.Value.ToString() : "Random";

        if (TrackSeed.HasValue)
        {
            _copyButton.onClick.AddListener(() => GUIUtility.systemCopyBuffer = TrackSeed.ToString());
            _viewButton.onClick.AddListener(() => TrackPanel.ViewTrack(TrackSeed));
        }
        else
        {
            Destroy(_copyButton.gameObject);
            Destroy(_viewButton.gameObject);
        }

        _removeButton.onClick.AddListener(() => TrackPanel.RemoveTrackItem(transform.GetSiblingIndex()));
    }

    private void Update()
    {
        _indexLabel.text = (transform.GetSiblingIndex() - TrackPanel.transform.GetSiblingIndex()).ToString();
    }
}
