using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "ScriptableObjects/ItemSO", order = 1)]
public class ItemSO : ShopItemSO
{
    [Header("Inventory")]
    public int slotCount = 1;
    public int maxCount = 1;

    [Header("Dropping")]
    public bool randomYRot;
    public Vector3 dropOffset;

    [Header("Tool Params")]
    public bool isTool;
}
