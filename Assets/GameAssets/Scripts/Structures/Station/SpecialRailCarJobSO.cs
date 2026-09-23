using UnityEngine;

[CreateAssetMenu(fileName = "SpecialRailCarJobSO", menuName = "ScriptableObjects/SpecialRailCarJobSO", order = 2)]
public class SpecialRailCarJobSO : SpecialJobSO
{
    public RailCarSO railCar;
    public int fee;

    public override Texture2D GetIcon()
    {
        return railCar.icon;
    }
    public override bool OrderJob()
    {
        if (Money.CanAfford(fee) == false)
            return false;
        RailCar spawnedRailCar = PlayerTrain.active.AddRailCar(railCar, 2);
        Money.AddMoney(-fee);
        return true;
    }
}
