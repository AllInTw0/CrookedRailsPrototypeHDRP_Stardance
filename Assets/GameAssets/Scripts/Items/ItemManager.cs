using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager active;

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
            shopItem.EvaluateCurves(0f);
        }
    }
}
