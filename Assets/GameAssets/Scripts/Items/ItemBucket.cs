using Unity.Netcode;
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

    private NetworkVariable<bool> filled = new(writePerm:NetworkVariableWritePermission.Owner);
    public override void OnNetworkSpawn()
    {
        filled.OnValueChanged += FilledChanged;
    }

    public override bool TryEquipping(Player targetPlayer = null, bool sendRpc = true)
    {
        bool equiped = base.TryEquipping(targetPlayer,sendRpc);

        if (equiped)
        {
            if (targetPlayer == null)
                targetPlayer = Player.localPlayer;
            ToolAnimationInfo toolAnimInfo = targetPlayer.avatar.GetAnimationInfo();
            toolAnimInfo.animatedObject.transform.GetChild(0).Find("BucketFluid").gameObject.SetActive(IsFilled());
        }

        return equiped;
    }
    private void Update()
    {
        UpdateItem();
    }

    private void FilledChanged(bool previous, bool current)
    {
        UpdateVisual(current);
    }

    private void UpdateVisual(bool isFilled)
    {
        ToolAnimationInfo toolAnimInfo = playerWhoHasItemClient.avatar.GetAnimationInfo();
        
        SoundManager.active.PlayAtPos(toolAnimInfo.animatedObject.transform.position,isFilled ? fillUpSound : emptySound);
        
        toolAnimInfo.animatedObject.transform.GetChild(0).Find("BucketFluid").gameObject.SetActive(isFilled);
        waterObject.SetActive(isFilled);
    }
    public bool IsFilled()
    {
        return filled.Value;
    }

    public float GetWaterAmmount()
    {
        return waterAmmount;
    }

    [Rpc(SendTo.Owner)]
    public void FillOwnerRpc()
    {
        filled.Value = true;
    }
    [Rpc(SendTo.Owner)]
    public void EmptyOwnerRpc()
    {
        filled.Value = false;
    }
}
