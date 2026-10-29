using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.HighDefinition.CameraSettings;

public class Sentry : Item
{
    
    private NetworkVariable<int> ammoCount = new(writePerm:NetworkVariableWritePermission.Server);
    private void OnAmmoCountChanged(int previous, int current)
    {
        if(current > previous)
            SoundManager.active.PlayAtPos(ammoFillInteractable.transform.position, ammoRefillString);
        UpdateAmmoLights();
    }

    private NetworkVariable<NetworkBehaviourReference> equippedGun = new(writePerm: NetworkVariableWritePermission.Server);
    private void OnEquippedGunChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        if (current.TryGet(out ItemGun itemGun))
        {
            if(itemGun.playerWhoHasItemClient)
                itemGun.playerWhoHasItemClient.inventory.UnEquipTool();
            
            itemGun.BecomeInvisible();
            foreach (GunVisual visual in gunVisualList)
            {
                if (visual.itemInfo == itemGun.itemInfo)
                {
                    visual.visualParent.SetActive(true);
                    gunPistonFollow.target = visual.pistonTarget.transform;

                    equipedGunVisual = visual;
                }
            }
            itemInteractable.SetObjectNameOverride(itemGun.itemInfo.name);
            itemInteractable.SetActionNameOverride("Pick Up");

            ammoFillInteractable.gameObject.SetActive(true);
            interactableCollider.enabled = false;
        }

        if (previous.TryGet(out ItemGun itemGun2))
        {
            //UnEquip Tool
            foreach (GunVisual visual in gunVisualList)
            {
                visual.visualParent.SetActive(false);
            }
            
            //DropAmmo();

            itemInteractable.ClearOverrides();
            
            equipedGunVisual = null;
            ammoFillInteractable.gameObject.SetActive(false);
            interactableCollider.enabled = true;
            
            itemGun2.BecomeVisible();
            itemGun2.DropFromPos(transform.position + Vector3.up);
        }
    }

    private ItemGun GetEquippedGun()
    {
        if (equippedGun.Value.TryGet(out ItemGun itemGun))
            return itemGun;
        return null;
    }
    private NetworkVariable<NetworkBehaviourReference> target = new(writePerm: NetworkVariableWritePermission.Server);
    private Health GetTarget()
    {
        if (target.Value.TryGet(out Health health))
            return health;
        return null;
    }
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
    private LayerMask raycastLayer;
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
    private GunVisual equipedGunVisual;

    private Vector2 targetRot;
    private Transform helperTransform;
    public override void Start()
    {
        base.Start();
        
        helperTransform = new GameObject().transform;

        ammoCount.OnValueChanged += OnAmmoCountChanged;
        equippedGun.OnValueChanged += OnEquippedGunChanged;
        
        itemInteractable.interactEventServer.AddListener((ulong clientID) =>
        {
            Player senderPlayer = Player.GetPlayerByClientID(clientID);
            if (GetEquippedGun() == null)
            {
                if (senderPlayer.inventory.tool && senderPlayer.inventory.tool is ItemGun)
                {
                    for (int i = 0; i < gunVisualList.Count; i++)
                    {
                        if (gunVisualList[i].itemInfo == senderPlayer.inventory.tool.itemInfo)
                        {
                            equippedGun.Value = (ItemGun)senderPlayer.inventory.tool;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (GetEquippedGun().TryEquipping(senderPlayer))
                {
                    //UnEquip Tool
                    DropAmmo();
                    equippedGun.Value = null;
                }
            }
        });

        ammoFillInteractable.interactEventServer.AddListener((ulong clientID) =>
        {
            Player senderPlayer = Player.GetPlayerByClientID(clientID);
            int targetCount = Mathf.Clamp(maxAmmoCount - ammoCount.Value, 0, GetEquippedGun().ammoItem.maxCount / 2);
            if (targetCount > 0 && senderPlayer.inventory.RemoveItem(GetEquippedGun().ammoItem, out int itemCountRemoved, targetCount))
            {
                ammoCount.Value += itemCountRemoved;
            }
        });
        
        sentryHealth.onTakeDamage.AddListener((float dammage, Vector3 force) =>
        {
            if (sentryHealth.GetHealth() == 0)
            {
                //Destroy Sentry

                if (GetEquippedGun() && IsServer)
                {
                    DropAmmo();
                    equippedGun.Value = null;
                }
                
                SoundManager.active.PlayAtPos(transform.position, sentryBreakString);
                
                if (IsServer)
                {
                    Item.SpawnItem(destroyedSentryItem, transform.position + Vector3.up * 0.2f, transform.rotation);
                    DestroyItem();
                }
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
        //Server only
        
        if (GetEquippedGun() == null)
            return;
        //Drop ammo
        while (ammoCount.Value > 0)
        {
            int count = Mathf.Clamp(GetEquippedGun().ammoItem.maxCount, 0, ammoCount.Value);
            Item.SpawnItem(GetEquippedGun().ammoItem, transform.position + Random.onUnitSphere + Vector3.up, Quaternion.Euler(0f, 0f, 0f), count);
            ammoCount.Value -= count;
        }
    }
    void Update()
    {
        UpdateItem();
        if (GetEquippedGun() == null)
            return;

        if (IsServer)
        {
            if (CheckForTargets(out Health closestHealth))
            {
                target.Value = closestHealth;
            }
            else
                target.Value = null;
        }
        
        if (GetTarget())
        {
            bool canShoot = true;
            Vector3 targetPos = GetTarget().GetOrigin();
            if (GetEquippedGun().bulletPrefab == null)
            {
                //HitScan
                helperTransform.position = rotXTransform.position;
                helperTransform.LookAt(targetPos);

                targetRot = new Vector2(helperTransform.eulerAngles.x, helperTransform.eulerAngles.y);
            }
            else if (GetEquippedGun().bulletPrefab.TryGetComponent(out Grenade grenade))
            {
                helperTransform.position = rotXTransform.position;
                helperTransform.LookAt(targetPos);
                
                canShoot = Util.SolveTrajectory(grenade.velocity, rotXTransform.position, targetPos, angleMode, out float angle);;
                targetRot = new Vector2(angle, helperTransform.eulerAngles.y);
            }
            
            
            if (canShoot)
            {
                if (GetEquippedGun().IsOnCooldown() == false && IsServer)
                {
                    helperTransform.rotation = Quaternion.Euler(targetRot.x, targetRot.y, 0f);
                    if (GetEquippedGun().HasAmmoInClip())
                    {
                        if (Vector3.Angle(rotXTransform.forward, helperTransform.forward) <= maxAngleDiff)
                            ShootRpc();
                    }
                    else
                    {
                        int count = Mathf.Clamp(ammoCount.Value, 0, GetEquippedGun().clipSize);
                        ReloadRpc(count);
                        ammoCount.Value -= count;
                    }
                }
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
    [Rpc(SendTo.Everyone)]
    private void ShootRpc()
    {
        bool doDamage = IsServer;
        GetEquippedGun().Shoot(equipedGunVisual.bulletSpawn.position, equipedGunVisual.bulletSpawn.forward, doDamage, ShooterType.Sentry);
    }
    [Rpc(SendTo.Everyone)]
    private void ReloadRpc(int count)
    {
        GetEquippedGun().ReloadClient(count, equipedGunVisual.bulletSpawn.position);
    }
    public void UpdateAmmoLights()
    {
        ammoFillInteractable.SetObjectNameOverride("Magazine [" + ammoCount.Value + "/" + maxAmmoCount + "]");

        float time = (float)ammoCount.Value / (float)maxAmmoCount;
        int lightColorIndex = Mathf.RoundToInt(time * lightColorList.Count);
        lightColorIndex = Mathf.Clamp(lightColorIndex, 0, lightColorList.Count - 1);
        int lightIndex = Mathf.RoundToInt(time * ammoLightList.Count);
        Debug.Log($"{time} : {lightColorIndex} : {lightIndex}");

        if (GetEquippedGun() == null)
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
                if (health.GetHealth() <= 0f)
                    continue;

                float distance = Vector3.Distance(transform.position, health.transform.position);

                if (distance <= maxDistance && distance < closestDistance)
                {
                    if (Physics.Raycast(rotXTransform.position, health.GetOrigin() - rotXTransform.position, out RaycastHit hit, maxDistance,raycastLayer))
                    {
                        if (hit.transform == health.transform)
                        {
                            closestHealth = health;
                            closestDistance = distance;
                        }
                        else
                        {
                            Debug.Log(hit.transform);
                        }
                    }
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
        
        OnEquippedGunChanged(equippedGun.Value, null);
    }
}
