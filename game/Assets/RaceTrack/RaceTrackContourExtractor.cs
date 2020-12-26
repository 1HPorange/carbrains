using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

[RequireComponent(typeof(RaceTrackShapeRenderer))]
public class RaceTrackContourExtractor : MonoBehaviour
{
    [Serializable]
    public class OuterInnerContourEvent : UnityEvent<Vector2[], Vector2[]> { }

    public OuterInnerContourEvent OnOuterInnerContoursExtracted;

    [SerializeField] private Camera _raceTrackCamera = default;

    [SerializeField]
    private double _OuterEpsilon = 0.0005;

    [SerializeField]
    private double _InnerEpsilon = 0.0005;

    private void OnEnable()
    {
        Assert.IsNotNull(_raceTrackCamera);

        GetComponent<RaceTrackShapeRenderer>().OnTextureGenerated.AddListener(GenerateContour);
    }

    private void OnDisable()
    {
        GetComponent<RaceTrackShapeRenderer>().OnTextureGenerated.RemoveListener(GenerateContour);
    }

    private void GenerateContour(Texture2D shape)
    {
        using (var mat = new Mat(shape.height, shape.width, MatType.CV_8U, shape.GetRawTextureData()))
        using (var threshold = mat.Threshold(127, 255, ThresholdTypes.Binary))
        {
            Point[][] contours = null;
            HierarchyIndex[] hierarchyIndices = null;

            threshold.FindContours(out contours, out hierarchyIndices, RetrievalModes.CComp, ContourApproximationModes.ApproxTC89KCOS);

            Point[] outer, inner;
            if (hierarchyIndices[0].Child != -1)
            {
                outer = contours[0];
                inner = contours[1];
            }
            else
            {
                outer = contours[1];
                inner = contours[0];
            }

            //var outerEpsilon = _epsilon * Cv2.ArcLength(outer, true);
            outer = Cv2.ApproxPolyDP(outer, _OuterEpsilon, true);

            //var innerEpsilon = _epsilon * Cv2.ArcLength(inner, true);
            inner = Cv2.ApproxPolyDP(inner, _InnerEpsilon, true);

            // Determine factor to scale them bad boys up
            var bottomLeft = _raceTrackCamera.ViewportToWorldPoint(Vector3.zero);
            var topRight = _raceTrackCamera.ViewportToWorldPoint(Vector3.one);
            var scale = topRight - bottomLeft;

            OnOuterInnerContoursExtracted.Invoke(
                PointToVectorArray(outer, shape.width, shape.height, scale), 
                PointToVectorArray(inner, shape.width, shape.height, scale));
        }
    }

    private Vector2[] PointToVectorArray(Point[] points, int width, int height, Vector2 scale)
    {
        return points.Select(p =>
        {
            var v = new Vector2((float) p.X / width, (float) p.Y / height);
            v.Scale(scale);
            return v - scale / 2f;
        }).ToArray();
    }
}
