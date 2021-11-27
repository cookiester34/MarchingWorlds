using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseSettings
{

    public float strength = 1;
    [Range(1, 3)]
    public int numLayers = 1;
    public float baseRoughness = 1;
    public float roughness = 2;
    [Range(1,2)]
    public float persistence = .5f;
    [Range(0,1)]
    public float minValue;
}