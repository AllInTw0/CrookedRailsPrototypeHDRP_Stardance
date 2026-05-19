using System.Collections.Generic;
using UnityEngine;

public class Sell : MonoBehaviour
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
            itemSO.EvaluateCurves(GameStateManager.currentLevel);
            return itemSO.sell;
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
            int sum = 0;
            foreach (SellEntry entry in sellEntryList)
            {
                sum += entry.GetAllItemValue();
                foreach (Item item in entry.linkedItemList)
                {
                    Destroy(item.gameObject);
                }
            }

            Money.AddMoney(sum);
            sellEntryList = new List<SellEntry>();
            UpdateMonitor();
        });
    }
    private void AddItem(Item item)
    {
        SellEntry sellEntry = FindSellEntry(item.itemInfo);
        if (sellEntry != null)
        {
            sellEntry.count += item.count;
            sellEntry.linkedItemList.Add(item);
        }
        else
        {
            sellEntryList.Add(new SellEntry(item.itemInfo, item.count, new List<Item>() { item }));
        }

        UpdateMonitor();
    }
    private void RemoveItem(Item item)
    {
        SellEntry sellEntry = FindSellEntry(item.itemInfo);
        sellEntry.count -= item.count;
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
