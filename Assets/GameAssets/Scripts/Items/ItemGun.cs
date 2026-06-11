using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemGun : Item
{
    //Variables
    [Header("Gun")] 
    [SerializeField] 
    public int clipSize = 2;
    [SerializeField] 
    public ItemSO ammoItem;
    [SerializeField] 
    public int bulletCount = 1;
    [SerializeField] 
    public float bulletDamage = 1;
    [SerializeField] 
    private float spread = 0.05f;
    [SerializeField] 
    private float reloadCoolDown = 0.5f;
    [SerializeField] 
    private float shootCoolDown = 0.5f;
    [SerializeField] 
    private string reloadSound;
    [SerializeField] 
    private string shootSound;
    [SerializeField] 
    private string failedSound;
    [SerializeField]
    public GameObject bulletPrefab;
    [SerializeField]
    public List<HealthType> healthTypeFilter = new List<HealthType>();
    //Run Time
    private int clip;
    private float coolDown;
    void Update()
    {
        if (coolDown > 0f)
            coolDown -= Time.deltaTime;
        
       if(Player.localPlayer.inventory.tool == this && coolDown <= 0f && Time.timeScale != 0f)
       {
           if ((InputManager.attackAction.IsPressed() && clip > 0) ||(InputManager.attackAction.triggered && clip == 0))
           {
               Shoot();
           }
           else if (InputManager.reloadAction.triggered)
           {
               Reload();
           }

           UpdateText();
       }
       
       UpdateItem();
    }

    public void Shoot()
    {
        if ((clip > 0) && coolDown <= 0f)
        {
            ShootRpc(Player.localPlayer.camera.GetRaycastPos());
        }
        else if(coolDown <= 0f)
        {
            EmptyClipRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ShootRpc(Vector3 aimPos, RpcParams rpcParams = default)
    {
        GetBulletDefiningVariables(aimPos, out Vector3 bulletOrigin, out Vector3 direction);
        ShooterType shooterType = ShooterType.Player;
        
        if (NetworkManager.Singleton.LocalClientId == rpcParams.Receive.SenderClientId)
        {
            //Damage will be done by gun shooter client
            if (bulletPrefab == null)
                BulletManager.active.ShootBullets(bulletOrigin, direction, bulletCount, bulletDamage, spread, healthTypeFilter, shooterType);
            else
                BulletManager.active.ShootPrefab(bulletOrigin, direction, bulletPrefab, bulletCount, spread, shooterType);
        }
        else
        {
            //Damage isn't done. Spawn only visuals
            if (bulletPrefab == null)
                BulletManager.active.ShootBullets(bulletOrigin, direction, bulletCount, bulletDamage, spread, healthTypeFilter, shooterType,false);
            else
                BulletManager.active.ShootPrefab(bulletOrigin, direction, bulletPrefab, bulletCount, spread, shooterType,false);
        }
        
        SoundManager.active.PlayAtPos(bulletOrigin, shootSound);   
        playerWhoHasItem.avatar.animator.SetTrigger("Shoot");
        
        coolDown = shootCoolDown;
        clip--;
    }
    [Rpc(SendTo.Everyone)]
    private void EmptyClipRpc()
    {
        SoundManager.active.PlayAtPos(GetBulletOrigin(), failedSound);
        coolDown = shootCoolDown;
    }
    
    public void Reload()
    {
        ReloadRpc();
    }
    
    [Rpc(SendTo.Everyone)]
    private void ReloadRpc()
    {
        if (playerWhoHasItem.inventory.RemoveItem(ammoItem, out int itemCountRemoved, clipSize - clip))
        {
            SoundManager.active.PlayAtPos(GetBulletOrigin(), reloadSound);
            playerWhoHasItem.avatar.animator.SetTrigger("Reload");
            
            coolDown = reloadCoolDown;
            clip += itemCountRemoved;
        }
        else
        {
            SoundManager.active.PlayAtPos(GetBulletOrigin(), failedSound);
        }
    }
    public bool HasAmmoInClip()
    {
        return clip > 0;
    }
    private void UpdateText()
    {
        InventoryUI.active.SetToolText("Clip:"+clip+"/"+clipSize);
    }

    private Vector3 GetBulletOrigin()
    {
        return playerWhoHasItem.avatar.GetAnimationInfo().bulletSpawn.transform.position;
    }
    private void GetBulletDefiningVariables(Vector3 aimPos, out Vector3 bulletOrigin, out Vector3 direction)
    {
        bulletOrigin = GetBulletOrigin();
        direction = (aimPos - bulletOrigin).normalized;
    }
}
