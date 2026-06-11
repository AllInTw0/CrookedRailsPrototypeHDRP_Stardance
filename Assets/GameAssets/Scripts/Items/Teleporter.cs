using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : Item
{
    public static List<Teleporter> activeTeleporterList = new List<Teleporter>();
    
    [Header("Teleporter")]
    [SerializeField] 
    private EventInteractable teleportInteractable;
    [SerializeField] 
    private string pairString;
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
    [SerializeField] private float teleportCooldown;
    
    private float cooldown;
    private Teleporter pairedTeleporter;

    public override void Awake()
    {
        base.Awake();
        
        teleportParticles.Stop();
        lightFlickerer.TurnOff();
    }
    public override void Start()
    {
        base.Start();
        
        activeTeleporterList.Add(this);
        
        onDroppedOnGround.AddListener(() =>
        {
            foreach (Teleporter teleporter in activeTeleporterList)
            {
                if (teleporter != this && teleporter.IsPaired() == false)
                    teleporter.Pair(this);
            }
        });

        teleportInteractable.interactEvent.AddListener(() =>
        {
            if (IsPaired())
            {
                float maxCooldown = MaxCooldown();
                if (maxCooldown <= 0f)
                {
                    if (pairedTeleporter.IsInGameplaySpace())
                    {
                        TeleportSequence();
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
        });
        teleportInteractable.enabled = false;
        
        onPickedUp.AddListener(() =>
        {
            if(IsPaired())
                pairedTeleporter.Unpair(this);
        });
        onPhysicsEnabled.AddListener(() =>
        {
            if(IsPaired())
                pairedTeleporter.Unpair(this);
        });
        DropFromPos(transform.position);
    }

    private void Update()
    {
        Quaternion rot = topRot.localRotation;
        float tagetRotY = -180f;
        if (IsPaired())
        {
            topRot.LookAt(pairedTeleporter.transform.position);
            tagetRotY = topRot.localEulerAngles.y;
        }

        topRot.localRotation = Quaternion.Lerp(rot, Quaternion.Euler(0f, tagetRotY + 180f, 0f), Time.deltaTime * 2f);
        
        
        //Cooldown
        cooldown -= Time.deltaTime;

        float maxCooldown = MaxCooldown();
        if (maxCooldown > 0f)
        {
            teleportInteractable.SetActionNameOverride("Cooldown - " + Mathf.Round(maxCooldown) + "s", 0.1f);
        }
        else
        {
            bool turnOnLight = IsPaired() && CanTeleport() && pairedTeleporter.CanTeleport();
            
            if(lightFlickerer.IsOn() == false && turnOnLight)
                lightFlickerer.TurnOn();
            else if(lightFlickerer.IsOn() && turnOnLight == false)
                lightFlickerer.TurnOff();
        }
        
        UpdateItem();
    }

    public bool IsPaired()
    {
        return pairedTeleporter != null;
    }

    public float GetCooldown()
    {
        return cooldown;
    }

    private float MaxCooldown()
    {
        if(IsPaired())
            return Mathf.Max(cooldown, pairedTeleporter.GetCooldown());
        return cooldown;
    }
    public void Pair(Teleporter teleporter)
    {
        if(IsPaired()) return;
        

        pairedTeleporter = teleporter;
        animator.SetBool("paired", true);
        teleporter.Pair(this);
        lightFlickerer.TurnOn();
        teleportInteractable.enabled = true;
        
        Debug.Log("Teleporter Paired");
        SoundManager.active.PlayAtPos(transform.position, pairString);
    }

    public void Unpair(Teleporter teleporter)
    {
        if(IsPaired() == false) return;
        
        pairedTeleporter = null;
        animator.SetBool("paired", false);
        teleporter.Unpair(this);
        lightFlickerer.TurnOff();
        teleportInteractable.enabled = false;
    }

    public void TeleportSequence()
    {
        StartCoroutine(TeleportEnumerator());
    }

    public IEnumerator TeleportEnumerator()
    {
        cooldown = teleportCooldown + 2.6f;
        animator.SetTrigger("teleport");
        pairedTeleporter.Receive();
        
        yield return new WaitForSeconds(0.1f);
        lightFlickerer.TurnOff();
        yield return new WaitForSeconds(0.5f);
        teleportParticles.Play();
        lightFlickerer.TurnOn();
        yield return new WaitForSeconds(0.5f);
        lightFlickerer.TurnOff();
        yield return new WaitForSeconds(0.5f);
        lightFlickerer.TurnOn();
        yield return new WaitForSeconds(0.5f);
        lightFlickerer.TurnOff();
        yield return new WaitForSeconds(0.35f);
        teleportParticles.Stop();
        yield return new WaitForSeconds(0.15f);
        lightFlickerer.TurnOn();
        
        Teleport();
        
        yield return new WaitForSeconds(1f);
        lightFlickerer.TurnOff();
    }
    private void Teleport()
    {
        Collider[] colliderArray = Util.PhysicsBoxColliderOverlap(teleportCollider,teleportLayerMask);
        Debug.Log("Deteccted: " + colliderArray.Length);
        float angleDif = pairedTeleporter.transform.eulerAngles.y - transform.eulerAngles.y;
        foreach (Collider collider in colliderArray)
        {
            Debug.Log(collider.gameObject.name);
            if (collider.TryGetComponent(out Rigidbody rb) && collider.TryGetComponent(out Teleporter teleporter) == false)
            {
                Vector3 localPos = transform.InverseTransformPoint(rb.position);
                rb.position = pairedTeleporter.transform.TransformPoint(localPos);
                rb.rotation = rb.rotation * Quaternion.Euler(0f, angleDif, 0f);
            }
        }
        //PlayerMovement.active.Teleport(pairedTeleporter.transform.position);
    }
    public void Receive()
    {
        animator.SetTrigger("receive");
        cooldown = teleportCooldown + 2.6f;
        StartCoroutine(ReceiveEnumerator());
    }

    public bool CanTeleport()
    {
        return IsInGameplaySpace() && cooldown <= 0f;
    }
    public bool IsInGameplaySpace()
    {
        return PlayerMovement.active.IsPositionInGameplaySpace(transform.position);
    }
    public IEnumerator ReceiveEnumerator()
    {
        yield return new WaitForSeconds(0.5f);
        lightFlickerer.TurnOff();
        yield return new WaitForSeconds(0.5f);
        lightFlickerer.TurnOn();
        yield return new WaitForSeconds(0.5f);
        lightFlickerer.TurnOff();
        yield return new WaitForSeconds(0.5f);
        lightFlickerer.TurnOn();
        
        
        yield return new WaitForSeconds(3f);
        lightFlickerer.TurnOff();
    }
    private void OnDestroy()
    {
        activeTeleporterList.Remove(this);
    }
}
