using UnityEngine;

public class WaterInteractable : Interactable
{
    [Header("Water Interactalbe")]
    [SerializeField]
    private ItemSO bucketItem;
    [SerializeField]
    private bool fillItem;
    [SerializeField]
    private Health targetHealth;
    [SerializeField]
    private Collider interactableCollider;
    void Update()
    {
        if (Player.localPlayer.inventory.tool != null)
            interactableCollider.enabled = Player.localPlayer.inventory.tool.itemInfo == bucketItem;
        else
            interactableCollider.enabled = false;

        UpdateInteractable();
    }
    public override bool Interact()
    {
        base.Interact(); //Sound

        ItemBucket bucketScript = (ItemBucket)Player.localPlayer.inventory.tool;
        if (fillItem)
        {
            if (bucketScript.IsFilled() == false)
            {
                bucketScript.Fill();
            }
            else
            {
                SetActionNameOverride("Bucket Full!", 0.5f);
                return false;
            }
        }
        else
        {
            if (bucketScript.IsFilled())
            {
                float waterAmmount = bucketScript.GetWaterAmmount();
                if(targetHealth.GetHealth() <= targetHealth.maxHealth - waterAmmount)
                {
                    bucketScript.Empty();
                    targetHealth.TakeDamage(-waterAmmount);
                }
                else
                {
                    SetActionNameOverride("Full!", 0.5f);
                    return false;
                }
                
            }
            else
            {
                SetActionNameOverride("Bucket Empty!", 0.5f);
                return false;
            }
        }

        return true;
    }
}
