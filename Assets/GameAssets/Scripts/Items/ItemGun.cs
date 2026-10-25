using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

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
    public GameObject bulletPrefab;
    [SerializeField]
    public bool spawnBurstEffect = true;
    [SerializeField]
    public List<HealthType> healthTypeFilter = new List<HealthType>();
    [Header("Sound")]
    [SerializeField] 
    public string reloadSound;
    [SerializeField] 
    private string shootStartSound;
    [SerializeField] 
    private string shootEndSound;
    [SerializeField] 
    private string shootSound;
    [SerializeField] 
    public string failedSound;
    [SerializeField] 
    public AudioSource shootLoopSound;
    //Run Time
    private int clip;
    private void SetClip(int value)
    {
        clip = value;
    }
    private float coolDown;
    private NetworkVariable<bool> shootingNetwork = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);
    public override void Start()
    {
        base.Start();
        shootingNetwork.OnValueChanged += ShootingChanged;
        if(shootLoopSound)
            shootLoopSound.transform.SetParent(null);
    }
    void Update()
    {
        if (coolDown > 0f)
            coolDown -= Time.deltaTime;
        
       if(Player.localPlayer.inventory.tool == this)
       {
           shootingNetwork.Value = InputManager.attackAction.IsPressed() && HasAmmoInClip();
           if (coolDown <= 0f && Time.timeScale != 0f)
           {
               if ((InputManager.attackAction.IsPressed() && clip > 0) || (InputManager.attackAction.triggered && clip == 0))
               {
                   Shoot();
               }
               else if (InputManager.reloadAction.triggered)
               {
                   Reload();
               }

               UpdateText();
           }
       }
       if(shootLoopSound)
           shootLoopSound.transform.position = GetBulletOrigin();
       
       UpdateItem();
    }

    private void ShootingChanged(bool previous, bool current)
    {
        Debug.Log("Shooting: "+current);
        if (current)
        {
            if(shootLoopSound)
                shootLoopSound.Play();
            SoundManager.active.PlayAtPos(GetBulletOrigin(), shootStartSound);
        }
        else
        {
            if(shootLoopSound)
                shootLoopSound.Stop();
            SoundManager.active.PlayAtPos(GetBulletOrigin(), shootEndSound);
        }
    }

    private bool CanShoot()
    {
        return !IsOnCooldown() && HasAmmoInClip();
    }
    public void Shoot()
    {
        if (CanShoot())
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
        
        if (NetworkManager.Singleton.LocalClientId == rpcParams.Receive.SenderClientId)
        {
            //Damage will be done by gun shooter client
            Shoot(bulletOrigin, direction);
        }
        else
        {
            //Damage isn't done. Spawn only visuals
            Shoot(bulletOrigin, direction, false);
        }
        
        playerWhoHasItem.avatar.animator.SetTrigger("Shoot");
    }

    public void Shoot(Vector3 bulletOrigin, Vector3 direction, bool doDamage = true, ShooterType shooterType = ShooterType.Player)
    {
        if (bulletPrefab == null)
            BulletManager.active.ShootBullets(bulletOrigin, direction, bulletCount, bulletDamage, spread, healthTypeFilter, shooterType, doDamage);
        else
            BulletManager.active.ShootPrefab(bulletOrigin, direction, bulletPrefab, bulletCount, spread, shooterType, doDamage, spawnBurstEffect);
        
        SoundManager.active.PlayAtPos(bulletOrigin, shootSound); 
        
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
        if (playerWhoHasItem.inventory.RemoveItem(ammoItem, out int itemCountRemoved, clipSize - clip, IsServer))
        {
            playerWhoHasItem.avatar.animator.SetTrigger("Reload");
            ReloadClient(itemCountRemoved, GetBulletOrigin());
        }
        else
        {
            SoundManager.active.PlayAtPos(GetBulletOrigin(), failedSound);
        }
    }

    public void ReloadClient(int value, Vector3 bulletOrigin)
    {
        if(value > 0)
            SoundManager.active.PlayAtPos(bulletOrigin, reloadSound);
        else
            SoundManager.active.PlayAtPos(bulletOrigin, failedSound);
        coolDown = reloadCoolDown;
        clip += value;
    }
    public bool HasAmmoInClip()
    {
        return clip > 0;
    }

    public bool IsOnCooldown()
    {
        return coolDown > 0f;
    }

    public float GetCooldown()
    {
        return coolDown;
    }
    private void UpdateText()
    {
        InventoryUI.active.SetToolText("Clip:"+clip+"/"+clipSize);
    }

    private Vector3 GetBulletOrigin()
    {
        if(!playerWhoHasItem)
            return transform.position;
        
        return playerWhoHasItem.avatar.GetAnimationInfo().bulletSpawn.transform.position;
    }
    private void GetBulletDefiningVariables(Vector3 aimPos, out Vector3 bulletOrigin, out Vector3 direction)
    {
        bulletOrigin = GetBulletOrigin();
        direction = (aimPos - bulletOrigin).normalized;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if(shootLoopSound)
            Destroy(shootLoopSound);
    }
}
