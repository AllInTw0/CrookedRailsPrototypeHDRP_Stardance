using Unity.Netcode;
using UnityEngine;

public class ItemMelee : Item
{
    //Variables
    [Header("Melee")]
    public float damage;
    public float range;
    public float swingCoolDown;
    public float damageDelay = 0.1f;
    
    public string swingSound;
    public string impactSound;
    //Run Time
    private float coolDown;
    void Update()
    {
        UpdateMelee();
    }

    public void UpdateMelee()
    {
        if(Player.localPlayer.inventory.tool == this && coolDown <= 0f)
        {
            if (InputManager.attackAction.IsPressed())
            {
                BeginSwingRpc();
            }
        }
        if (coolDown > 0f)
            coolDown -= Time.deltaTime;
        UpdateItem();
    }
    [Rpc(SendTo.Everyone)]
    public void BeginSwingRpc()
    {
        Invoke(nameof(Swing),damageDelay);
        
        SoundManager.active.PlayAtPos(playerWhoHasItemClient.avatar.GetAnimationInfo().animatedObject.transform.position, swingSound);
        playerWhoHasItemClient.avatar.animator.SetTrigger("Swing");
        
        coolDown = swingCoolDown;
    }

    public void Swing()
    {
        playerWhoHasItemClient.GetCameraVectors(out Vector3 origin, out Vector3 dir);

        RaycastHit hit = BulletManager.active.ShootInvisibleBullet(origin, dir, IsOwner ? damage : 0, range);
        if (hit.transform != null)
        {
            SoundManager.active.PlayAtPos(hit.point, impactSound);
        }
    }
}
