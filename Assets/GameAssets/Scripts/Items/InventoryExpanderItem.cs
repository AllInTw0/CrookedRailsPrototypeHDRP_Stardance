using UnityEngine;

public class InventoryExpanderItem : Item
{
    public override bool Interact()
    {
        PlayerInventory.active.slotCount++;
        Destroy(gameObject);
        return true;
    }
}
