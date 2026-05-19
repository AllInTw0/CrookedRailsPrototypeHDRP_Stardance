using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory active;
    
    [NonSerialized]
    public Item tool; //The hand held item
    [NonSerialized]
    public List<Item> items = new List<Item>(); //Items in inventory
    
    public int slotCount = 3;
    private int filledSlots = 0;
    
    [NonSerialized]
    public int selectIndex;
    private void Start()
    {
        active = this;
    }

    private void Update()
    {
        //index range is 0/1 to items.Count
        // 0 if tool is equipped
        // 1 if no tool is equipped
        selectIndex += (int)InputManager.scrollAction.ReadValue<float>();
        UpdateSelectIndexInterval();
        //Debug.Log("Current selectIndex: "+selectIndex);
        
        //Dropping Items
        if (InputManager.dropAction.triggered && (PlayerInventory.active.items.Count > 0 || PlayerInventory.active.tool != null))
        {
            if (selectIndex == 0)
                UnEquipTool();
            else
                UnEquipItem(selectIndex - 1);
        }

        //Swaping Tools
        if (InputManager.swapToolAction.triggered)
        {
            if(selectIndex > 0 && items.Count > 0 && items[selectIndex - 1].itemInfo.isTool)
            {
                Item toolRefrence = tool == null ? null : UnEquipTool();
                Item newTool = UnEquipItem(selectIndex - 1);

                if (newTool != null)
                    newTool.Interact();
                if(toolRefrence != null)
                    toolRefrence.Interact();

                //TryEquipping(newTool);
                //TryEquipping(toolRefrence);

                selectIndex = items.Count;
                UpdateSelectIndexInterval();
            }
        }
    }

    public bool TryEquipping(Item item)
    {
        if(item.itemInfo.isTool && tool == null)
        {
            EquipAsTool(item);
            return true;
        }

        if (item.itemInfo.maxCount > 1)
        {
            //Stackable Item
            foreach (var equippedItem in items)
            {
                if (equippedItem.itemInfo == item.itemInfo && equippedItem.count < equippedItem.itemInfo.maxCount)
                {
                    EquipStackableItem(equippedItem, item);
                    return true;
                }
            }
        }
        
        if(item.itemInfo.slotCount <= (slotCount - filledSlots))
        {
            EquipAsItem(item);
            return true;
        }

        return false;
    }

    private void EquipAsTool(Item item)
    {
        tool = item;
        item.transform.SetParent(null);
        item.BecomeInvisible();
        PlayerAvatar.active.EquipTool(item);
        Debug.Log(item + " Equipped As Tool");

        MovingPlatformManager.active.RemoveEntry(item.transform);
        InventoryUI.active.UpdateToolIcon();
    }
    private void EquipAsItem(Item item)
    {
        items.Add(item);
        filledSlots += item.itemInfo.slotCount;

        item.transform.SetParent(null);
        item.BecomeInvisible();
        Debug.Log(item + " Equipped As Item");

        MovingPlatformManager.active.RemoveEntry(item.transform);
        InventoryUI.active.UpdateItemIcons();
    }

    private void EquipStackableItem(Item equippedItem, Item item)
    {
        int sum = equippedItem.count + item.count;
        if (sum <= equippedItem.itemInfo.maxCount)
        {
            equippedItem.count = sum;
            item.count = 0;
            MovingPlatformManager.active.RemoveEntry(item.transform);
            PlayerInteract.active.StopInteracting();
            Destroy(item.gameObject);
        }
        else
        {
            int leftover = sum - equippedItem.itemInfo.maxCount;
            equippedItem.count = equippedItem.itemInfo.maxCount;

            item.count = leftover;
            
            InteractIcon.active.Refresh();//Update the item.count on the interact icon
        }
        Debug.Log(item + " Equipped As Stackable Item");
        InventoryUI.active.UpdateItemIcons();
    }
    public Item UnEquipTool()
    {
        if (tool == null)
            return null;

        ToolAnimationInfo animInfo = PlayerAvatar.active.GetAnimationInfo();
        
        Quaternion rot = Quaternion.identity;
        Vector3 pos = transform.position + Vector3.up;
        if (animInfo != null)
        {
            rot = animInfo.animatedObject.transform.rotation;
            pos = animInfo.animatedObject.transform.position;
        }
        
        tool.BecomeVisible();
        tool.transform.rotation = rot;
        tool.DropFromPos(pos);

        Item refrence = tool;

        tool = null;
        UpdateSelectIndexInterval();
        PlayerAvatar.active.UnEquipTool();
        InventoryUI.active.UpdateToolIcon();

        return refrence;
    }
    private Item UnEquipItem(int index)
    {
        Item item = items[index];
        items.RemoveAt(index);
        filledSlots -= item.itemInfo.slotCount;
        
        item.BecomeVisible();

        ItemSO itemInfo = item.itemInfo;
        Transform orientation = PlayerMovement.active.orientation;
        item.transform.rotation = Quaternion.Euler(Random.Range(-90f, 90f), itemInfo.randomYRot ? Random.Range(0f, 360f) : orientation.eulerAngles.y, Random.Range(-90f, 90f));
        item.DropFromPos(transform.position + Vector3.up + orientation.forward * itemInfo.dropOffset.z + orientation.up * itemInfo.dropOffset.y + orientation.right * itemInfo.dropOffset.x);

        if(selectIndex > items.Count)
            selectIndex--;
        UpdateSelectIndexInterval();
        InventoryUI.active.UpdateItemIcons();
        Debug.Log(item + " Dropped As Item");

        return item;
    }

    public bool RemoveItem(ItemSO itemInfo, out int itemCountRemoved, int targetCount = 1)
    {
        itemCountRemoved = 0;

        //Loop through items in inventory and get the amount of ammo needed
        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item.itemInfo == itemInfo)
            {
                int ammoToFind = targetCount - itemCountRemoved;
                for (int j = 0; j < ammoToFind; j++)
                {
                    item.count--;
                    itemCountRemoved++;
                    if (item.count <= 0)
                    {
                        DestroyItem(item);
                        i--;
                        break;
                    }

                    if (itemCountRemoved == targetCount)
                        break;
                }
            }
            if (itemCountRemoved == targetCount)
                break;
        }

        InventoryUI.active.UpdateItemIcons();

        return itemCountRemoved != 0;
    }
    public void DestroyItem(Item item)
    {
        items.Remove(item);
        filledSlots -= item.itemInfo.slotCount;
        
        Destroy(item.gameObject);
        
        UpdateSelectIndexInterval();
        InventoryUI.active.UpdateItemIcons();
        Debug.Log(item + " Destroyed");
    }
    private void UpdateSelectIndexInterval()
    {
        if (tool != null)
        {
            if (selectIndex < 0)
                selectIndex = items.Count;
            else if (selectIndex > items.Count)
                selectIndex = 0;
        }
        else
        {
            if (selectIndex < 1)
                selectIndex = items.Count;
            else if (selectIndex > items.Count)
                selectIndex = 1; 
        }
    }

    public void UnEquipAll()
    {
        if (tool != null)
        {
            UnEquipTool();
        }

        while ( items.Count > 0)
        {
            UnEquipItem(0);
        }
    }
}
