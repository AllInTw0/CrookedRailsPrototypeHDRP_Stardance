using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSpawnerParamsSO", menuName = "ScriptableObjects/ItemSpawnerParamsSO", order = 1)]
public class ItemSpawnerParamsSO : ScriptableObject
{
    public ItemSpawnerParamsSO copySettingsFrom;
    
    [System.Serializable]
    public class ItemSpawnEntry
    {
        public ItemSO item;
        public Vector2Int minMaxStackCount;
        public List<ShopItemSO> neededBoughtItemsToSpawn;
    }

    public List<Util.ProbabilityListElement<ItemSpawnEntry>> itemSpawnEntryList;

    public List<Util.ProbabilityListElement<ItemSpawnEntry>> GetItemSpawnEntryList()
    {
        List<Util.ProbabilityListElement<ItemSpawnEntry>> list = new List<Util.ProbabilityListElement<ItemSpawnEntry>>();

        ItemSpawnerParamsSO paramsSO = this;
        while (paramsSO != null)
        {
            list.AddRange(paramsSO.itemSpawnEntryList);
            paramsSO = paramsSO.copySettingsFrom;
        }
        return list;
    }
}
