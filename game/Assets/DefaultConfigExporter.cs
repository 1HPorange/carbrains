using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultConfigExporter : MonoBehaviour
{
    void Start()
    {
        Population.ExportDefaultConfig("default_config.json");
    }
}
