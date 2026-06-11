using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager active;
    private static Dictionary<ushort, Item> itemDictionary = new Dictionary<ushort, Item>();
    
    [SerializeField]
    private LayerMask _itemDropLayerMask;
    public static LayerMask itemDropLayerMask;

    [SerializeField]
    List<ShopItemSO> shopItemList;
    private void Start()
    {
        itemDropLayerMask = _itemDropLayerMask;
        active = this;

        foreach (ShopItemSO shopItem in shopItemList)
        {
            shopItem.EvaluateCurves(0);
        }
    }

    public static Item GetItemByID(ushort networkBehaviourId)
    {
        return itemDictionary[networkBehaviourId];
    }

    public static void AddItem(Item item)
    {
        itemDictionary.Add(item.NetworkBehaviourId, item);
    }

    public static void RemoveItem(Item item)
    {
        itemDictionary.Remove(item.NetworkBehaviourId);
    }
}
