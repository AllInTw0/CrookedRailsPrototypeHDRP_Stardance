using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
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
    private NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public string GetUserName()
    {
        return playerName.Value.ToString();
    }
    public NetworkVariable<PlayerNetworkData> playerNetworkData = new (writePerm: NetworkVariableWritePermission.Owner);
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

    [Header("PlayerName")] 
    [SerializeField]
    private Transform playerNamePrefab;
    [SerializeField]
    private Transform playerNameFollowTransform;
    [SerializeField] 
    private Vector3 playerNameOffset;
    [SerializeField] 
    private float playerNameFadeSpeed = 10f;
    private Transform playerNameTransform;
    private TMP_Text playerNameText;
    private float playerNameDuration;
    public void SetPlayerNameDuration(float duration)
    {
        playerNameDuration = duration;
    }
    //Travel dir (used for enemy spawning)
    private const int maxTravelDirCount = 10;
    private Vector3 lastPos;
    private List<Vector3> travelDirList = new List<Vector3>();
    private Vector3 travelDir = Vector3.zero;

    private void Awake()
    {
        //Get components
        camera = GetComponentInChildren<PlayerCamera>();
        camera.player = this;
        movement = GetComponent<PlayerMovement>();
        movement.player = this;
        avatar = GetComponent<PlayerAvatar>();
        avatar.player = this;
        inventory = GetComponent<PlayerInventory>();
        inventory.player = this;
        health = GetComponent<PlayerHealth>();
        health.player = this;
        rb = GetComponent<Rigidbody>();
        rbStartInterpolation = rb.interpolation;
    }
    
    protected override void OnNetworkPostSpawn()
    {
        if (IsOwner)
        {
            if (string.IsNullOrEmpty(MultiplayerManager.username))
                playerName.Value = Application.isEditor ? "Editor" : "Application";
            else
                playerName.Value = MultiplayerManager.username;
        }
        else
        {
            if (playerNameTransform == null)
                playerNameTransform = Instantiate(playerNamePrefab);
            playerNameText = playerNameTransform.GetComponentInChildren<TMP_Text>();
            playerNameText.text = GetUserName();
        }
    }

    void Start()
    {
        //Add to static variables
        if (IsOwner) localPlayer = this;
        playerList.Add(this);
        
        //Destroy components that are client side only
        if (IsOwner == false)
        {
            Debug.Log("Not owner. Destroying");
            //Destroy camera object. (Destroys PlayerInteract, PlayerCamera, Camera)
            Destroy(camera.gameObject);
            //Remove movement script
            //Destroy(movement);
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
        
        if (IsOwner)
            AwaitTeleport();
        
        InvokeRepeating(nameof(UpdateTravelDir),0.5f,0.5f);

        playerName.OnValueChanged += (value, newValue) =>
        {
            if (IsOwner == false)
            {
                if(playerNameTransform == null)
                    playerNameTransform = Instantiate(playerNamePrefab);
                playerNameText =playerNameTransform.GetComponentInChildren<TMP_Text>();
                playerNameText.text = GetUserName();
            }
        };
    }

    private async void AwaitTeleport()
    {
        while (true)//Safety net
        {
            //Teleport to starting location
            if (TeleportOnStart.active != null && PlayerTrain.active.IsInitialized())
            {
                TeleportOnStart.active.Teleport();
                return;
            }
            await Task.Delay(10);
        }
    }

    private void UpdateTravelDir()
    {
        Vector3 dir = (transform.position - lastPos + orientation.forward * 0.1f).normalized;
        travelDirList.Add(dir);
        
        if(travelDirList.Count > maxTravelDirCount)
            travelDirList.RemoveAt(0);

        Vector3 sum = Vector3.zero;
        foreach (Vector3 d in travelDirList)
        {
            sum += d;
        }

        travelDir = (sum / travelDirList.Count).normalized;

        lastPos = transform.position;
    }

    public Vector3 GetTravelDir()
    {
        return travelDir;
    }
    private void Update()
    {
        if (IsOwner)
        {
            byte movingPlatformID = 0;
            Vector3 position = transform.position;
            if (movement.IsOnRailCar(out RailCar railCar))
            {
                List<RailCar> railCarList = PlayerTrain.active.GetConsist();
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
                moveInput = InputManager.active.moveInput,
                crouching = InputManager.crouchAction.IsPressed(),
                jumping = InputManager.jumpAction.IsPressed(),
                sprinting = InputManager.sprintAction.IsPressed(),
                yRot = (short)orientation.eulerAngles.y,
                xRot = (short)camera.rotationX,
                movingPlatformID = movingPlatformID
            };
        }
        else
        {
            orientation.rotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(orientation.rotation.eulerAngles.y, playerNetworkData.Value.yRot, ref rotYVelocity, rotSmoothTime), 0);
            rotX = Mathf.SmoothDampAngle(rotX, playerNetworkData.Value.xRot, ref rotXVelocity, rotSmoothTime);

            if (playerNameTransform)
            {
                playerNameTransform.position = playerNameFollowTransform.position + playerNameOffset;
                playerNameTransform.LookAt(PlayerCamera.active.transform.position);
                
                playerNameText.color = Color.Lerp(playerNameText.color,new Color(1f,1f,1f,playerNameDuration > 0 ? 1f : 0f),playerNameFadeSpeed * Time.deltaTime);
                playerNameDuration -= Time.deltaTime;
            }
        }
    }

    private void FixedUpdate()
    {
        //if (IsOwner == false)
        //{
        //    rb.AddForce(Vector3.down * gravityForce);
        //}
    }

    private void StateUpdate(PlayerNetworkData previous, PlayerNetworkData current)
    {
        //Debug.Log("Max Error: " + maxPositionDistanceError * (current.velocity.magnitude * velocityImpactOnError)+", dist: " + Vector3.Distance(transform.position,current.position) +", vel: " + current.velocity.magnitude);
        MovingPlatformManager.active.RemoveEntry(transform);
        Vector3 targetWorldPos = current.position;
        if (current.movingPlatformID != 0)
        {
            int index = current.movingPlatformID - 1;
            Transform platform = PlayerTrain.active.GetConsist()[index].transform;
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

    public void GetCameraVectors(out Vector3 origin, out Vector3 dir)
    {
        if (IsOwner)
        {
            origin = PlayerCamera.active.transform.position;
            dir = PlayerCamera.active.transform.forward;
        }
        else
        {
            origin = transform.position + Vector3.up * movement.cameraStandingHeight;
            dir = Quaternion.AngleAxis(orientation.rotation.eulerAngles.y, Vector3.up) *
                  (Quaternion.AngleAxis(GetRotX(), Vector3.right) * Vector3.forward);
        }
    }
    public struct PlayerNetworkData : INetworkSerializable
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector2 moveInput;
        public bool crouching;
        public bool sprinting;
        public bool jumping;
        public short yRot, xRot;
        public byte movingPlatformID; // 0-255 (0 - not on platform)
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref moveInput);
            serializer.SerializeValue(ref crouching);
            serializer.SerializeValue(ref jumping);
            serializer.SerializeValue(ref sprinting);
            serializer.SerializeValue(ref yRot);
            serializer.SerializeValue(ref xRot);
            serializer.SerializeValue(ref movingPlatformID);
        }
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        Destroy(playerNameTransform.gameObject);
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

    public static List<Player> GetDeadPlayers()
    {
        List<Player> deadList = new List<Player>();
        foreach (Player player in playerList)
        {
            if(player.health.isAlive == false)
                deadList.Add(player);
        }
        return deadList;
    }

    public static void ReviveDeadPlayers(int reviveHealth = 50)
    {
        foreach (Player player in GetDeadPlayers())
        {
            player.health.TakeDamage(-reviveHealth);
            player.health.ReviveRpc();
        }
    }
}
