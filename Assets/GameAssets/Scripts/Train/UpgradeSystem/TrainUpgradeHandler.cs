using System.Collections.Generic;
using UnityEngine;

public enum TrainStatType
{
    None,
    WaterConsumption,
    FuelConsumption,
    Speed,
    Acceleration,
    Deceleration,
    BreakdownChance,
    DerailByObjectChance,
}
public enum UpgradeType
{
    Percent,
    Addition
}
[System.Serializable]
public class Upgrade
{
    public TrainStatType trainStat;
    public UpgradeType upgradeType;
    public float value;
}
public class TrainUpgradeHandler : MonoBehaviour
{
    public static TrainUpgradeHandler active;

    [System.Serializable]
    public class DefaultStats
    {
        public TrainStatType trainStat;
        public string statName;
        public float defaultValue;
    }
    [System.Serializable]
    public class UpgradeVisual
    {
        public UpgradeSO linkedUpgrade;
        public List<GameObject> enableObjectList;
        public List<GameObject> disableObjectList;
    }

    [SerializeField]
    private List<DefaultStats> defaultStatsList;
    [SerializeField]
    private List<UpgradeVisual> upgradeVisualList = new List<UpgradeVisual>();
    //Run time
    private List<UpgradeSO> boughtUpgradeList = new List<UpgradeSO>();

    private Dictionary<TrainStatType, DefaultStats> defaultStatsDict = new Dictionary<TrainStatType, DefaultStats>();
    private Dictionary<TrainStatType, List<Upgrade>> currentUpgradeDict = new Dictionary<TrainStatType, List<Upgrade>>();
    void Awake()
    {
        foreach (DefaultStats defaultStats in defaultStatsList)
        {
            defaultStatsDict.Add(defaultStats.trainStat, defaultStats);
            currentUpgradeDict.Add(defaultStats.trainStat, new List<Upgrade>());
        }
        UpdateVisuals();
        active = this;
    }

    void Update()
    {
        
    }

    public void AddUpgrade(UpgradeSO upgradeInfo)
    {
        boughtUpgradeList.Add(upgradeInfo);
        foreach (Upgrade upgrade in upgradeInfo.upgradeList)
        {
            currentUpgradeDict[upgrade.trainStat].Add(upgrade);
        }

        UpdateVisuals();
    }
    private void UpdateVisuals()
    {
        foreach (UpgradeVisual visual in upgradeVisualList)
        {
            foreach (GameObject obj in visual.enableObjectList)
            {
                obj.SetActive(boughtUpgradeList.Contains(visual.linkedUpgrade));
            }
            foreach (GameObject obj in visual.disableObjectList)
            {
                if(boughtUpgradeList.Contains(visual.linkedUpgrade))
                    obj.SetActive(false);
            }
        }
    }
    public float GetStatValue(TrainStatType trainStatType)
    {
        float value = defaultStatsDict[trainStatType].defaultValue;
        float addition = 0f;
        float percent = 1f;

        foreach (Upgrade upgrade in currentUpgradeDict[trainStatType])
        {
            if(upgrade.upgradeType == UpgradeType.Addition)
            {
                addition += upgrade.value;
            }
            else if(upgrade.upgradeType == UpgradeType.Percent)
            {
                percent += upgrade.value;
            }
        }

        return value * percent + addition;
    }

    public string GetUpgradeDescription(Upgrade upgrade)
    {
        string str = "";
        if (upgrade.value >= 0)
            str += "Increases ";
        else
            str += "Decreases ";

        DefaultStats defaultStats = defaultStatsDict[upgrade.trainStat];
        str += defaultStats.statName;

        str += " by ";

        if (upgrade.upgradeType == UpgradeType.Addition)
        {
            str += upgrade.value;
        }
        else if(upgrade.upgradeType == UpgradeType.Percent)
        {
            str += Mathf.Round(upgrade.value * 100f * 10f) * 0.1f + "%";
        }

        return str;
    }
}
