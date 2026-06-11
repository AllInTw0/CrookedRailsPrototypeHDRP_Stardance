using UnityEngine;

public class ItemGiver : Interactable
{
    [Header("Item Giver")]
    [SerializeField]
    private ItemSO itemInfo;
    [SerializeField]
    private int itemCount;
    [SerializeField]
    private int maxGiveCount;
    [Header("Item Display")]
    [SerializeField]
    private ItemDisplay itemDisplay;
    private void Start()
    {
        itemDisplay.SetTarget(itemCount);
    }
    public override bool Interact()
    {
        bool sucessfull = false;
        if (itemCount > 0)
        {
            Item item = Item.SpawnItem(itemInfo, Vector3.zero, Quaternion.identity, Mathf.Min(maxGiveCount, itemCount));

            sucessfull = Player.localPlayer.inventory.TryEquipping(item);

            int countGiven = maxGiveCount;
            Debug.Log(Player.localPlayer.inventory.items.Contains(item) + ": " + item.count);
            if (Player.localPlayer.inventory.items.Contains(item) == false)
            {
                countGiven -= item.count;
                Destroy(item.gameObject);
            }

            itemCount -= countGiven;
            itemDisplay.SetTarget(itemCount);
        }

        SoundManager.active.PlayAtPos(iconPosition.position, interactSound);
        return sucessfull;
    }
}
