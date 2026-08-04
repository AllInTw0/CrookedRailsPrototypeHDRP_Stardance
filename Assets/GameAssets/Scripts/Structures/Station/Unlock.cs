using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Unlock : NetworkBehaviour
{
    [SerializeField]
    private MonitorArm unlockMonitor;
    [SerializeField]
    private Shop targetUnlockShop;

    private void Start()
    {
        unlockMonitor.onInteract.AddListener(() =>
        {
            CheckForUnlocks();
        });
    }
    
    private void CheckForUnlocks()
    {
        List<ShopItemSO> playerItemList = new List<ShopItemSO>();

        if(Player.localPlayer.inventory.tool != null) playerItemList.Add(Player.localPlayer.inventory.tool.itemInfo);
        foreach (Item item in Player.localPlayer.inventory.items)
        {
            if(playerItemList.Contains(item.itemInfo) == false)
            {
                playerItemList.Add(item.itemInfo);
            }
        }

        for (int i = 0; i < GameStateManager.unlockList.Count; i++)
        {
            UnlockEntry unlockEntry = GameStateManager.unlockList[i];
            
            if (unlockEntry.isUnlocked) continue;

            bool unlock = true;
            foreach (ShopItemSO neededItem in unlockEntry.neededShopItemList)
            {
                if(playerItemList.Contains(neededItem) == false)
                {
                    unlock = false;
                    break;
                }
            }

            if (unlock)
            {
                UnlockByIndexRpc(i);
                return;
            }

        }

        unlockMonitor.buttonInteractable.InteractionFailed();
        unlockMonitor.buttonInteractable.SetActionNameOverride("No items match", 2f);
    }
    
    [Rpc(SendTo.Everyone)]
    public void UnlockByIndexRpc(int index)
    {
        Debug.Log("Unlocked");
        if(IsServer) 
            targetUnlockShop.AddItemToShop(GameStateManager.unlockList[index].targetShopItem);
        
        GameStateManager.unlockList[index].isUnlocked = true;
        unlockMonitor.buttonInteractable.SetActionNameOverride("Unlocked " + GameStateManager.unlockList[index].targetShopItem.GetName() + "!", 2f);

        UpdateMonitor();
    }
    public void PlayerEntered() 
    {
        unlockMonitor.EnableMonitor(true);
        UpdateMonitor();
    }

    public void PlayerExited()
    {
        unlockMonitor.DisableMonitor();
        unlockMonitor.printer.ClearNotifications();
    }

    private void UpdateMonitor()
    {
        unlockMonitor.printer.ClearNotifications();

        Override unlockListOverride = new Override("UnlockList", OverrideType.UnlockList);
        unlockListOverride.unlockListOverride = GameStateManager.unlockList;

        unlockMonitor.printer.AddNotification(PaperRenderer.active.RenderPaper("UnlockList", new List<Override>() { unlockListOverride }), float.MaxValue);
    }
}
