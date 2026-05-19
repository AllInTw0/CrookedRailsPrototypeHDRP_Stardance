using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UnlockEntry
{
    public ShopItemSO targetShopItem;
    public List<ShopItemSO> neededShopItemList;
    public bool isUnlocked;
}
public class GameStateManager : MonoBehaviour
{
    //Serialized

    //Run Time
    public static HaulingJob currentHaulingJob;
    public static bool gameStarted;
    //public static bool isStartingLocationSpawned;
    //public static bool isStationSpawned;

    public static int currentLevel = 0;

    public static float distanceTravelled = 0f;

    //public static bool canEnemiesSpawn
    //{
    //    get
    //    {
    //        return !(isStartingLocationSpawned || isStationSpawned);
    //    }
    //}
    public static List<DistanceWaypoint> waypointList = new List<DistanceWaypoint>();

    //Shop Stuff
    public static List<ShopItemSO> boughtItemList = new List<ShopItemSO>();

    public static List<UnlockEntry> unlockList;
    [SerializeField]
    private List<UnlockEntry> _unlockList;
    private void Start()
    {
        ResetValues();
    }
    private void Awake()
    {
        gameStarted = false;
    }
    public void ResetValues()
    {
        gameStarted = false;
        currentHaulingJob = null;
        currentLevel = 0;
        distanceTravelled = 0f;

        waypointList = new List<DistanceWaypoint>();
        boughtItemList = new List<ShopItemSO>();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;

        unlockList = _unlockList;
        statisticEntryList = new List<StatisticEntry>();
        Money.SetStartingMoney();

        GameOverScreen.onGameOver.AddListener(() =>
        {
            AddToStatistic("Train's Travelled Distance", Mathf.Round((distanceTravelled / 1000f) * 100f) / 100f, StatisticType.DistanceKilometers);
        });
        AddToStatistic("Train's Travelled Distance", 0f, StatisticType.DistanceKilometers);
    }

    public static bool IsItemUnlocked(ShopItemSO shopItem)
    {
        foreach (ShopItemSO neededBoughtItem in shopItem.neededBoughtItems)
        {
            if(boughtItemList.Contains(neededBoughtItem) == false)
            {
                return false;
            }
        }

        foreach (UnlockEntry unlockEntry in unlockList)
        {
            if(unlockEntry.targetShopItem == shopItem && unlockEntry.isUnlocked == false)
            {
                return false;
            }
        }

        if(shopItem.maxCountBought >= 0)
        {
            int countFound = 0;
            for (int i = 0; i < boughtItemList.Count; i++)
            {
                if (boughtItemList[i] == shopItem) countFound++;
            }
            if (countFound >= shopItem.maxCountBought)
                return false;
        }
        return true;
    }

    //Statistics
    public enum StatisticType
    {
        Number,
        Money,
        DistanceMeters,
        DistanceKilometers
    }
    public class StatisticEntry
    {
        public string name;
        public float value;
        public StatisticType type;
        public StatisticEntry(string name, float value, StatisticType type)
        {
            this.name = name;
            this.value = value;
            this.type = type;
        }
    }

    public static List<StatisticEntry> statisticEntryList = new List<StatisticEntry>();

    public static void AddToStatistic(string name, float value, StatisticType type = StatisticType.Number)
    {
        foreach (StatisticEntry entry in statisticEntryList)
        {
            if(entry.name == name)
            {
                entry.value += value;
                return;
            }
        }
        statisticEntryList.Add(new StatisticEntry(name, value, type));
    }


}
