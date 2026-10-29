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
    public bool isFakeItem;
    //[SerializeField] 
    private MeshRenderer[] meshRenderers;
    //[SerializeField] 
    [NonSerialized]
    public Collider interactableCollider;
    
    private NetworkVariable<int> count = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField]
    private int spawnCount = 1;
    public void SetSpawnCount(int value)
    {
        spawnCount = value;
    }

    public int GetCount()
    {
        return count.Value;
    }

    [Rpc(SendTo.Server)]
    public void SetCountServerRpc(int value)
    {
        count.Value = value;
    }
    //Run time
    [NonSerialized]
    public EventInteractable eventInteractable;
    
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

    public struct ItemOwnershipNetwork : INetworkSerializable
    {
        public byte playerID; //null
        public bool tool;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerID);
            serializer.SerializeValue(ref tool);
        }
    }
    
    private NetworkVariable<ItemOwnershipNetwork> ownershipNetworkVariable = new NetworkVariable<ItemOwnershipNetwork>(writePerm: NetworkVariableWritePermission.Server);
    public Player playerWhoHasItemServer
    {
        get
        {
            return Player.GetPlayerByID(ownershipNetworkVariable.Value.playerID);
        }
        set
        {
            if (IsServer)
            {
                if(value)
                    ownershipNetworkVariable.Value = new ItemOwnershipNetwork(){playerID = value.playerID,  tool = value.inventory.tool == this};
                else
                    ownershipNetworkVariable.Value = new ItemOwnershipNetwork(); //Equipped by none
            }
        }
    }
    [NonSerialized]
    public Player playerWhoHasItemClient;

    private void OnPlayerChanged(ItemOwnershipNetwork previous, ItemOwnershipNetwork current)
    {
        Debug.Log($"Recieved pickup data {ownershipNetworkVariable.Value.playerID} ,{ownershipNetworkVariable.Value.tool} ");
        if (IsServer == false)
        {
            if(playerWhoHasItemClient == playerWhoHasItemServer)
                return;
            
            if (playerWhoHasItemClient)
            {
                if (playerWhoHasItemClient.inventory.tool == this)
                    playerWhoHasItemClient.inventory.UnEquipTool();
                else
                    playerWhoHasItemClient.inventory.UnEquipItem(this);
            }

            if (playerWhoHasItemServer)
            {
                if (ownershipNetworkVariable.Value.tool)
                {
                    if (playerWhoHasItemServer.inventory.tool)
                        playerWhoHasItemServer.inventory.UnEquipTool();
                    playerWhoHasItemServer.inventory.TryEquipping(this);
                }
                else
                {
                    playerWhoHasItemServer.inventory.EquipAsItem(this);
                }
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        //ItemManager.AddItem(this);
        if(IsServer)
            SetCountServerRpc(spawnCount);

        ownershipNetworkVariable.OnValueChanged += OnPlayerChanged;
        count.OnValueChanged += CountChanged;
    }

    private void CountChanged(int previous, int current)
    {
        if (playerWhoHasItemClient == Player.localPlayer)
        {
            InventoryUI.active.UpdateItemIcons();
        }
    }

    public virtual void Awake()
    {
        onDroppedOnGround = new UnityEvent();
        onPickedUp = new UnityEvent();
        onPhysicsEnabled = new UnityEvent();

        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        interactableCollider = GetComponent<BoxCollider>();
        

        if(TryGetComponent(out rb))
            rb.isKinematic = true;
        
        if(TryGetComponent(out networkRigidbody))
            networkRigidbody.enabled = false;
        if(TryGetComponent(out networkTransform))
            networkTransform.enabled = false;
        
    }
    
    public virtual void Start()
    {
        DropFromPos(transform.position);
        
        if (TryGetComponent(out eventInteractable))
        {
            eventInteractable.interactEventLocal.AddListener(() =>
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
            if (sendRpc)
            {
                if(IsSpawned && isActiveAndEnabled) // When picking up a Stackable item it gets destroyed by the server and the server increases the count of another item
                    EquipItemServerRpc(targetPlayer.OwnerClientId);
            }
            return true;
        }
        return false;
    }
    //Equipping Multiplayer
    [Rpc(SendTo.Server)]
    private void EquipItemServerRpc(ulong clientID)
    {
        Player targetPlayer = Player.GetPlayerByClientID(clientID);
        if (playerWhoHasItemClient == targetPlayer || TryEquipping(targetPlayer, false))
        {
            NetworkObject.ChangeOwnership(clientID);
            playerWhoHasItemServer = playerWhoHasItemClient;
            Debug.Log("[Item Server] Server Agrees On Equip");
        }
        else
        {
            Debug.Log("[Item Server] Server Disagrees On Equip");
        }
    }
    //Dropping Multiplayer
    [Rpc(SendTo.Server)]
    public void UnEquipItemServerRpc(RpcParams receiveParams = default)
    {
        Player senderPlayer = Player.GetPlayerByClientID(receiveParams.Receive.SenderClientId);
        if (playerWhoHasItemClient == null || playerWhoHasItemClient == senderPlayer)
        {
            if (playerWhoHasItemClient == senderPlayer)
            {
                if (playerWhoHasItemClient.inventory.tool == this)
                    playerWhoHasItemClient.inventory.UnEquipTool();
                else
                    playerWhoHasItemClient.inventory.UnEquipItem(this);
            }
            
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            playerWhoHasItemServer = playerWhoHasItemClient;
            Debug.Log("[Item Server] Server Agrees On UnEquip");
        }
        else
        {
            Debug.Log("[Item Server] Server Disagrees On UnEquip");
        }
    }
    private void Update()
    {
        UpdateItem();
    }

    public void UpdateItem()
    {
        if (isFakeItem == false)
        {
            if (itemInfo.maxCount > 1)
                eventInteractable.SetObjectNameOverride(eventInteractable.objectName + " [" + GetCount() + "]");
            if (physicsEnabled)
            {
                falling = false;
                if (IsServer)
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
    private void DropFromPosRpc(Vector3 startPos, bool hasSound = true)
    {
        DropFromPos(startPos, hasSound);
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
        if(isFakeItem)
            return;
        
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
        
        item.SetSpawnCount(Mathf.Clamp(stackCount, 1, item.itemInfo.maxCount));
        
        NetworkObject networkObject = copy.GetComponent<NetworkObject>();
        networkObject.Spawn(true);
        
        item.DropFromPosRpc(pos, dropSound);

        return item;
    }

    public void DestroyItem()
    {
        DestroyItemServerRpc();
        
        if(IsServer == false)
            Destroy(gameObject);
    }

    [Rpc(SendTo.Server)]
    private void DestroyItemServerRpc()
    {
        NetworkObject.Despawn(true);
    }
    
    public override void OnDestroy()
    {
        if (playerWhoHasItemClient)
        {
            if (playerWhoHasItemClient.inventory.tool == this)
                playerWhoHasItemClient.inventory.UnEquipTool();
            else
                playerWhoHasItemClient.inventory.UnEquipItem(this);
        }

        transform.position = Vector3.zero;
        //ItemManager.RemoveItem(this);
    }
}
