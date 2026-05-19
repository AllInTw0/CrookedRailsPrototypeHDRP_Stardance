using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnBounds : MonoBehaviour
{
    [SerializeField]
    public Vector3 maxRandomOffset;
    [SerializeField]
    private List<ItemSO> itemFilter;
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

        if (itemFilter.Count > 0 && itemFilter.Contains(itemSO) == false) return false;

        return true;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;

        Gizmos.DrawWireCube(transform.position, maxRandomOffset * 2f);
    }
}
