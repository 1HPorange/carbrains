using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Car;
using UnityEngine;

public class FollowLeaderCam : MonoBehaviour
{
    [SerializeField]
    private NeuralNetworkTrainer _networkTrainer;

    //void Update()
    //{
    //    if (null == _networkTrainer.NeuralCars)
    //    {
    //        return;
    //    }

    //    var leader = _networkTrainer.NeuralCars[0];

    //    foreach (var nc in _networkTrainer.NeuralCars)
    //    {
    //        if (nc.IsActive && nc.HighestCheckpointReached > leader.HighestCheckpointReached)
    //        {
    //            leader = nc;
    //        }
    //    }

    //    transform.position = new Vector3(leader.transform.position.x, leader.transform.position.y, transform.position.z);
    //}
}
