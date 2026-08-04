using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Util;
public class ShopItem : INetworkSerializable
{
    private int itemID;
    public int standIndex;
    public int price;
    public int stock;

    public ShopItemSO itemInfo
    {
        get{return linkedShop.GetItemFromID(itemID);}
    }
    public Shop linkedShop
    {
        get { return linkedShopRefrence.TryGet(out Shop shop) ? shop : null; }
    }
    private NetworkBehaviourReference linkedShopRefrence;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemID);
        serializer.SerializeValue(ref standIndex);
        serializer.SerializeValue(ref price);
        serializer.SerializeValue(ref stock);
        
        serializer.SerializeValue(ref linkedShopRefrence);
    }

    //This is needed otherwise the network code returns an error: (MissingMethodException: Default constructor not found for type ShopItem)
    public ShopItem() : this(null, 0, 0,0, null)
    {
        
    }
    public ShopItem(ShopItemSO itemInfo, int price, int stock,int standIndex, Shop linkedShop)
    {
        if(linkedShop == null)return;
        
        this.itemID = linkedShop.GetItemID(itemInfo);
        this.standIndex = standIndex;
        this.price = price;
        this.stock = stock;
        linkedShopRefrence = linkedShop;
    }
}
public class Shop : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField]
    private List<ShopStand> shopStandList;
    [SerializeField]
    private MonitorArm shopMonitor;
    [SerializeField]
    private Transform itemSpawnPos;
    [SerializeField]
    private Flickerer shopLight;
    [Header("Items")]
    [SerializeField]
    private List<ShopItemSO> shopItemEntryList;
    [SerializeField] 
    private bool forceSingleStock;
    [Header("Missing")]
    [SerializeField]
    private Texture2D missingIcon;

    [Header("Debug")] 
    public int debugLevel;
    
    private ShopItem[] shopItemArray;

    [HideInInspector]
    public ShopItem selectedShopItem;


    public int GetItemID(ShopItemSO item)
    {
        for (int i = 0; i < shopItemEntryList.Count; i++)
        {
            if (item == shopItemEntryList[i])
                return i;
        }
        return -1;
    }
    public ShopItemSO GetItemFromID(int  itemID)
    {
        return shopItemEntryList[itemID];
    }
    
    private void Start()
    {
        shopMonitor.onInteract.AddListener(() => {
            //Buy item
            if (selectedShopItem != null)
            {
                BuyItemRpc();
            }
            else
            {
                Debug.LogWarning("No selected item");
            }
        });

        shopItemArray = new ShopItem[shopStandList.Count];
    }
    
    [Rpc(SendTo.Server)]
    private void BuyItemRpc()
    {
        if (selectedShopItem != null)
        {
            if (selectedShopItem.stock > 0 && Money.CanAfford(selectedShopItem.price))
            {
                if (selectedShopItem.itemInfo is ItemSO) 
                {
                    Vector3 pos = itemSpawnPos.position + new Vector3(Random.Range(-0.4f, 0.4f), 0, Random.Range(-0.4f, 0.4f));
                    Quaternion rot = Quaternion.Euler(Random.Range(-90f, 90), Random.Range(-90f, 90), Random.Range(-90f, 90));
                    Item.SpawnItem((ItemSO)selectedShopItem.itemInfo, pos, rot);
                }
                else if (selectedShopItem.itemInfo is UpgradeSO)
                {
                    TrainUpgradeHandler.active.AddUpgrade((UpgradeSO)selectedShopItem.itemInfo);
                }

                if(GameStateManager.boughtItemList.Contains(selectedShopItem.itemInfo) == false)
                    GameStateManager.boughtItemList.Add(selectedShopItem.itemInfo);

                selectedShopItem.stock--;
                Money.AddMoney(-selectedShopItem.price);
                
                SyncShopItemRpc(selectedShopItem);
            }
            else
            {
                Debug.LogWarning("Cant buy item");
            }
        }
        else
        {
            Debug.LogWarning("No selected item");
        }
    }
    public void Initialize(int overrideLevel = -1)
    {
        if (shopItemEntryList.Count == 0 || IsServer == false) return;

        int currentLevel = overrideLevel == -1 ? GameStateManager.currentLevel.Value : overrideLevel;
        
        List<ProbabilityListElement<ShopItemSO>> shopItemProbabilityList = new List<ProbabilityListElement<ShopItemSO>>();
        
        for (int i = 0; i < shopItemEntryList.Count; i++)
        {
            //Values
            shopItemEntryList[i].EvaluateCurves(currentLevel);
            
            //Stock
            if (forceSingleStock)
                shopItemEntryList[i].stock = 1;
            else
                shopItemEntryList[i].EvaluateStock(currentLevel);
            
            if (GameStateManager.IsItemUnlocked(shopItemEntryList[i]) && shopItemEntryList[i].probability > 0)
            {
                shopItemProbabilityList.Add(new ProbabilityListElement<ShopItemSO>(shopItemEntryList[i], shopItemEntryList[i].probability, 1));
            }
        }

        ProbabilityList<ShopItemSO> probabilityList = new ProbabilityList<ShopItemSO>(shopItemProbabilityList);

        //Debug
        string str = "";
        for (int i = 0; i < shopItemProbabilityList.Count; i++)
        {
            str += shopItemProbabilityList[i].element + ":" + shopItemProbabilityList[i].probability + ", ";
        }
        Debug.Log("Shop probbability [" + currentLevel +"]: " + str);

        //Pick Items
        for (int i = 0; i < shopStandList.Count; i++)
        {
            ShopItemSO shopItem = probabilityList.PickNext();
            if(shopItem != null)
                AddItemToShop(shopItem);
        }
    }

    public void AddItemToShop(ShopItemSO shopItem)
    {
        for (int i = 0; i < shopItemArray.Length; i++)
        {
            if (shopItemArray[i] == null)
            {
                SyncShopItemRpc(new ShopItem(shopItem, shopItem.price, shopItem.stock, i, this));

                return;
            }
        }

        Debug.LogWarning("Cant add item to shop!");
        
    }

    [Rpc(SendTo.Everyone)]
    private void SyncShopItemRpc(ShopItem shopItem)
    {
        shopItemArray[shopItem.standIndex] = shopItem;
        if (shopStandList[shopItem.standIndex].GetShopItem() == null)
        {
            shopStandList[shopItem.standIndex].Intialize(shopItem);
        }
        else
        {
            shopStandList[shopItem.standIndex].UpdateShopItem(shopItem);

            if (selectedShopItem.standIndex == shopItem.standIndex)
            {
                selectedShopItem = shopItem;
                
                RenderMonitorPaper();

                if (selectedShopItem.stock <= 0)
                    shopMonitor.DisableButton();
            }
        }
    }
    
    [Rpc(SendTo.Everyone)]
    public void ClearShopRpc()
    {
        foreach (ShopStand stand in shopStandList)
        {
            stand.ClearStand();
        }

        shopItemArray = new ShopItem[shopStandList.Count];
    }
    [Rpc(SendTo.Everyone)]
    public void SelectShopItemRpc(int standIndex)
    {
        selectedShopItem = shopItemArray[standIndex];

        shopMonitor.EnableButton();
        RenderMonitorPaper();     
    }
    private void RenderMonitorPaper()
    {
        shopMonitor.EnableMonitor();
        shopMonitor.printer.ClearNotifications();

        Override override1 = new Override("Title", OverrideType.Text);
        override1.stringOverride = selectedShopItem.itemInfo.GetName();
        Override override2 = new Override("ItemIcon", OverrideType.RawImageTexture);
        override2.textureOverride = selectedShopItem.itemInfo.icon != null ? selectedShopItem.itemInfo.icon : missingIcon;
        Override override3 = new Override("ItemDescription", OverrideType.Text);
        override3.stringOverride = GetDescription(selectedShopItem.itemInfo);
        Override override4 = new Override("ItemShopStats", OverrideType.Text);
        override4.stringOverride = "Stock:" + selectedShopItem.stock + " Price:" + selectedShopItem.price + "$";

        shopMonitor.printer.AddNotification(PaperRenderer.active.RenderPaper("ItemInfo", new List<Override>() { override1 , override2, override3, override4}), float.MaxValue);
    }
    private string GetDescription(ShopItemSO itemInfo)
    {
        string description = itemInfo.description;
        if (itemInfo is ItemSO)
        {
            Item itemScript = itemInfo.prefab.GetComponent<Item>();
            if (itemScript is ItemGun)
            {
                ItemGun gunScript = (ItemGun)itemScript;
                description += "\nSTATS:";
                description += "\nAmmo: " + gunScript.ammoItem.GetName();
                description += "\nClip Size: " + gunScript.clipSize;
                description += "\nBullet Damage: " + gunScript.bulletDamage;
                description += "\nBullet Count: " + gunScript.bulletCount;
            }
            if (itemScript is ItemMelee)
            {
                ItemMelee meleeScript = (ItemMelee)itemScript;
                description += "\nSTATS:";
                description += "\nDamage: " + meleeScript.damage;
                description += "\nRange: " + meleeScript.range + "m";
            }
        }
        else if(itemInfo is UpgradeSO)
        {
            if (((UpgradeSO)itemInfo).upgradeList.Count > 0)
            {
                description += "\nEFFECT:";
                foreach (Upgrade upgrade in ((UpgradeSO)itemInfo).upgradeList)
                {
                    description += "\n" + TrainUpgradeHandler.active.GetUpgradeDescription(upgrade);
                }
            }
        }
        return description;
    }
    public void PlayerEntered()
    {
        if (shopLight != null) shopLight.TurnOn();
        if(selectedShopItem != null) RenderMonitorPaper();
    }
    //This gets called only in the upgrade/general shops and not in the unlock shop
    public void PlayerEnteredOnce()
    {
        Initialize();
    }
    public void PlayerExited()
    {
        if (shopLight != null) shopLight.TurnOff();

        shopMonitor.DisableMonitor();
        shopMonitor.printer.ClearNotifications();
    }
}
