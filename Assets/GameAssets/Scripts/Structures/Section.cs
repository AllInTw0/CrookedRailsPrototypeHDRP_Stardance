using System.Collections.Generic;
using UnityEngine;

public class Section : MonoBehaviour
{
    public List<GameObject> globalEndPrefabList;
    public List<GameObject> globalConnectedPrefabList;
    [SerializeField]
    private List<Connection> connectionList;
    public void AddTranfromToConnections(Transform transform)
    {
        if (connectionList == null) connectionList = new List<Connection>();

        Connection newConnection = new Connection();
        newConnection.connectionTransform = transform;
        newConnection.useGlobalEnds = true;
        connectionList.Add(newConnection);
    }
    [SerializeField]
    private float length;

    //Run-time
    [HideInInspector]
    public List<Connection> connectingConnectionList;
    [HideInInspector]
    public List<Connection> nonConnectingConnectionList;
    [HideInInspector]
    public bool isOverlapping;

    private BoxCollider[] boxColliderArray;
    private void Awake()
    {
        //Initialize()
    }
    public void Initialize()
    {
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
    public float GetLength()
    {
        return length;
    }
    public BoxCollider[] GetBoxColliderArray()
    {
        return boxColliderArray;
    }
}
