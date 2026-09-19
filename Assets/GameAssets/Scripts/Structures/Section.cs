using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Section : NetworkBehaviour
{
    private NetworkVariable<int> structureID = new (writePerm: NetworkVariableWritePermission.Server);
    
    public List<GameObject> globalEndPrefabList;
    public List<GameObject> globalConnectedPrefabList;
    [SerializeField]
    private List<Connection> connectionList;
    public void AddTransformToConnections(Transform transform)
    {
        if (connectionList == null) connectionList = new List<Connection>();

        Connection newConnection = new Connection();
        newConnection.connectionTransform = transform;
        newConnection.useGlobalEnds = true;
        connectionList.Add(newConnection);
    }
    [SerializeField]
    private float length;
    [SerializeField]
    public Vector2 randomRotation;
    [Header("Special Connections")] 
    [SerializeField]
    public Transform targetSpecialConnection;
    [SerializeField]
    public Vector2 connectToTrackInterval;
    
    //Run-time
    [HideInInspector]
    public List<Connection> connectingConnectionList;
    [HideInInspector]
    public List<Connection> nonConnectingConnectionList;
    [HideInInspector]
    public bool isOverlapping;

    private BoxCollider[] boxColliderArray;
    private StructureMaster structureMaster;
    private void Awake()
    {
        //Initialize()
    }
    
    public void Initialize(StructureMaster structureMaster)
    {
        this.structureMaster = structureMaster;
        
        //Sort connections
        connectingConnectionList = new List<Connection>();
        nonConnectingConnectionList = new List<Connection>();

        foreach (Connection connection in connectionList)
        {
            connection.section = this;
            if (connection.validConnectionNameList.Count > 0)
            {
                connectingConnectionList.Add(connection);
            }
            else
            {
                nonConnectingConnectionList.Add(connection);
            }
        }

        boxColliderArray = GetComponents<BoxCollider>();
    }

    public bool SyncToClient()
    {
        if (TryGetComponent(out NetworkObject networkObject))
        {
            transform.SetParent(null);
            networkObject.Spawn(true);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void DestroySection()
    {
        if (IsServer && TryGetComponent(out NetworkObject networkObject) && networkObject.IsSpawned)
            networkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
    public override void OnNetworkSpawn()
    {
        if(IsServer)
            structureID.Value = structureMaster.GetStructureID();
        else
            AwaitStructureID();
    }

    private async void AwaitStructureID()
    {
        while (structureID.Value == 0)
        {
            Debug.Log("Waiting for structureID");
            await Task.Delay(10);
        }
        
        structureMaster = StructureMaster.GetStructureById(structureID.Value);
        Initialize(structureMaster);
            
        structureMaster.structureSectionList.Add(this);
        structureMaster.structureConnectionList.AddRange(GetAllConnectionList());
    }

    public List<Connection> GetConnectingConnectionList()
    {
        return connectingConnectionList;
    }
    public List<Connection> GetNonConnectingConnectionList()
    {
        return nonConnectingConnectionList;
    }
    public List<Connection> GetAllConnectionList()
    {
        return connectionList;
    }

    public Connection GetConnectionByTransform(Transform t)
    {
        foreach (Connection connection in connectionList)
        {
            if (connection.connectionTransform == t)
            {
                return connection;
            }
        }
        return null;
    }
    
    public float GetLength()
    {
        return length;
    }
    public BoxCollider[] GetBoxColliderArray()
    {
        return boxColliderArray;
    }
}
