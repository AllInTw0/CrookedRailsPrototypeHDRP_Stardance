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
        if(PlayerInventory.active.tool == this && coolDown <= 0f)
        {
            if (InputManager.attackAction.IsPressed())
            {
                BeginSwing();
            }
        }
        if (coolDown > 0f)
            coolDown -= Time.deltaTime;
        UpdateItem();
    }
    public void BeginSwing()
    {

        Invoke(nameof(Swing),damageDelay);
        
        SoundManager.active.PlayAtPos(PlayerAvatar.active.GetAnimationInfo().animatedObject.transform.position, swingSound);
        PlayerAvatar.active.animator.SetTrigger("Swing");
        
        coolDown = swingCoolDown;
    }

    public void Swing()
    {
        Vector3 startPos = PlayerCamera.active.transform.position;
        Vector3 dir = PlayerCamera.active.transform.forward;

        RaycastHit hit = BulletManager.active.ShootInvisibleBullet(startPos,dir,damage,range);
        if (hit.transform != null)
        {
            SoundManager.active.PlayAtPos(hit.point, impactSound);
        }
    }
}
