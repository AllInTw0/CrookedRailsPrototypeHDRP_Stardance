using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HighDefinition.CameraSettings;

public class Sentry : Item
{
    [Header("Rotation")]
    [SerializeField]
    private Transform rotYTransform;
    [SerializeField]
    private float rotYSpeed;
    [SerializeField]
    private Transform rotXTransform;
    [SerializeField]
    private float rotXSpeed;
    [Header("Targeting")]
    [SerializeField]
    private List<HealthType> healthTypeFilterList;
    [SerializeField]
    private float maxDistance;
    [SerializeField]
    private float maxAngleDiff;
    [SerializeField]
    private bool angleMode;
    [System.Serializable]
    public class GunVisual
    {
        public ItemSO itemInfo;
        public GameObject visualParent;
        public GameObject pistonTarget;
        public Transform bulletSpawn;
    }
    [Header("Gun Visual")]
    [SerializeField]
    public List<GunVisual> gunVisualList;
    [SerializeField]
    public FollowObject gunPistonFollow;
    [Header("Ammo Visual")]
    [SerializeField]
    private List<Light> ammoLightList;
    [SerializeField]
    private List<Color> lightColorList;
    [Header("Interactables")]
    [SerializeField]
    private EventInteractable itemInteractable;
    [SerializeField]
    private EventInteractable ammoFillInteractable;
    [Header("Ammo")]
    [SerializeField]
    private int maxAmmoCount;
    [SerializeField]
    private int ammoCount;
    [Header("Health")]
    [SerializeField]
    private Health sentryHealth;
    [SerializeField]
    private ItemSO destroyedSentryItem;
    [Header("Sound")]
    [SerializeField]
    private string ammoRefillString;
    [SerializeField]
    private string sentryBreakString;
    //RunTime
    private ItemGun equipedGun;
    private GunVisual equipedGunVisual;

    private Vector2 targetRot;
    private Transform helperTransform;
    void Start()
    {
        helperTransform = new GameObject().transform;

        itemInteractable.interactEvent.AddListener(() =>
        {
            if (equipedGun == null)
            {
                if (PlayerInventory.active.tool && PlayerInventory.active.tool is ItemGun)
                {
                    //Equip Tool
                    ItemGun tool = (ItemGun)PlayerInventory.active.UnEquipTool();
                    tool.BecomeInvisible();
                    foreach (GunVisual visual in gunVisualList)
                    {
                        if (visual.itemInfo == tool.itemInfo)
                        {
                            visual.visualParent.SetActive(true);
                            gunPistonFollow.target = visual.pistonTarget.transform;

                            equipedGunVisual = visual;
                        }
                    }
                    itemInteractable.SetObjectNameOverride(tool.itemInfo.name);
                    itemInteractable.SetActionNameOverride("Pick Up");
                    equipedGun = tool;
                    ammoFillInteractable.gameObject.SetActive(true);
                    interactableCollider.enabled = false;
                }
            }
            else
            {

                if (PlayerInventory.active.TryEquipping(equipedGun))
                {
                    //UnEquip Tool
                    foreach (GunVisual visual in gunVisualList)
                    {
                        visual.visualParent.SetActive(false);
                    }
                    DropAmmo();

                    itemInteractable.ClearOverrides();
                    equipedGun = null;
                    equipedGunVisual = null;
                    ammoFillInteractable.gameObject.SetActive(false);
                    interactableCollider.enabled = true;

                    UpdateAmmoLights();
                }
                
            }
        });

        ammoFillInteractable.interactEvent.AddListener(() =>
        {
            int targetCount = Mathf.Clamp(maxAmmoCount - ammoCount, 0, equipedGun.ammoItem.maxCount / 2);
            if (targetCount > 0 && PlayerInventory.active.RemoveItem(equipedGun.ammoItem, out int itemCountRemoved, targetCount))
            {
                SoundManager.active.PlayAtPos(ammoFillInteractable.transform.position, ammoRefillString);
                ammoCount += itemCountRemoved;
                UpdateAmmoLights();
            }
        });

        sentryHealth.onTakeDamage.AddListener((float dammage, Vector3 force) =>
        {
            if (sentryHealth.health == 0)
            {
                //Destroy Sentry

                if (equipedGun)
                {
                    DropAmmo();
                    equipedGun.BecomeVisible();
                    equipedGun.transform.rotation = transform.rotation;
                    equipedGun.DropFromPos(transform.position + Vector3.up);
                }
                SoundManager.active.PlayAtPos(transform.position, sentryBreakString);
                Item.SpawnItem(destroyedSentryItem, transform.position + Vector3.up * 0.2f, transform.rotation);
                Destroy(gameObject);
            }
        });

        ammoFillInteractable.gameObject.SetActive(false);
        foreach (GunVisual visual in gunVisualList)
        {
            visual.visualParent.SetActive(false);
        }
        UpdateAmmoLights();
    }
    private void DropAmmo()
    {
        if (equipedGun == null)
            return;
        //Drop ammo
        while (ammoCount > 0)
        {
            int count = Mathf.Clamp(equipedGun.ammoItem.maxCount, 0, ammoCount);
            Item.SpawnItem(equipedGun.ammoItem, transform.position + Random.onUnitSphere + Vector3.up, Quaternion.Euler(0f, 0f, 0f), count);
            ammoCount -= count;
        }
    }
    public override bool Interact()
    {
        SoundManager.active.PlayAtPos(iconPosition != null ? iconPosition.position : transform.position, interactSound);

        bool success = PlayerInventory.active.TryEquipping(this);
        if (success)
        {
            falling = false;
            DisablePhysics();
            MovingPlatformManager.active.RemoveEntry(transform);
        }
        return success;
    }
    void Update()
    {
        UpdateItem();
        if (equipedGun == null)
            return;

        if (CheckForTargets(out Health closestHealth))
        {
            bool canShoot = true;
            Vector3 targetPos = closestHealth.GetOrigin();
            if (equipedGun.bulletPrefab == null)
            {
                //HitScan
                helperTransform.position = rotXTransform.position;
                helperTransform.LookAt(targetPos);

                targetRot = new Vector2(helperTransform.eulerAngles.x, helperTransform.eulerAngles.y);
            }
            else if (equipedGun.bulletPrefab.TryGetComponent(out Grenade grenade))
            {
                helperTransform.position = rotXTransform.position;
                helperTransform.LookAt(targetPos);
                
                canShoot = Util.SolveTrajectory(grenade.velocity, rotXTransform.position, targetPos, angleMode, out float angle);;
                targetRot = new Vector2(angle, helperTransform.eulerAngles.y);
            }

            if (canShoot)
            {
                helperTransform.rotation = Quaternion.Euler(targetRot.x, targetRot.y, 0f);
                if (equipedGun.HasAmmoInClip())
                {
                    if (Vector3.Angle(rotXTransform.forward, helperTransform.forward) <= maxAngleDiff)
                        equipedGun.Shoot(false, equipedGunVisual.bulletSpawn, ShooterType.Sentry);
                }
                else
                {
                    int count = Mathf.Clamp(ammoCount, 0, equipedGun.clipSize);
                    if (count > 0)
                    {
                        equipedGun.Reload(count, equipedGunVisual.bulletSpawn);
                        ammoCount -= count;
                    }
                    else
                    {
                        //Produce SFX
                        equipedGun.Shoot(false, equipedGunVisual.bulletSpawn, ShooterType.Sentry);
                    }
                }
                UpdateAmmoLights();
            }
            else
            {
                targetRot = new Vector2(0f, transform.eulerAngles.y + Mathf.Floor(Time.time % 2f) * 20f - 10f);
            }
        }
        else
        {
            targetRot = new Vector2(0f, transform.eulerAngles.y + Mathf.Floor(Time.time % 2f) * 20f - 10f);
        }
        rotYTransform.rotation = Quaternion.RotateTowards(rotYTransform.rotation, Quaternion.Euler(0f, targetRot.y, 0f), rotYSpeed * Time.deltaTime);
        rotXTransform.localRotation = Quaternion.RotateTowards(rotXTransform.localRotation, Quaternion.Euler(targetRot.x, 0f, 0f), rotXSpeed * Time.deltaTime);
    }
    public void UpdateAmmoLights()
    {
        ammoFillInteractable.SetObjectNameOverride("Magazine [" + ammoCount + "/" + maxAmmoCount + "]");

        float time = (float)ammoCount / (float)maxAmmoCount;
        int lightColorIndex = Mathf.RoundToInt(time * lightColorList.Count);
        lightColorIndex = Mathf.Clamp(lightColorIndex, 0, lightColorList.Count - 1);
        int lightIndex = Mathf.RoundToInt(time * ammoLightList.Count);
        Debug.Log($"{time} : {lightColorIndex} : {lightIndex}");

        if (equipedGun == null)
            lightIndex = -1;

        for (int i = 0; i < ammoLightList.Count; i++)
        {
            if(i <= lightIndex)
            {
                ammoLightList[i].enabled = true;
                ammoLightList[i].color = lightColorList[lightColorIndex];
            }
            else
            {
                ammoLightList[i].enabled = false;
            }
        }
    }

    public bool CheckForTargets(out Health closestHealth)
    {
        closestHealth = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < healthTypeFilterList.Count; i++)
        {
            foreach (Health health in EnemyManager.active.healthTypeArray[(int)healthTypeFilterList[i]])
            {
                if (health.health <= 0f)
                    continue;

                float distance = Vector3.Distance(transform.position, health.transform.position);

                if (distance <= maxDistance && distance < closestDistance)
                {
                    closestHealth = health;
                    closestDistance = distance;
                }
            }
        }

        if (closestHealth)
            return true;
        return false;
    }
    private void OnDestroy()
    {
        if(helperTransform)
            Destroy(helperTransform.gameObject);
    }
}
