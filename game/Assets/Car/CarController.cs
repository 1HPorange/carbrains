using System.Collections;
using System.Collections.Generic;
using Assets.Car;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CarController : MonoBehaviour
{
    private ICarInputSource _inputSource;

    private Rigidbody2D _rigidBody;

    public float AccelerationForce;

    public float BreakingForce;

    public float BackwardsForce;

    public float SteeringForce;

    private void Start()
    {
        _inputSource = GetComponent<ICarInputSource>() ?? new DummyCarInputSource();
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    public void SetInputSource(ICarInputSource inputSource)
    {
        _inputSource = inputSource;
    }

    public void AddForces()
    {
        if (!_inputSource.IsActive)
        {
            return;
        }

        var throttleBreak = Mathf.Clamp((float) _inputSource.ThrottleBreak, -1f, 1f);

        if (throttleBreak >= 0)
        {
            _rigidBody.AddForce(transform.up * throttleBreak * AccelerationForce, ForceMode2D.Impulse);
        }
        else
        {
            if (Vector2.Angle(transform.up, _rigidBody.velocity) < 90f)
            {
                _rigidBody.AddForce(transform.up * throttleBreak * BreakingForce, ForceMode2D.Impulse);
            }
            else
            {
                _rigidBody.AddForce(transform.up * throttleBreak * BackwardsForce, ForceMode2D.Impulse);
            }
        }
        
        _rigidBody.AddTorque(Mathf.Clamp((float)_inputSource.Steering, -1f, 1f) * -SteeringForce, ForceMode2D.Impulse);
    }
}
