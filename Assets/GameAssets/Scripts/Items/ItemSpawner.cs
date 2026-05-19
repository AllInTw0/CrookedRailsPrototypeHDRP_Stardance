using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Util;

public class ItemSpawner : StructureGenerator
{
    [System.Serializable]
    public class ItemSpawnEntry
    {
        public ItemSO item;
        public Vector2Int minMaxStackCount;
    }
    [SerializeField]
    private List<ProbabilityListElement<ItemSpawnEntry>> itemSpawnEntryList = new List<ProbabilityListElement<ItemSpawnEntry>>();
    [SerializeField]
    private Vector2Int minMaxCount;

    public override IEnumerator Generate(StructureMaster structureMaster)
    {
        if (itemSpawnEntryList.Count == 0)
        {
            Debug.LogWarning("No Items Defined!");
            yield break;
        }

        ItemSpawnBounds[] structureItemSpawnBoundsArray = transform.GetComponentsInChildren<ItemSpawnBounds>();

        ProbabilityList<ItemSpawnEntry> probabilityList = new ProbabilityList<ItemSpawnEntry>(itemSpawnEntryList);

        int count = Random.Range(minMaxCount.x, minMaxCount.y);
        for (int a = 0; a < count; a++)
        {
            ItemSpawnEntry itemSpawnEntry = probabilityList.PickNext();

            //Get valid spawns points
            List<ItemSpawnBounds> itemSpawnBoundsList = new List<ItemSpawnBounds>(structureItemSpawnBoundsArray);
            for (int b = 0; b < itemSpawnBoundsList.Count; b++)
            {
                if (itemSpawnBoundsList[b].CanSpawnItem(itemSpawnEntry.item) == false)
                {
                    itemSpawnBoundsList.RemoveAt(b);
                    b--;
                }
            }

            if (itemSpawnBoundsList.Count == 0)
            {
                Debug.Log("No valid bounds. Removing item from list: " + itemSpawnEntry.item);
                probabilityList.RemoveLastPicked();
            }
            else
            {
                ItemSpawnBounds chosenBounds = itemSpawnBoundsList[Random.Range(0, itemSpawnBoundsList.Count)];
                Vector3 pos = chosenBounds.transform.position + chosenBounds.transform.right * Random.Range(-chosenBounds.maxRandomOffset.x, chosenBounds.maxRandomOffset.x) + chosenBounds.transform.up * Random.Range(-chosenBounds.maxRandomOffset.y, chosenBounds.maxRandomOffset.y) + chosenBounds.transform.forward * Random.Range(-chosenBounds.maxRandomOffset.z, chosenBounds.maxRandomOffset.z);
                //Spawn with no sound
                Item spawnedItem = Item.SpawnItem(itemSpawnEntry.item, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Random.Range(itemSpawnEntry.minMaxStackCount.x, itemSpawnEntry.minMaxStackCount.y), false);
                spawnedItem.transform.SetParent(transform);
                chosenBounds.AddItem(spawnedItem);
                yield return new WaitForSecondsRealtime(0.01f);
            }

        }

        yield break;
    }
}
