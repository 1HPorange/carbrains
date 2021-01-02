using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongRunningTimer : MonoBehaviour
{
    // Basically Time.time, but without loss of millisecond precision after 9 hours...
    public double Now { get; private set; }

    public void Reset()
    {
        Now = 0.0;
    }

    private void FixedUpdate()
    {
        Now += Time.deltaTime;
    }
}
