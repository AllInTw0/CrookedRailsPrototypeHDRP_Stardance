using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.Netcode;
using UnityEngine;

public class Sell : NetworkBehaviour
{
    [SerializeField]
    private MonitorArm sellMonitor;

    private bool sellEnabled;
    public class SellEntry
    {
        public ItemSO itemSO;
        public int count;
        public List<Item> linkedItemList;
        public SellEntry(ItemSO itemSO, int count, List<Item> linkedItemList)
        {
            this.itemSO = itemSO;
            this.count = count;
            this.linkedItemList = linkedItemList;
        }
        public int GetSingleItemValue()
        {
            itemSO.EvaluateCurves(GameStateManager.currentLevel.Value);
            return itemSO.sell;
        }

        public void RecalculateCount()
        {
            int _count = 0;
            foreach (Item item in linkedItemList)
            {
                _count += item.GetCount();
            }
            count = _count;
        }
        public int GetAllItemValue()
        {
            return GetSingleItemValue() * count;
        }
    }
    private List<SellEntry> sellEntryList;

    private void Start()
    {
        sellEntryList = new List<SellEntry>();
        sellMonitor.onInteract.AddListener(() =>
        {
            if (GetTotalSellValue() > 0)
            {
                sellMonitor.printer.ClearNotifications();
                sellMonitor.DisableMonitor();
                SellRpc();
            }
        });
        Money.onMoneyChanged.AddListener(() =>
        {
            UpdateMonitor();
        });
    }

    private void Update()
    {
        //remove items that have been destroyed. Mostly meant for clients
        bool refreshMonitor = false;
        for (int i = 0; i < sellEntryList.Count; i++)
        {
            for (int j = 0; j < sellEntryList[i].linkedItemList.Count; j++)
            {
                if (sellEntryList[i].linkedItemList[j] == null)
                {
                    sellEntryList[i].linkedItemList.RemoveAt(j);
                    j--;
                    refreshMonitor = true;
                }
            }

            if (refreshMonitor)
            {
                if (sellEntryList[i].linkedItemList.Count == 0)
                {
                    sellEntryList.RemoveAt(i);
                    i--;
                }
                else
                {
                    sellEntryList[i].RecalculateCount();
                }
            }
        }
        if(refreshMonitor)
            UpdateMonitor();
    }

    [Rpc(SendTo.Server)]
    private void SellRpc()
    {
        int sum = 0;
        foreach (SellEntry entry in sellEntryList)
        {
            sum += entry.GetAllItemValue();
            foreach (Item item in entry.linkedItemList)
            {
                item.DestroyItem();
            }
        }
        
        sellEntryList = new List<SellEntry>();
        
        Money.AddMoney(sum);
        //UpdateMonitor();
    }
    private void AddItem(Item item)
    {
        SellEntry sellEntry = FindSellEntry(item.itemInfo);
        if (sellEntry != null)
        {
            sellEntry.count += item.GetCount();
            sellEntry.linkedItemList.Add(item);
        }
        else
        {
            sellEntryList.Add(new SellEntry(item.itemInfo, item.GetCount(), new List<Item>() { item }));
        }

        UpdateMonitor();
    }
    private void RemoveItem(Item item)
    {
        SellEntry sellEntry = FindSellEntry(item.itemInfo);
        sellEntry.count -= item.GetCount();
        sellEntry.linkedItemList.Remove(item);

        if(sellEntry.count <= 0)
        {
            sellEntryList.Remove(sellEntry);
        }

        UpdateMonitor();
    }
    private void UpdateMonitor()
    {
        if (sellEnabled == false) return;

        sellMonitor.printer.ClearNotifications();
        if (sellEntryList.Count > 0)
        {
            Override sellListOverride = new Override("SellReceipt", OverrideType.SellReceipt);
            sellListOverride.sellReceiptOverride = sellEntryList;

            Override sumOverride = new Override("Sum", OverrideType.Text, "Sum: " + GetTotalSellValue() + "$");

            sellMonitor.printer.AddNotification(PaperRenderer.active.RenderPaper("SellReceipt", new List<Override>() { sellListOverride, sumOverride }), float.MaxValue);

            sellMonitor.EnableMonitor();
            sellMonitor.EnableButton();
        }
        else
        {

            sellMonitor.printer.AddNotification(PaperRenderer.active.RenderPaper("SellWelcome", new List<Override>()), float.MaxValue);

            sellMonitor.EnableMonitor(false);
            sellMonitor.DisableButton();
        }
    }
    public void Enable()
    {
        if (sellEnabled) return;
        sellEnabled = true;
        UpdateMonitor();
        
    }
    public void Disable()
    {
        if (sellEnabled == false) return;
        sellEnabled = false;

        sellMonitor.printer.ClearNotifications();
        sellMonitor.DisableMonitor();
    }
    private int GetTotalSellValue()
    {
        int sum = 0;
        foreach (SellEntry sellEntry in sellEntryList)
        {
            sum += sellEntry.GetAllItemValue();
        }
        return sum;
    }
    private SellEntry FindSellEntry(ItemSO itemSO)
    {
        foreach (SellEntry entry in sellEntryList)
        {
            if(entry.itemSO == itemSO)
            {
                return entry;
            }
        }
        return null;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Item item)) AddItem(item);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Item item)) RemoveItem(item);
    }
}
