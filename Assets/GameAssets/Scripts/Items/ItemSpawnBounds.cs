using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ItemSpawnBounds : MonoBehaviour
{
    [SerializeField]
    public Vector3 maxRandomOffset;
    [FormerlySerializedAs("itemFilter")] [SerializeField]
    private List<ItemSO> itemWhiteList;
    [SerializeField]
    private List<ItemSO> itemBlackList;
    [SerializeField]
    private int maxItemCount = -1;

    private int itemCount;

    public void AddItem(Item item)
    {
        itemCount++;
    }

    public bool CanSpawnItem(ItemSO itemSO)
    {
        if (itemCount >= maxItemCount && maxItemCount >= 0) return false;

        if (itemWhiteList.Count > 0 && itemWhiteList.Contains(itemSO) == false) return false;
        if (itemBlackList.Count > 0 && itemBlackList.Contains(itemSO)) return false;
        
        
        return true;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;

        Gizmos.DrawWireCube(transform.position, maxRandomOffset * 2f);
    }
}
