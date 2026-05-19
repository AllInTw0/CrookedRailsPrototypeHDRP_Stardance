using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RailCarSO", menuName = "ScriptableObjects/RailCarSO", order = 2)]
public class RailCarSO : ScriptableObject
{
    public string railCarName;

    [Header("Visual")]
    public GameObject prefab;
    public Texture2D icon;

    [Header("Params")]
    public float weight;

    [Header("HaulingJobParams")]
    public int minLevel;
    public int maxLevel;
    public Vector2 payRange;

    public string GetName()
    {
        if (railCarName == "")
            return this.name;
        else
            return railCarName;
    }
}
