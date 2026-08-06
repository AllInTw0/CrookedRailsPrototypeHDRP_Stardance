using UnityEngine;

public class InventoryExpanderItem : Item
{
    public override bool TryEquipping(Player targetPlayer = null, bool sendRpc = true)
    {
        Player.localPlayer.inventory.ModifySlotCountRpc(1);
        DestroyItem();
        return true;
    }
}
