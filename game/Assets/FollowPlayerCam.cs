using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerCam : MonoBehaviour
{
    [SerializeField]
    private Transform _player = default;

    void Update()
    {
        transform.position = new Vector3(_player.position.x, _player.position.y, transform.position.z);
    }
}
