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
    //Run time
    private TrainUpgrade[] trainUpgradeArray;

    private Dictionary<TrainStatType, DefaultStats> defaultStatsDict = new Dictionary<TrainStatType, DefaultStats>();
    public Dictionary<TrainStatType, List<Upgrade>> currentUpgradeDict = new Dictionary<TrainStatType, List<Upgrade>>();
    void Awake()
    {
        foreach (DefaultStats defaultStats in defaultStatsList)
        {
            defaultStatsDict.Add(defaultStats.trainStat, defaultStats);
            currentUpgradeDict.Add(defaultStats.trainStat, new List<Upgrade>());
        }
        active = this;
        trainUpgradeArray = GetComponentsInChildren<TrainUpgrade>();
    }

    void Update()
    {
        
    }
    
    public void AddUpgrade(UpgradeSO upgradeInfo)
    {
        for (int i = 0; i < trainUpgradeArray.Length; i++)
        {
            if (trainUpgradeArray[i].linkedUpgradeSO == upgradeInfo)
            {
                trainUpgradeArray[i].SetUnlocked(true);
            }
        }

        for (int i = 0; i < upgradeInfo.replaceList.Count; i++)
        {
            RemoveUpgrade(upgradeInfo.replaceList[i]);
        }
    }

    public void RemoveUpgrade(UpgradeSO upgradeInfo)
    {
        for (int i = 0; i < trainUpgradeArray.Length; i++)
        {
            if (trainUpgradeArray[i].linkedUpgradeSO == upgradeInfo)
            {
                trainUpgradeArray[i].SetUnlocked(false);
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
