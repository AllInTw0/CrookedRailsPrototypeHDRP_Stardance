using System.Collections.Generic;
using UnityEngine;
using static Util;
public class ShopItem
{
    public ShopItemSO itemInfo;
    public int price;
    public int stock;

    public Shop linkedShop;

    public ShopItem(ShopItemSO itemInfo, int price, int stock, Shop linkedShop)
    {
        this.itemInfo = itemInfo;
        this.price = price;
        this.linkedShop = linkedShop;
        this.stock = stock;
    }
}
public class Shop : MonoBehaviour
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
    
    private List<ShopItem> shopItemList;

    [HideInInspector]
    public ShopItem selectedShopItem;
    
    private void Start()
    {
        shopMonitor.onInteract.AddListener(() => {
            //Buy item
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
                    RenderMonitorPaper();

                    if(selectedShopItem.stock <= 0)
                        shopMonitor.DisableButton();
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
        });

        shopItemList = new List<ShopItem>();
    }
    public void Initialize(int overrideLevel = -1)
    {
        if (shopItemEntryList.Count == 0) return;

        int currentLevel = overrideLevel == -1 ? GameStateManager.currentLevel : overrideLevel;
        
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
        if(shopItemList.Count < shopStandList.Count)
        {
            shopItemList.Add(new ShopItem(shopItem, shopItem.price, shopItem.stock, this));
            shopStandList[shopItemList.Count - 1].Intialize(shopItemList[shopItemList.Count - 1]);
        }
        else
        {
            Debug.LogWarning("Cant add item to shop!");
        }
    }

    public void ClearShop()
    {
        foreach (ShopStand stand in shopStandList)
        {
            stand.ClearStand();
        }
        shopItemList.Clear();
    }
    public void SelectShopItem(ShopItem shopItem)
    {
        selectedShopItem = shopItem;

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
