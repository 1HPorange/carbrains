using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarVision : MonoBehaviour
{
    [SerializeField]
    private LayerMask _rayMask;

    private RaycastHit2D[] _hitBuffer = new RaycastHit2D[1];

    private int _lastCachedFrame = -1;

    private float _front;
    public float Front
    {
        get
        {
            Recalculate();
            return _front;
        }
    }

    private float _frontRight;
    public float FrontRight
    {
        get
        {
            Recalculate();
            return _frontRight;
        }
    }

    private float _right;
    public float Right
    {
        get
        {
            Recalculate();
            return _right;
        }
    }

    //public float BackRight { get; private set; }

    //public float Back { get; private set; }

    //public float BackLeft { get; private set; }

    private float _left;
    public float Left
    {
        get
        {
            Recalculate();
            return _left;
        }
    }

    private float _frontLeft;
    public float FrontLeft
    {
        get
        {
            Recalculate();
            return _frontLeft;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Recalculate();

        Gizmos.color = Color.white;

        Gizmos.color = RayDistanceColor(Front);
        Gizmos.DrawRay(transform.position, transform.up * Front);

        Gizmos.color = RayDistanceColor(FrontRight);
        Gizmos.DrawRay(transform.position, (transform.up + transform.right).normalized * FrontRight);

        Gizmos.color = RayDistanceColor(Right);
        Gizmos.DrawRay(transform.position, transform.right * Right);

        //Gizmos.color = RayDistanceColor(BackRight);
        //Gizmos.DrawRay(transform.position, (transform.right - transform.up).normalized * BackRight);

        //Gizmos.color = RayDistanceColor(Back);
        //Gizmos.DrawRay(transform.position, -transform.up * Back);

        //Gizmos.color = RayDistanceColor(BackLeft);
        //Gizmos.DrawRay(transform.position, (-transform.up - transform.right).normalized * BackLeft);

        Gizmos.color = RayDistanceColor(Left);
        Gizmos.DrawRay(transform.position, -transform.right * Left);

        Gizmos.color = RayDistanceColor(FrontLeft);
        Gizmos.DrawRay(transform.position, (transform.up - transform.right).normalized * FrontLeft);
    }

    private Color RayDistanceColor(float distance)
    {
        var whiteTransparent = new Color(1f, 1f, 1f, 0f);
        return Color.Lerp(Color.red, whiteTransparent, distance / 1.5f);
    }

    private void Recalculate()
    {
        if (_lastCachedFrame < Time.frameCount)
        {
            _front = GetRayLength(transform.up);
            _frontRight = GetRayLength((transform.up + transform.right).normalized);
            _right = GetRayLength(transform.right);
            //BackRight = GetRayLength((transform.right - transform.up).normalized);
            //Back = GetRayLength(-transform.up);
            //BackLeft = GetRayLength((-transform.up - transform.right).normalized);
            _left = GetRayLength(-transform.right);
            _frontLeft = GetRayLength((transform.up - transform.right).normalized);

            _lastCachedFrame = Time.frameCount;
        }
    }

    private float GetRayLength(Vector2 direction)
    {
        Physics2D.RaycastNonAlloc(transform.position, direction, _hitBuffer, float.MaxValue, _rayMask.value);
        return Vector2.Distance(transform.position, _hitBuffer[0].point);
    }
}
