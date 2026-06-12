using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class Item : NetworkBehaviour
{
    [NonSerialized] 
    public UnityEvent onDroppedOnGround;
    [NonSerialized] 
    public UnityEvent onPickedUp;
    [NonSerialized] 
    public UnityEvent onPhysicsEnabled;
    //Variables
    [Header("Item")]
    public ItemSO itemInfo;

    //[SerializeField] 
    private MeshRenderer[] meshRenderers;
    //[SerializeField] 
    [NonSerialized]
    public Collider interactableCollider;

    public int count = 1;

    //Run time
    private EventInteractable eventInteractable;
    
    [HideInInspector]
    public bool falling;
    private float targetY;
    private float distance;
    private float velocity;
    private float rotVelocity;

    private Rigidbody rb;
    private NetworkTransform networkTransform;
    private NetworkRigidbody networkRigidbody;
    private bool physicsEnabled;
    private bool dropSilently;

    [NonSerialized] 
    public Player playerWhoHasItem = null;
    public override void OnNetworkSpawn()
    {
        //ItemManager.AddItem(this);
    }

    public virtual void Awake()
    {
        onDroppedOnGround = new UnityEvent();
        onPickedUp = new UnityEvent();
        onPhysicsEnabled = new UnityEvent();

        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        interactableCollider = GetComponent<BoxCollider>();
        

        rb = gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        
        networkRigidbody = GetComponent<NetworkRigidbody>();
        networkRigidbody.enabled = false;
        networkTransform = GetComponent<NetworkTransform>();
        networkTransform.enabled = false;
        
    }
    
    public virtual void Start()
    {
        DropFromPos(transform.position);
        
        eventInteractable = GetComponent<EventInteractable>();
        eventInteractable.interactEvent.AddListener(() =>
        {
            if (TryEquipping())
            {
                Debug.Log("Item Equipped");
            }
            else
            {
                eventInteractable.InteractionFailed();
                Debug.Log("Cant Equip Item");
            }
        });
    }
    
    public virtual bool TryEquipping(Player targetPlayer = null, bool sendRpc = true)
    {
        if (targetPlayer == null)
            targetPlayer = Player.localPlayer;
        
        if (targetPlayer.inventory.TryEquipping(this))
        {
            falling = false;
            DisablePhysics();
            onPickedUp.Invoke();
            MovingPlatformManager.active.RemoveEntry(transform);
            if (sendRpc && targetPlayer == Player.localPlayer)
            {
                EquipItemServerRpc();
            }
            return true;
        }
        return false;
    }
    //Equipping Multiplayer
    [Rpc(SendTo.Server)]
    private void EquipItemServerRpc(RpcParams receiveParams = default)
    {
        Player senderPlayer = Player.GetPlayerByClientID(receiveParams.Receive.SenderClientId);
        if (playerWhoHasItem == senderPlayer || TryEquipping(senderPlayer, false))
        {
            EquipItemClientRpc(receiveParams.Receive.SenderClientId);
            Debug.Log("[Item Server] Server Agrees On Equip");
        }
        else
        {
            Debug.Log("[Item Server] Server Disagrees On Equip");
        }
    }
    [Rpc(SendTo.NotServer)]
    private void EquipItemClientRpc(ulong clientID)
    {
        Player targetPlayer = Player.GetPlayerByClientID(clientID);
        if (playerWhoHasItem == targetPlayer || TryEquipping(targetPlayer,false))
        {
            Debug.Log("[Item Client] Client Agrees On Equip");
        }
        else
        {
            Debug.Log("[Item Client] Client Disagrees On Equip");
        }
    }
    //Dropping Multiplayer
    [Rpc(SendTo.Server)]
    public void UnEquipItemServerRpc(RpcParams receiveParams = default)
    {
        Player senderPlayer = Player.GetPlayerByClientID(receiveParams.Receive.SenderClientId);
        if (playerWhoHasItem == null || playerWhoHasItem == senderPlayer)
        {
            UnEquipItemClientRpc(receiveParams.Receive.SenderClientId);
            if (playerWhoHasItem == senderPlayer)
            {
                if (playerWhoHasItem.inventory.tool == this)
                    playerWhoHasItem.inventory.UnEquipTool();
                else
                    playerWhoHasItem.inventory.UnEquipItem(this);
            }
            Debug.Log("[Item Server] Server Agrees On UnEquip");
        }
        else
        {
            Debug.Log("[Item Server] Server Disagrees On UnEquip");
        }
    }
    [Rpc(SendTo.NotServer)]
    private void UnEquipItemClientRpc(ulong clientID)
    {
        Player targetPlayer = Player.GetPlayerByClientID(clientID);
        if (playerWhoHasItem == null || playerWhoHasItem == targetPlayer)
        {
            if (playerWhoHasItem == targetPlayer)
            {
                if (playerWhoHasItem.inventory.tool == this)
                    playerWhoHasItem.inventory.UnEquipTool();
                else
                    playerWhoHasItem.inventory.UnEquipItem(this);
            }
            Debug.Log("[Item Client] Client Agrees On UnEquip");
        }
        else
        {
            Debug.Log("[Item Client] Client Disagrees On UnEquip");
        }
    }
    private void Update()
    {
        UpdateItem();
        if (itemInfo.maxCount > 1)
            eventInteractable.SetObjectNameOverride(eventInteractable.objectName + " [" + count + "]");
    }

    public void UpdateItem()
    {
        if (physicsEnabled)
        {
            falling = false;
            if(IsServer)
            {
                if (rb.linearVelocity.magnitude <= 0.1f)
                {
                    DropFromPosRpc(transform.position);
                }
                else if (transform.position.y < -10f) // Safety net if item falls out of world
                {
                    if (Physics.Raycast(new Vector3(transform.position.x, 100f, transform.position.z), Vector3.down,
                            out RaycastHit hit, 115f, ItemManager.itemDropLayerMask))
                    {
                        Debug.LogWarning("Item fell out of world. Teleporting to surface");
                        DropFromPosRpc(hit.point + Vector3.up * 0.25f);
                    }
                    else
                    {
                        Debug.LogWarning("Cant get item back. Teleporting to player");
                        DropFromPosRpc(Player.localPlayer.transform.position + Vector3.up * 0.25f);
                    }
                }
            }
        }
        else
        {
            rb.isKinematic = true;
        }
        if (falling)
        {
            velocity += 10f * Time.deltaTime;
            transform.position -= new Vector3(0, velocity * Time.deltaTime, 0f);
            transform.Rotate(0f, rotVelocity * Time.deltaTime, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, 0), (distance - (transform.position.y - targetY)) / distance);
            if (transform.position.y < targetY)
            {
                falling = false;
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

                if (dropSilently == false)
                    SoundManager.active.PlayAtPos(transform.position, "Item - Drop");
                else
                    dropSilently = false;
                
                onDroppedOnGround.Invoke();
            }
        }
    }
    [Rpc(SendTo.Everyone)]
    private void DropFromPosRpc(Vector3 startPos)
    {
        DropFromPos(startPos);
    }
    public void DropFromPos(Vector3 startPos, bool hasSound = true)
    {
        transform.position = startPos;
        falling = true;
        velocity = 0;
        rotVelocity = Random.Range(-35f, 35f);
        if (hasSound == false) dropSilently = true;
        if (Physics.Raycast(startPos + new Vector3(0f,0.05f,0f), Vector3.down, out RaycastHit hit, 100f, ItemManager.itemDropLayerMask))
        {
            targetY = hit.point.y;
            distance = transform.position.y - targetY;

            if (hit.transform.CompareTag("Moving"))
            {
                MovingPlatformManager.active.AddEntry(transform,transform,hit.transform);
            }

            DisablePhysics();
        }
        else
        {
            falling = false;
            Debug.Log("No Ground Found");
        }
    }
    public void BecomeInvisible()
    {
        transform.position = new Vector3(0f, -50f, 0f); //Look below
        foreach (var mesh in meshRenderers)
        {
            mesh.enabled = false;
        }
        //This is done so triggers can detect if an item has exited it when picked up by the player
        //interactableCollider.enabled = false;
        falling = false;
    }
    public void BecomeVisible()
    {
        foreach (var mesh in meshRenderers)
        {
            mesh.enabled = true;
        }
        interactableCollider.enabled = true;
    }
    //Physics
    [Rpc(SendTo.Everyone)]
    public void EnablePhysicsRpc(Vector3 velocity)
    {
        EnablePhysics(velocity);
    }
    private void EnablePhysics(Vector3 velocity)
    {
        if (interactableCollider.enabled == false) return;
        
        MovingPlatformManager.active.RemoveEntry(transform);
        rb.isKinematic = false;
        falling = false;
        physicsEnabled = true;
        rb.linearVelocity = velocity;
        rb.angularVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));

        networkRigidbody.enabled = true;
        networkTransform.enabled = true;
        
        onPhysicsEnabled.Invoke();
    }
    public void AddExplosionForce(Vector3 explosionPos, float force, float range)
    {
        AddExplosionForceServerRpc(explosionPos, (short)force, (short)range);
    }

    [Rpc(SendTo.Server)]
    private void AddExplosionForceServerRpc(Vector3 explosionPos, short force, short range)
    {
        if(physicsEnabled == false)
        {
            EnablePhysicsRpc(Vector3.zero);
            rb.AddExplosionForce(force, explosionPos, range);
        }
    }
    [Rpc(SendTo.Everyone)]
    private void DisablePhysicsRpc()
    {
        DisablePhysics();
    }
    private void DisablePhysics()
    {
        rb.isKinematic = true;
        physicsEnabled = false;
        
        networkRigidbody.enabled = false;
        networkTransform.enabled = false;
    }
    public bool IsPhysicsEnabled()
    {
        return physicsEnabled;
    }
    public static Item SpawnItem(ItemSO itemSO, Vector3 pos, Quaternion rot, int stackCount = 1, bool dropSound = true)
    {
        
        Transform copy = Instantiate(itemSO.prefab,pos,rot).transform;

        Item item = copy.GetComponent<Item>();
        item.DropFromPos(pos, dropSound);
        item.count = Mathf.Clamp(stackCount, 1, item.itemInfo.maxCount);

        return item;
    }

    public override void OnDestroy()
    {
        //ItemManager.RemoveItem(this);
    }
}
