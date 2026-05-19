using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CargoSO", menuName = "ScriptableObjects/CargoSO", order = 1)]
public class CargoSO : ScriptableObject
{
    public string cargoName;

    [Header("Visual")]
    public GameObject cargoPrefab;

    [Header("Params")]
    public float weight;

    [Header("HaulingJobParams")]
    public List<CargoSO> fittingCargo = new List<CargoSO>();
    public float dangerLevel;
    public List<RailCarSO> fittingRailCars = new List<RailCarSO>();
    public Vector2 payRange;
    [Header("Icons")]
    public List<Texture2D> iconList = new List<Texture2D>();

    public Texture2D GetIcon(RailCarSO railCarSO)
    {
        for (int i = 0; i < fittingRailCars.Count; i++)
        {
            if (fittingRailCars[i] == railCarSO)
            {
                return iconList[i];
            }
        }
        Debug.LogWarning("Couldnt find icon!");
        return iconList[0];
    }
    public string GetName()
    {
        if (cargoName == "")
            return this.name;
        else
            return cargoName;
    }
}
