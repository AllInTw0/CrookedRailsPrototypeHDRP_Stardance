using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Teleporter : Item
{
    public static List<Teleporter> activeTeleporterList = new List<Teleporter>();

    public NetworkVariable<TeleporterNetworkData> networkData = new(writePerm: NetworkVariableWritePermission.Server);
    public struct TeleporterNetworkData : INetworkSerializable
    {
        public NetworkBehaviourReference pairedTeleporter;
        public float teleportTime;
        public float cooldownTime;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref pairedTeleporter);
            serializer.SerializeValue(ref teleportTime);
            serializer.SerializeValue(ref cooldownTime);
        }
    }

    private void StateChanged(TeleporterNetworkData previous, TeleporterNetworkData current)
    {
        previous.pairedTeleporter.TryGet(out Teleporter previousTeleporter);
        current.pairedTeleporter.TryGet(out Teleporter currentTeleporter);
        
        if(previousTeleporter == null && currentTeleporter != null)
            SoundManager.active.PlayAtPos(transform.position, pairString);

        RefreshTeleported();
        if(currentTeleporter)
            currentTeleporter.RefreshTeleported();

        if (IsPaired() && IsTeleporting(out bool reciever, out float timeTillTeleport) && reciever == false)
            SoundManager.active.PlayAtPos(transform.position, teleportingString);
    }

    public void RefreshTeleported()
    {
        teleported = false;
    }
    public Teleporter GetPairedTeleporter()
    {
        if(networkData.Value.pairedTeleporter.TryGet(out Teleporter teleporter))
            return teleporter;
        return null;
    }
    
    [Header("Teleporter")]
    [SerializeField] 
    private EventInteractable teleportInteractable;
    [SerializeField] 
    private string pairString;
    [SerializeField] 
    private string teleportingString = "Teleporter - Teleporting";
    [SerializeField] 
    private string teleportString = "Teleporter - Teleport";
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Flickerer lightFlickerer;
    [SerializeField]
    private ParticleSystem teleportParticles;
    [SerializeField]
    private BoxCollider teleportCollider;
    [SerializeField]
    private LayerMask teleportLayerMask;
    [SerializeField]
    private Transform topRot; //Satellite dish

    [Header("Balance")] 
    [SerializeField] private float teleportTime;
    [SerializeField] private float teleportCooldown;

    private bool teleported;
    private bool animationPlayed;
    private float flickerTime;
    public override void Awake()
    {
        base.Awake();
        
        teleportParticles.Stop();
        lightFlickerer.TurnOff();
    }

    public override void OnNetworkSpawn()
    {
        networkData.OnValueChanged += StateChanged;
    }

    public override void Start()
    {
        base.Start();
        
        activeTeleporterList.Add(this);
        
        onDroppedOnGround.AddListener(() =>
        {
            if (IsServer)
            {
                foreach (Teleporter teleporter in activeTeleporterList)
                {
                    if (teleporter != this && teleporter.IsPaired() == false)
                        teleporter.Pair(this);
                }
            }
        });

        void Interact()
        {
            Debug.Log("Interact");
            if (IsPaired())
            {
                float maxCooldown = MaxCooldown();
                if (maxCooldown <= 0f)
                {
                    if (GetPairedTeleporter().IsInGameplaySpace())
                    {
                        if (IsServer)
                        {
                            networkData.Value = new()
                            {
                                pairedTeleporter = GetPairedTeleporter(),
                                teleportTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + teleportTime,
                                cooldownTime = NetworkManager.Singleton.ServerTime.TimeAsFloat + teleportTime + teleportCooldown,
                            };
                        }
                    }
                    else
                    {
                        teleportInteractable.InteractionFailed();
                        teleportInteractable.SetActionNameOverride("Target Too Far From Train", 1.5f);
                    }
                }
                else
                {
                    teleportInteractable.InteractionFailed();
                    //teleportInteractable.SetActionNameOverride("Cooldown - " + maxCooldown + "s", 1.5f);
                }
            }
            else
            {
                teleportInteractable.InteractionFailed();
                teleportInteractable.SetActionNameOverride("Not Paired", 1.5f);
            }
        }
        teleportInteractable.interactEventLocal.AddListener(() =>
        {
            if(IsServer == false)
                Interact();
        });
        teleportInteractable.interactEventServer.AddListener((ulong clientID) =>
        {
            Interact();
        });
        
        
        teleportInteractable.enabled = false;
        
        onPickedUp.AddListener(() =>
        {
            if(IsPaired() && IsServer)
                Unpair(GetPairedTeleporter());
        });
        onPhysicsEnabled.AddListener(() =>
        {
            if(IsPaired() && IsServer)
                Unpair(GetPairedTeleporter());
        });
        DropFromPos(transform.position);
    }

    private void Update()
    {
        Quaternion rot = topRot.localRotation;
        float tagetRotY = -180f;
        if (IsPaired())
        {
            topRot.LookAt(GetPairedTeleporter().transform.position);
            tagetRotY = topRot.localEulerAngles.y;
            
            animator.SetBool("paired", true);
            
            if (IsTeleporting(out bool reciever, out float timeTillTeleport))
            {
                flickerTime += Time.deltaTime;
                if (flickerTime > 0.5f)
                {
                    if (lightFlickerer.IsOn())
                        lightFlickerer.TurnOff();
                    else 
                        lightFlickerer.TurnOn();
                    flickerTime = 0f;
                }
                
                
                teleportInteractable.enabled = false;
                eventInteractable.enabled = false;
                if (reciever == false)
                {
                    animator.SetBool("teleporting", true);
                    
                    if(timeTillTeleport <= teleportTime - 1f)
                        teleportParticles.Play();
                    if(timeTillTeleport <= 0.25f)
                        teleportParticles.Stop();
                }
                else
                {
                    animator.SetBool("recieving", true);
                }
                
                
                
                if (timeTillTeleport <= 0.25f)
                {
                    animator.SetTrigger("teleport");
                }

                if (timeTillTeleport <= 0f)
                {
                    SoundManager.active.PlayAtPos(transform.position, teleportString);
                    teleported = true;
                    if(reciever == false)
                        Teleport();
                    
                    animator.SetBool("teleporting", false);
                    animator.SetBool("recieving", false);
                    lightFlickerer.TurnOff();
                }
            }
            else
            {
                teleportInteractable.enabled = true;
                eventInteractable.enabled = true;
            }
        }
        else
        {
            animator.SetBool("paired", false);
            teleportInteractable.enabled = false;
            eventInteractable.enabled = true;
        }
        topRot.localRotation = Quaternion.Lerp(rot, Quaternion.Euler(0f, tagetRotY + 180f, 0f), Time.deltaTime * 2f);

        
        float maxCooldown = MaxCooldown();
        if (maxCooldown > 0f)
        {
            teleportInteractable.SetActionNameOverride("Cooldown - " + Mathf.Round(maxCooldown) + "s", 0.1f);
        }
        else
        {
            bool turnOnLight = IsPaired() && CanTeleport() && GetPairedTeleporter().CanTeleport();
            
            if(lightFlickerer.IsOn() == false && turnOnLight)
                lightFlickerer.TurnOn();
            else if(lightFlickerer.IsOn() && turnOnLight == false)
                lightFlickerer.TurnOff();
        }
        
        UpdateItem();
    }

    public bool IsTeleporting(out bool reciever, out float timeTillTeleport)
    {
        reciever = false;
        timeTillTeleport = -1;
        if (IsPaired() == false || teleported)
            return false;
        
        if (GetTimeTillTeleport() > -1f)
        {
            reciever = false;
            timeTillTeleport = GetTimeTillTeleport();
            return true;
        }

        if (GetPairedTeleporter().GetTimeTillTeleport() > -1f)
        {
            reciever = true;
            timeTillTeleport = GetPairedTeleporter().GetTimeTillTeleport();
            return true;
        }
        
        return false;
    }
    
    public bool IsPaired()
    {
        return GetPairedTeleporter() != null;
    }

    public float GetCooldown()
    {
        return networkData.Value.cooldownTime - NetworkManager.ServerTime.TimeAsFloat;
    }

    public float GetTimeTillTeleport()
    {
        return networkData.Value.teleportTime - NetworkManager.ServerTime.TimeAsFloat;
    }
    private float MaxCooldown()
    {
        if(IsPaired())
            return Mathf.Max(GetCooldown(), GetPairedTeleporter().GetCooldown());
        return GetCooldown();
    }
    
    public void Pair(Teleporter teleporter)
    {
        if(IsPaired()) return;

        networkData.Value = new TeleporterNetworkData()
        {
            pairedTeleporter = teleporter
        };
        teleporter.Pair(this);
       
        Debug.Log("Teleporter Paired");
    }

    public void Unpair(Teleporter teleporter)
    {
        if(IsPaired() == false) return;

        networkData.Value = new TeleporterNetworkData()
        {
            pairedTeleporter = null
        };
        teleporter.Unpair(this);
    }
    private void Teleport()
    {
        Collider[] colliderArray = Util.PhysicsBoxColliderOverlap(teleportCollider,teleportLayerMask);
        Debug.Log("Deteccted: " + colliderArray.Length);
        float angleDif = GetPairedTeleporter().transform.eulerAngles.y - transform.eulerAngles.y;
        foreach (Collider collider in colliderArray)
        {
            Debug.Log(collider.gameObject.name);
            if (collider.TryGetComponent(out Rigidbody rb) && collider.TryGetComponent(out Teleporter teleporter) == false)
            {
                Vector3 localPos = transform.InverseTransformPoint(rb.position);
                rb.position = GetPairedTeleporter().transform.TransformPoint(localPos);
                rb.rotation = rb.rotation * Quaternion.Euler(0f, angleDif, 0f);
            }
        }
        //PlayerMovement.active.Teleport(pairedTeleporter.transform.position);
    }

    public bool CanTeleport()
    {
        return IsInGameplaySpace() && GetCooldown() <= 0f;
    }
    public bool IsInGameplaySpace()
    {
        return Player.localPlayer.movement.IsPositionInGameplaySpace(transform.position);
    }
    private void OnDestroy()
    {
        activeTeleporterList.Remove(this);
    }
}
