using System.Collections.Generic;
using UnityEngine;

public class Tender : MonoBehaviour
{
    //Variables
    public static Tender active;


    [Header("Tender Fuel & Water")]
    public Health waterLevel;
    public Health fuelLevel;
    public List<float> procentWarningList;
    public float notificationDuration = 3f;

    [Header("Water Hatch")]
    public Animator animator;
    public Transform waterHatch;
    public float waterHatchOffset;
    //Run Time
    [HideInInspector]
    public bool hatchOpened;

    private float lastWaterLevel;
    private float lastFuelLevel;
    private void Start()
    {
        active = this;
        lastWaterLevel = waterLevel.GetHealth();
        lastFuelLevel = fuelLevel.GetHealth();
    }
    private void Update()
    {
        void UpdateWarning(string warningName, float currentValue, float lastValue, float maxValue)
        {
            if(currentValue < lastValue)
            {
                float currentPrecent = (currentValue / maxValue) * 100f;
                float lastPrecent = (lastValue / maxValue) * 100f;

                foreach (float precent in procentWarningList)
                {
                    if(currentPrecent < precent && precent < lastPrecent)
                    {
                        MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper(warningName, new List<Override>() { new Override("precent", OverrideType.Text, (precent + Random.Range(-3,3)) + "%") }), notificationDuration);
                        return;
                    }
                }
            }
        }

        if (waterLevel.GetHealth() != lastWaterLevel)
            UpdateWarning("Water", waterLevel.GetHealth(), lastWaterLevel, waterLevel.maxHealth);
        if (fuelLevel.GetHealth() != lastFuelLevel)
            UpdateWarning("Fuel", fuelLevel.GetHealth(), lastFuelLevel, fuelLevel.maxHealth);

        lastWaterLevel = waterLevel.GetHealth();
        lastFuelLevel = fuelLevel.GetHealth();
    }
    public void HatchInteract()
    {
        hatchOpened = !hatchOpened;
        if (hatchOpened)
            animator.SetBool("open", true);
        else
            animator.SetBool("open", false);
    }
}
