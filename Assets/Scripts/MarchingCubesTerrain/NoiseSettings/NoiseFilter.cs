using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseFilter
{
    Noise _noise = new Noise();
    readonly NoiseSettings _settings;

    public NoiseFilter(NoiseSettings settings)
    {
        _settings = settings;
    }

    public void SetSeed(int seed)
    {
        _noise = new Noise(seed);
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;
        var frequency = _settings.baseRoughness;
        float amplitude = 1;

        for (var i = 0; i < _settings.numLayers; i++)
        {
            var v = _noise.Evaluate(new Vector3(point.x * frequency / 16f + 0.001f, point.y * frequency / 16f + 0.001f, point.z * frequency / 16f + 0.001f));
            noiseValue += (v + 1) * .5f * amplitude;
            frequency *= _settings.roughness;
            amplitude *= _settings.persistence;
        }

        noiseValue = Mathf.Max(0, noiseValue - _settings.minValue);
        return noiseValue * _settings.strength;
    }
}