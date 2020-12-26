using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceTrackBoundaryGenerator : MonoBehaviour
{
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
    }
}
