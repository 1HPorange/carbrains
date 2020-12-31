using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarVision : MonoBehaviour
{
    [SerializeField]
    private LayerMask _rayMask;

    private RaycastHit2D[] _hitBuffer = new RaycastHit2D[1];

    public float Front { get; private set; }

    public float FrontRight { get; private set; }

    public float Right { get; private set; }

    //public float BackRight { get; private set; }

    //public float Back { get; private set; }

    //public float BackLeft { get; private set; }

    public float Left { get; private set; }

    public float FrontLeft { get; private set; }

    public void Recalculate()
    {
        Front = GetRayLength(transform.up);
        FrontRight = GetRayLength((transform.up + transform.right).normalized);
        Right = GetRayLength(transform.right);
        //BackRight = GetRayLength((transform.right - transform.up).normalized);
        //Back = GetRayLength(-transform.up);
        //BackLeft = GetRayLength((-transform.up - transform.right).normalized);
        Left = GetRayLength(-transform.right);
        FrontLeft = GetRayLength((transform.up - transform.right).normalized);
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

    private float GetRayLength(Vector2 direction)
    {
        Physics2D.RaycastNonAlloc(transform.position, direction, _hitBuffer, float.MaxValue, _rayMask.value);
        return Vector2.Distance(transform.position, _hitBuffer[0].point);
    }
}
