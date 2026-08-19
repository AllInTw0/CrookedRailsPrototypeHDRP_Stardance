using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerInventory : NetworkBehaviour
{
    private NetworkVariable<int> slotCount = new (writePerm:NetworkVariableWritePermission.Server);

    public int GetSlotCount()
    {
        return slotCount.Value;
    }
    
    [NonSerialized]
    public Item tool; //The hand held item
    [NonSerialized]
    public List<Item> items = new List<Item>(); //Items in inventory
    
    public int startingSlotCount = 3;
    private int filledSlots = 0;
    
    [NonSerialized]
    public int selectIndex;

    [NonSerialized] 
    public Player player;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            slotCount.Value = startingSlotCount;
    }

    private void Update()
    {
        if(IsOwner == false)
            return;
        
        //index range is 0/1 to items.Count
        // 0 if tool is equipped
        // 1 if no tool is equipped
        selectIndex += (int)InputManager.scrollAction.ReadValue<float>();
        UpdateSelectIndexInterval();
        //Debug.Log("Current selectIndex: "+selectIndex);
        
        //Dropping Items
        if (InputManager.dropAction.triggered && (items.Count > 0 || tool != null))
        {
            if (selectIndex == 0)
            {
                Item item = UnEquipTool();
                item.UnEquipItemServerRpc();
            }
            else
            {
                Item item = UnEquipItem(items[selectIndex - 1]);
                item.UnEquipItemServerRpc();
            }
        }

        //Swaping Tools
        if (InputManager.swapToolAction.triggered)
        {
            if(selectIndex > 0 && items.Count > 0 && items[selectIndex - 1].itemInfo.isTool)
            {
                Item toolRefrence = tool == null ? null : UnEquipTool();
                Item newTool = UnEquipItem(items[selectIndex - 1]);
                
                SwapToolRpc(toolRefrence, newTool);
                
                selectIndex = items.Count;
                UpdateSelectIndexInterval();
            }
        }
    }
    [Rpc(SendTo.Everyone)]
    private void SwapToolRpc(NetworkBehaviourReference _oldTool, NetworkBehaviourReference _newTool)
    {
        _oldTool.TryGet(out Item oldTool);
        _newTool.TryGet(out Item newTool);
        
        if (oldTool == tool)
            UnEquipTool();
        UnEquipItem(newTool);
        
        TryEquipping(newTool);
        if(oldTool)
            TryEquipping(oldTool);
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
                if (equippedItem.itemInfo == item.itemInfo && equippedItem.GetCount() < equippedItem.itemInfo.maxCount)
                {
                    EquipStackableItem(equippedItem, item);
                    return true;
                }
            }
        }
        
        if(item.itemInfo.slotCount <= (GetSlotCount() - filledSlots))
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
        player.avatar.EquipTool(item);
        Debug.Log(item + " Equipped As Tool");
        
        MovingPlatformManager.active.RemoveEntry(item.transform);
        InventoryUI.active.UpdateToolIcon();

        item.playerWhoHasItem = player;
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
        
        item.playerWhoHasItem = player;
    }

    private void EquipStackableItem(Item equippedItem, Item item)
    {
        Debug.Log(item + " Equipped As Stackable Item");

        if (IsServer)
        {
            int sum = equippedItem.GetCount() + item.GetCount();
            if (sum <= equippedItem.itemInfo.maxCount)
            {
                equippedItem.SetCountServerRpc(sum);
                MovingPlatformManager.active.RemoveEntry(item.transform);
                if(IsOwner) PlayerInteract.active.StopInteracting();
                item.DestroyItem();
            }
            else
            {
                int leftover = sum - equippedItem.itemInfo.maxCount;
                equippedItem.SetCountServerRpc(equippedItem.itemInfo.maxCount);

                item.SetCountServerRpc(leftover);
            
                InteractIcon.active.Refresh();//Update the item.count on the interact icon
            }
        }

        InventoryUI.active.UpdateItemIcons();
    }
    public Item UnEquipTool()
    {
        if (tool == null)
            return null;

        ToolAnimationInfo animInfo = player.avatar.GetAnimationInfo();
        
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
        
        tool.playerWhoHasItem = null;
        
        Item reference = tool;

        tool = null;
        UpdateSelectIndexInterval();
        player.avatar.UnEquipTool();
        InventoryUI.active.UpdateToolIcon();

        return reference;
    }
    public Item UnEquipItem(Item item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            filledSlots -= item.itemInfo.slotCount;

            item.BecomeVisible();

            ItemSO itemInfo = item.itemInfo;
            Transform orientation = player.orientation;
            item.transform.rotation = Quaternion.Euler(Random.Range(-90f, 90f),
                itemInfo.randomYRot ? Random.Range(0f, 360f) : orientation.eulerAngles.y, Random.Range(-90f, 90f));
            item.DropFromPos(transform.position + Vector3.up + orientation.forward * itemInfo.dropOffset.z +
                             orientation.up * itemInfo.dropOffset.y + orientation.right * itemInfo.dropOffset.x);

            item.playerWhoHasItem = null;

            if (selectIndex > items.Count)
                selectIndex--;
            UpdateSelectIndexInterval();
            InventoryUI.active.UpdateItemIcons();
            Debug.Log(item + " Dropped As Item");
        }

        return item;
    }

    public bool RemoveItem(ItemSO itemInfo, out int itemCountRemoved, int targetCount = 1, bool changeCount = true)
    {
        itemCountRemoved = 0;

        //Loop through items in inventory and get the amount of ammo needed
        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item.itemInfo == itemInfo)
            {
                int ammoToFind = targetCount - itemCountRemoved;
                int count = item.GetCount();
                for (int j = 0; j < ammoToFind; j++)
                {
                    count--;
                    itemCountRemoved++;
                    if (count <= 0 && changeCount)
                    {
                        item.DestroyItem();
                        i--;
                        break;
                    }

                    if (itemCountRemoved == targetCount)
                        break;
                }
                if(item && changeCount && item.IsSpawned) //IsSpawned cheks if item wasnt destroyed
                    item.SetCountServerRpc(count);
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
    [Rpc(SendTo.Server)]
    public void ModifySlotCountRpc(int amount)
    {
        slotCount.Value += amount;
    }
    public void UnEquipAll()
    {
        if (tool != null)
        {
            tool.UnEquipItemServerRpc();
        }
        
        while ( items.Count > 0)
        {
            items[0].UnEquipItemServerRpc();
            UnEquipItem(items[0]);
        }
    }
}