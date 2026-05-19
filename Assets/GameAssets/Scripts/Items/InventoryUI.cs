using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI active;
    //Variables
    [SerializeField] 
    private RectTransform inventoryParent;
    [Header("Item")]
    [SerializeField] 
    private RectTransform imagePrefab;
    [SerializeField] 
    private RectTransform itemCountPrefab;
    [SerializeField] 
    private Sprite itemSlotSprite;
    [SerializeField] 
    private RectTransform itemSlotParent;
    [SerializeField] 
    private RectTransform itemIconParent;
    
    [Header("Tool")]
    [SerializeField] 
    private RectTransform toolSlot;
    [SerializeField] 
    private TMP_Text toolText;
    [SerializeField] 
    private RawImage toolImage;

    [Header("Selector")] 
    [SerializeField] 
    private RectTransform selector;
    [SerializeField] 
    private float selectorSpeed;
    [Header("Temp")]
    [SerializeField] 
    private Sprite itemIconTempSprite;
    
    //Run Time
    private List<RectTransform> itemSlots = new List<RectTransform>();
    private List<RectTransform> itemIcons = new List<RectTransform>();
    
    private void Start()
    {
        active = this;
    }

    private void Update()
    {
        if (itemSlots.Count < PlayerInventory.active.slotCount)
        {
            AddItemSlot();
        }

        if (PlayerInventory.active.items.Count > 0 || PlayerInventory.active.tool != null)
        {
            //Has Items
            if(!selector.gameObject.activeSelf)
                selector.gameObject.SetActive(true);
            
            Vector3 targetPos;
            if (PlayerInventory.active.selectIndex == 0)
                targetPos = toolImage.rectTransform.position;
            else
                targetPos = itemIcons[PlayerInventory.active.selectIndex - 1].position;
            
            targetPos = new Vector3(targetPos.x, 0f, 0f);
            selector.position = Vector3.Lerp(selector.position, targetPos, Mathf.Clamp01(selectorSpeed * Time.deltaTime));
        }
        else
        {
            //No Items
            if(selector.gameObject.activeSelf)
                selector.gameObject.SetActive(false);
        }
    }

    private void AddItemSlot()
    {
        RectTransform newItemSlot = Instantiate(imagePrefab, itemSlotParent);
        newItemSlot.GetComponent<RawImage>().texture = itemSlotSprite.texture;
        
        itemSlots.Add(newItemSlot);
        UpdateItemIcons();
    }

    public void UpdateItemIcons()
    {
        foreach (var itemIcon in itemIcons)
        {
            Destroy(itemIcon.gameObject);
        }

        itemIcons = new List<RectTransform>();
        
        int slotIndex = 0;
        foreach (Item item in PlayerInventory.active.items)
        {
            RectTransform newItemIcon = Instantiate(imagePrefab, itemIconParent);
            
            if(item.itemInfo.icon != null)
                newItemIcon.GetComponent<RawImage>().texture = item.itemInfo.icon;
            else
                newItemIcon.GetComponent<RawImage>().texture = itemIconTempSprite.texture;
            
            itemIcons.Add(newItemIcon);
            
            Vector3 itemSlotPosSum = Vector3.zero;
            for (int i = 0; i < item.itemInfo.slotCount; i++)
            {
                itemSlotPosSum += itemSlots[slotIndex].position;
                slotIndex++;
            }

            newItemIcon.position = itemSlotPosSum / item.itemInfo.slotCount;
            
            //Item Count
            if (item.itemInfo.maxCount > 1)
            {
                RectTransform newCountText = Instantiate(itemCountPrefab, newItemIcon);
                newCountText.GetComponent<TMP_Text>().text = item.count + "/" + item.itemInfo.maxCount;
                newCountText.position = newItemIcon.position;
            }
        }
        
    }
    public void UpdateToolIcon()
    {
        if (PlayerInventory.active.tool != null)
        {
            if(PlayerInventory.active.tool.itemInfo.icon != null)
                toolImage.texture = PlayerInventory.active.tool.itemInfo.icon;
            else
                toolImage.texture = itemIconTempSprite.texture;
            
            toolImage.enabled = true;
        }
        else
        {
            toolImage.enabled = false;
            toolText.text = "";
        }
    }

    public void SetToolText(string text)
    {
        toolText.text = text;
    }

    public void Hide()
    {
        inventoryParent.gameObject.SetActive(false);
    }
    public void UnHide()
    {
        inventoryParent.gameObject.SetActive(true);
    }
}
