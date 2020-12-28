using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class RaceTrackBoundaryGenerator : MonoBehaviour
{
    public UnityEvent OnColliderGenFailed;

    [SerializeField]
    private RaceTrackContourExtractor _contourExtractor = default;

    [SerializeField]
    private EdgeCollider2D _outer = default, _inner = default;

    private void OnEnable()
    {
        _contourExtractor.OnOuterInnerContoursExtracted.AddListener(CreateEdgeColliders);
    }

    private void OnDisable()
    {
        _contourExtractor.OnOuterInnerContoursExtracted.RemoveListener(CreateEdgeColliders);
    }

    private void CreateEdgeColliders(Vector2[] outer, Vector2[] inner)
    {
        _outer.points = outer.Append(outer[0]).ToArray();
        _inner.points = inner.Append(inner[0]).ToArray();

        if (_inner.bounds.size == Vector3.zero || _outer.bounds.size == Vector3.zero)
        {
            OnColliderGenFailed.Invoke();
        }
    }
}
