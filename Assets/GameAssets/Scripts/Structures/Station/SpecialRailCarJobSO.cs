using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpecialRailCarJobSO", menuName = "ScriptableObjects/SpecialRailCarJobSO", order = 2)]
public class SpecialRailCarJobSO : SpecialJobSO
{
    public List<RailCarSO> railCarList = new List<RailCarSO>();
    public int fee;

    public override Texture2D GetIcon()
    {
        return railCarList[0].icon;
    }
    public override bool OrderJob()
    {
        if (Money.CanAfford(fee) == false)
            return false;
        for (int i = 0; i < railCarList.Count; i++)
        {
            RailCar spawnedRailCar = PlayerTrain.active.AddRailCar(railCarList[i], 2);
        }
        Money.AddMoney(-fee);
        return true;
    }
}
