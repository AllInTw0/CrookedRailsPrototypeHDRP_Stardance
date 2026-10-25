using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StartingShop : Shop
{
    public static StartingShop activeStartingShop = null;
    public static NetworkVariable<bool> hasBoughtEveryItem = new NetworkVariable<bool>();
    [System.Serializable]
    private class StartingShopEntry
    {
        public List<ShopItemSO> shopItemEntryList;
    }
    
    [Header("Starting Shop")]
    [SerializeField]
    private List<StartingShopEntry> startingShopItemEntryList;


    
    private int count = 0;

    private void Awake()
    {
        activeStartingShop = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        hasBoughtEveryItem.Value = false;
    }

    public override void Start()
    {
        base.Start();
        shopItemEntryList = startingShopItemEntryList[0].shopItemEntryList;
        onItemBought.AddListener(() =>
        {
            if(!IsServer)
                return;
            
            ClearShopRpc();
            count++;
            if (count < startingShopItemEntryList.Count)
            {
                ChangeShopItemsRpc(count);
                
                Initialize();
            }
            hasBoughtEveryItem.Value = true;
        });
    }

    [Rpc(SendTo.Everyone)]
    private void ChangeShopItemsRpc(int index)
    {
        shopItemEntryList = startingShopItemEntryList[index].shopItemEntryList;
    }
}
