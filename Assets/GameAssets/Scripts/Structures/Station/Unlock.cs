using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Unlock : MonoBehaviour
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

        if(PlayerInventory.active.tool != null) playerItemList.Add(PlayerInventory.active.tool.itemInfo);
        foreach (Item item in PlayerInventory.active.items)
        {
            if(playerItemList.Contains(item.itemInfo) == false)
            {
                playerItemList.Add(item.itemInfo);
            }
        }

        foreach (UnlockEntry unlockEntry in GameStateManager.unlockList)
        {
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
                Debug.Log("Unlocked");
                targetUnlockShop.AddItemToShop(unlockEntry.targetShopItem);
                unlockEntry.isUnlocked = true;
                unlockMonitor.buttonInteractable.SetActionNameOverride("Unlocked " + unlockEntry.targetShopItem.GetName() + "!", 2f);

                UpdateMonitor();
                return;
            }

        }

        unlockMonitor.buttonInteractable.InteractionFailed();
        unlockMonitor.buttonInteractable.SetActionNameOverride("No items match", 2f);
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
