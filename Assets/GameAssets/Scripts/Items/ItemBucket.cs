using UnityEngine;

public class ItemBucket : Item
{
    [Header("Bucket")]
    [SerializeField]
    private float waterAmmount;
    [SerializeField]
    private GameObject waterObject;
    [Header("Sound")]
    [SerializeField]
    private string fillUpSound;
    [SerializeField]
    private string emptySound;

    private bool filled;
    public override bool TryEquipping(Player targetPlayer = null, bool sendRpc = true)
    {
        bool equiped = base.TryEquipping(targetPlayer,sendRpc);

        if (equiped)
        {
            ToolAnimationInfo toolAnimInfo = targetPlayer.avatar.GetAnimationInfo();
            toolAnimInfo.animatedObject.transform.GetChild(0).Find("BucketFluid").gameObject.SetActive(filled);
        }

        return equiped;
    }
    private void Update()
    {
        UpdateItem();
    }

    public bool IsFilled()
    {
        return filled;
    }

    public float GetWaterAmmount()
    {
        return waterAmmount;
    }

    public void Fill()
    {
        ToolAnimationInfo toolAnimInfo = playerWhoHasItem.avatar.GetAnimationInfo();
        SoundManager.active.PlayAtPos(toolAnimInfo.animatedObject.transform.position, fillUpSound);

        toolAnimInfo.animatedObject.transform.GetChild(0).Find("BucketFluid").gameObject.SetActive(true);
        waterObject.SetActive(true);

        filled = true;
    }
    public void Empty()
    {
        ToolAnimationInfo toolAnimInfo = playerWhoHasItem.avatar.GetAnimationInfo();
        SoundManager.active.PlayAtPos(toolAnimInfo.animatedObject.transform.position, emptySound);

        toolAnimInfo.animatedObject.transform.GetChild(0).Find("BucketFluid").gameObject.SetActive(false);
        waterObject.SetActive(false);

        filled = false;
    }
}
