using UnityEngine;

[CreateAssetMenu(fileName = "StructureSO", menuName = "ScriptableObjects/StructureSO", order = 2)]
public class StructureSO : ScriptableObject
{
    public GameObject structurePrefab;

    public enum StructureDistanceType
    {
        setDistance,
        fuelDistance
    }
    [Header("Spawn Location Params")]
    public StructureDistanceType distanceType;
    public Vector2 distanceTypeValue; // setDistance = distance; fuelDistance = fuelDistance * value
    public int maxCount = -1;
    public float minDistanceFromStation;

    public float GetDistance()
    {
        if (distanceType == StructureDistanceType.setDistance)
        {
            return Random.Range(distanceTypeValue.x, distanceTypeValue.y);
        }
        else if (distanceType == StructureDistanceType.fuelDistance)
        {
            float minDistance = float.MaxValue;
            foreach (PlayerTrain.FuelSource fuelSource in PlayerTrain.active.GetFuelSourceList())
            {
                minDistance = Mathf.Min(fuelSource.targetHealth.GetMaxHealth() / TrainUpgradeHandler.active.GetStatValue(fuelSource.consumptionRate), minDistance);
            }
            return minDistance * Random.Range(distanceTypeValue.x, distanceTypeValue.y);
        }

        return 0f;
    }
}
