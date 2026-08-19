using Unity.Netcode;
using UnityEngine;

public class ItemGiver : Interactable
{
    private NetworkVariable<int> itemCount = new(writePerm: NetworkVariableWritePermission.Server);
    private int clientCountDiff = 0;

    private int GetItemCount()
    {
        return itemCount.Value + clientCountDiff;
    }
    [Header("Item Giver")]
    [SerializeField]
    private ItemSO itemInfo;
    [SerializeField]
    private int startingItemCount = 50;
    [SerializeField]
    private int maxGiveCount;
    [Header("Item Display")]
    [SerializeField]
    private ItemDisplay itemDisplay;
    public override void OnNetworkSpawn()
    {
        if(IsServer)
            itemCount.Value = startingItemCount;
        UpdateVisual();
        itemCount.OnValueChanged += CountChanged;
    }

    private void UpdateVisual()
    {
        itemDisplay.SetTarget(GetItemCount());
    }
    private void CountChanged(int previous, int current)
    {
        clientCountDiff = 0;
        UpdateVisual();
    }
    public override bool Interact()
    {
        bool sucessfull = false;
        if (GetItemCount() > 0)
        {
            GiveItemServerRpc(NetworkManager.Singleton.LocalClientId);
            //if(IsServer == false)
                //clientCountDiff -= Mathf.Min(maxGiveCount, GetItemCount());

            UpdateVisual();
            sucessfull = true;
        }

        SoundManager.active.PlayAtPos(iconPosition.position, interactSound);
        return sucessfull;
    }

    [Rpc(SendTo.Server)]
    private void GiveItemServerRpc(ulong clientID)
    {
        if (GetItemCount() > 0)
        {
            Player targetPlayer = Player.GetPlayerByClientID(clientID);
            
            Item item = Item.SpawnItem(itemInfo, Vector3.zero, Quaternion.identity, Mathf.Min(maxGiveCount, GetItemCount()));

            item.TryEquipping(targetPlayer,true);

            int countGiven = maxGiveCount;
            //IsSpawned checks if the item has been destroyed
            if (item.IsSpawned && targetPlayer.inventory.items.Contains(item) == false)
            {
                countGiven -= item.GetCount();
                item.DestroyItem();
            }

            itemCount.Value -= countGiven;
        }
    }
}
