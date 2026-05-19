using System.Collections.Generic;
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
        
       if(PlayerInventory.active.tool == this && coolDown <= 0f && Time.timeScale != 0f)
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

    public void Shoot(bool ingoreClipSize = false, Transform bulletSpawnOverride = null, ShooterType shooterType = ShooterType.Player)
    {
        if ((clip > 0 || ingoreClipSize) && coolDown <= 0f)
        {
            Vector3 startPos;
            Vector3 dir;
            if (bulletSpawnOverride == null)
            {
                //Triggered by player
                startPos = GetBulletOrigin();
                dir = (PlayerCamera.active.GetRaycastPos() - startPos).normalized;
                PlayerAvatar.active.animator.SetTrigger("Shoot");
            }
            else
            {
                //Triggered by sentry
                startPos = bulletSpawnOverride.position;
                dir = bulletSpawnOverride.forward;
            }

            if (bulletPrefab == null)
                BulletManager.active.ShootBullets(startPos, dir, bulletCount, bulletDamage, spread, healthTypeFilter, shooterType);
            else
                BulletManager.active.ShootPrefab(startPos, dir, bulletPrefab, bulletCount, spread, shooterType);

            SoundManager.active.PlayAtPos(startPos, shootSound);   

            coolDown = shootCoolDown;
            clip--;
        }
        else if(coolDown <= 0f)
        {
            if(bulletSpawnOverride == null)
                //Triggered by player
                SoundManager.active.PlayAtPos(GetBulletOrigin(), failedSound);
            else
                //Triggered by sentry
                SoundManager.active.PlayAtPos(bulletSpawnOverride.position, failedSound);

            coolDown = shootCoolDown;
        }
    }

    public void Reload(int ammoCountOverride = -1, Transform bulletSpawnOverride = null)
    {
        int itemCountRemoved = ammoCountOverride;
        if (ammoCountOverride > 0 || PlayerInventory.active.RemoveItem(ammoItem, out itemCountRemoved, clipSize - clip))
        {
            if (bulletSpawnOverride)
                //Triggered by sentry
                SoundManager.active.PlayAtPos(bulletSpawnOverride.position, reloadSound);
            else
            {
                //Triggered by player
                SoundManager.active.PlayAtPos(GetBulletOrigin(), reloadSound);
                PlayerAvatar.active.animator.SetTrigger("Reload");
            }
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
        return PlayerAvatar.active.GetAnimationInfo().animatedObject.transform.position;
    }
}
