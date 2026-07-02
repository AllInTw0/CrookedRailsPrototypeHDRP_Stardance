using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Player : NetworkBehaviour
{
    //Static
    public static List<Player> playerList = new List<Player>();
    public static Player localPlayer = null;
    private static byte nextPlayerID = 0; // byte is a 0-255 int
    
    //Network
    private NetworkVariable<PlayerNetworkData> playerNetworkData = new (writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<byte> playerIDNetworkVariable = new (writePerm: NetworkVariableWritePermission.Server);
    public byte playerID
    {
        get { return playerIDNetworkVariable.Value; }
    }
    //Components
    [NonSerialized] public new PlayerCamera camera;
    [NonSerialized] public PlayerMovement movement;
    [NonSerialized] public PlayerAvatar avatar;
    [NonSerialized] public PlayerInventory inventory;
    [NonSerialized] public PlayerHealth health;
    [NonSerialized] public Rigidbody rb;
    
    //Refrencces
    [Header("Transforms")] 
    public Transform orientation;
    [Header("Prediction")]
    [SerializeField] private float gravityForce = 18f;
    [SerializeField] private float rotSmoothTime = 0.2f;
    [SerializeField] private float maxPositionDistanceError = 0.1f;
    [SerializeField] private float velocityImpactOnError = 0.75f;
    private float rotYVelocity;
    private float rotXVelocity, rotX;
    
    private RigidbodyInterpolation rbStartInterpolation;
    void Start()
    {
        //Add to static variables
        if (IsOwner) localPlayer = this;
        playerList.Add(this);
        
        //Get components
        camera = GetComponentInChildren<PlayerCamera>();
        camera.player = this;
        movement = GetComponent<PlayerMovement>();
        avatar = GetComponent<PlayerAvatar>();
        avatar.player = this;
        inventory = GetComponent<PlayerInventory>();
        inventory.player = this;
        health = GetComponent<PlayerHealth>();
        health.player = this;
        rb = GetComponent<Rigidbody>();
        rbStartInterpolation = rb.interpolation;
        
        //Destroy components that are client side only
        if (IsOwner == false)
        {
            Debug.Log("Not owner. Destroying");
            //Destroy camera object. (Destroys PlayerInteract, PlayerCamera, Camera)
            Destroy(camera.gameObject);
            //Remove movement script
            Destroy(movement);
        }
        
        //Set player mesh
        avatar.SetPlayerMesh(IsOwner);
        
        //Position Networking
        if(IsOwner == false)
            playerNetworkData.OnValueChanged += StateUpdate;
        
        //Give player an id
        if (IsServer)
        {
            playerIDNetworkVariable.Value = nextPlayerID;
            nextPlayerID++;
        }
        
        //Teleport to starting location
        if(TeleportOnStart.active != null)
            TeleportOnStart.active.Teleport();
    }

    private void Update()
    {
        if (IsOwner)
        {
            byte movingPlatformID = 0;
            Vector3 position = transform.position;
            if (movement.IsOnRailCar(out RailCar railCar))
            {
                List<RailCar> railCarList = Train.playerTrain.GetConsist();
                for (int i = 0; i < railCarList.Count; i++)
                {
                    if (railCarList[i] == railCar)
                    {
                        movingPlatformID = (byte)(i + 1);
                        position = railCar.transform.InverseTransformPoint(position);
                        break;
                    }
                }
            }
            
            playerNetworkData.Value = new PlayerNetworkData()
            {
                position = position,
                velocity = rb.linearVelocity,
                yRot = (short)orientation.eulerAngles.y,
                xRot = (short)camera.rotationX,
                movingPlatformID = movingPlatformID
            };
        }
        else
        {
            orientation.rotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(orientation.rotation.eulerAngles.y, playerNetworkData.Value.yRot, ref rotYVelocity, rotSmoothTime), 0);
            rotX = Mathf.SmoothDampAngle(rotX, playerNetworkData.Value.xRot, ref rotXVelocity, rotSmoothTime);
        }
    }

    private void FixedUpdate()
    {
        if (IsOwner == false)
        {
            rb.AddForce(Vector3.down * gravityForce);
        }
    }

    private void StateUpdate(PlayerNetworkData previous, PlayerNetworkData current)
    {
        //Debug.Log("Max Error: " + maxPositionDistanceError * (current.velocity.magnitude * velocityImpactOnError)+", dist: " + Vector3.Distance(transform.position,current.position) +", vel: " + current.velocity.magnitude);
        MovingPlatformManager.active.RemoveEntry(transform);
        Vector3 targetWorldPos = current.position;
        if (current.movingPlatformID != 0)
        {
            int index = current.movingPlatformID - 1;
            Transform platform = Train.playerTrain.GetConsist()[index].transform;
            MovingPlatformManager.active.AddEntry(rb,transform,platform,true,rbStartInterpolation);
            targetWorldPos = platform.TransformPoint(targetWorldPos);
        }
        
        if(Vector3.Distance(transform.position,targetWorldPos) > maxPositionDistanceError * (current.velocity.magnitude * velocityImpactOnError))
            rb.position = targetWorldPos;
        rb.linearVelocity = current.velocity;
    }

    public float GetRotX()
    {
        if (IsOwner) return camera.rotationX;
        return rotX;
    }
    
    private struct PlayerNetworkData : INetworkSerializable
    {
        public Vector3 position;
        public Vector3 velocity;
        public short yRot, xRot;
        public byte movingPlatformID; // 0-255 (0 - not on platform)
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref yRot);
            serializer.SerializeValue(ref xRot);
            serializer.SerializeValue(ref movingPlatformID);
        }
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        playerList.Remove(this);
    }

    public static Player GetPlayerByClientID(ulong clientID)
    {
        for (int i = 0; i < playerList.Count; i++)
        {
            if(playerList[i].OwnerClientId == clientID)
                return playerList[i];
        }
        Debug.LogWarning("Couldn't find player by ID: " + clientID);
        return null;
    }
}
