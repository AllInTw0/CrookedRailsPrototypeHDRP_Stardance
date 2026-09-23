using Unity.Netcode;
using UnityEngine;

public class ItemTaker : Interactable
{
    [Header("Item Taker")]
    [SerializeField]
    private ItemSO itemInfo;
    [SerializeField]
    private int targetCount;
    [Header("Health")]
    [SerializeField]
    private Health linkedHealth;
    [SerializeField]
    private float healthPerItem;
    private void Update()
    {
        UpdateInteractable();
    }
    public override bool Interact()
    {
        SoundManager.active.PlayAtPos(iconPosition.position, interactSound);

        int removeCount = targetCount;
        if (linkedHealth != null)
        {
            float dammage = linkedHealth.GetMaxHealth() - linkedHealth.GetHealth();

            removeCount = Mathf.Clamp(Mathf.FloorToInt(dammage / healthPerItem), 0, targetCount);

            if (removeCount == 0)
            {
                Debug.Log("Full Health");
                SetActionNameOverride("Full!", 1f);
                return false;
            }
        }

        if(Player.localPlayer.inventory.RemoveItem(itemInfo,out int itemCountRemoved, removeCount))
        {
            if (linkedHealth != null) linkedHealth.TakeDamage(-healthPerItem * itemCountRemoved);
            SetActionNameOverride();
            return true;
        }
        else
        {
            Debug.Log("No Items");
            SetActionNameOverride("No Valid Items", 1f);
            return false;
        }
        
    }
}
