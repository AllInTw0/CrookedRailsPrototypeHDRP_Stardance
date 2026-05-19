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
    public override bool Interact()
    {
        bool equiped = base.Interact();

        if (equiped && PlayerInventory.active.tool == this)
        {
            ToolAnimationInfo toolAnimInfo = PlayerAvatar.active.GetAnimationInfo();
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
        ToolAnimationInfo toolAnimInfo = PlayerAvatar.active.GetAnimationInfo();
        SoundManager.active.PlayAtPos(toolAnimInfo.animatedObject.transform.position, fillUpSound);

        toolAnimInfo.animatedObject.transform.GetChild(0).Find("BucketFluid").gameObject.SetActive(true);
        waterObject.SetActive(true);

        filled = true;
    }
    public void Empty()
    {
        ToolAnimationInfo toolAnimInfo = PlayerAvatar.active.GetAnimationInfo();
        SoundManager.active.PlayAtPos(toolAnimInfo.animatedObject.transform.position, emptySound);

        toolAnimInfo.animatedObject.transform.GetChild(0).Find("BucketFluid").gameObject.SetActive(false);
        waterObject.SetActive(false);

        filled = false;
    }
}
