using System;
using TMPro;
using UnityEngine;

public class DisplayBoard : MonoBehaviour
{
    [SerializeField] 
    private RailCar linkedRailCar;
    [Header("References")]
    [SerializeField] 
    private TMP_Text healthText;
    private void Update()
    {
        healthText.text = linkedRailCar.railCarSO.GetName() + ": " + linkedRailCar.health.GetHealthPercent() + "%";

        CargoInfo cargoInfo = linkedRailCar.GetCargoInfo();
        if (cargoInfo.GetCargoSO() && cargoInfo.GetCargoHealth())
        {
            healthText.text += "\n" + cargoInfo.GetCargoSO().GetName() + ": " + cargoInfo.GetCargoHealth().GetHealthPercent() + "%";
        }
    }
}
