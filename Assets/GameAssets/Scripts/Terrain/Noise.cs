using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static List<NoiseSettings> settingsList;
    public static void Initialize(int seed, List<NoiseSettings> noiseSettingList)
    {
        settingsList = noiseSettingList;
        for (int i = 0; i < settingsList.Count; i++)
        {
            settingsList[i].Initialize(seed + i);
        }
    }
    public static float SampleNoise(float posX, float posY)
    {
        float totalValue = 0f;
        foreach (NoiseSettings settings in settingsList)
        {
            if (settings.ignore) continue;

            float value = 0f;
            for (int i = 0; i < settings.octaveOffsetArray.Length; i++)
            {
                float sampleX = (posX + settings.octaveOffsetArray[i].x) * settings.scale * settings.octaveFrequencyArray[i];
                float sampleY = (posY + settings.octaveOffsetArray[i].y) * settings.scale * settings.octaveFrequencyArray[i];

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1;
                value += perlinValue * settings.octaveAmplitudeArray[i];
            }
            value = value / settings.octaveAmplitudeSum;
            if (settings.useHeightCurve)
            {
                lock (settings.heightCurve)
                {
                    value = settings.heightCurve.Evaluate(value);
                }
            }
            totalValue += value;
        }

        return totalValue;
    }
}
[System.Serializable]
public class NoiseSettings
{
    [Header("Development")]
    public bool ignore;
    [Header("Params")]
    public float scale;
    [Header("Octaves")]
    public int octaveCount;
    public float amplitudeMult;
    public float frequencyMult;

    [Header("Height mult")]
    public bool useHeightCurve;
    public AnimationCurve heightCurve;

    //Caluclated at run time
    [HideInInspector]
    public Vector2[] octaveOffsetArray;
    [HideInInspector]
    public float[] octaveAmplitudeArray;
    [HideInInspector]
    public float[] octaveFrequencyArray;
    [HideInInspector]
    public float octaveAmplitudeSum;
    //public NoiseSettings(int seed, float scale, int octaveCount, float amplitudeMult, float frequencyMult)
    //{

    //}
    public void Initialize(int seed)
    {
        if (scale == 0f)
            scale = 0.0001f;

        System.Random rng = new System.Random(seed);

        octaveOffsetArray = new Vector2[octaveCount];
        octaveAmplitudeArray = new float[octaveCount];
        octaveFrequencyArray = new float[octaveCount];
        float amplitude = 1f;
        float frequency = 1f;
        octaveAmplitudeSum = 0;
        for (int i = 0; i < octaveCount; i++)
        {
            octaveOffsetArray[i] = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));
            octaveAmplitudeArray[i] = amplitude;
            octaveFrequencyArray[i] = frequency;
            octaveAmplitudeSum += amplitude;
            amplitude *= amplitudeMult;
            frequency *= frequencyMult;
        }
    }
}
