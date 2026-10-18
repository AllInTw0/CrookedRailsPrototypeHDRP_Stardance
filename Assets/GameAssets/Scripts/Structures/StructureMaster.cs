using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum LengthType
{
    None,
    PlayerTrainLength,
    MaxChosenTrainLength
}
public enum StructureType
{
    None,
    Station,
    Payload
}
[System.Serializable]
public class Connection 
{
    //public string name;
    public Transform connectionTransform;
    public List<string> validConnectionNameList;
    public List<GameObject> endPrefabList;
    public List<GameObject> connectedPrefabList;

    public bool useGlobalEnds;
    public bool dontConnect;
    public bool ignoreConnections; // Able to connect to connections that are already connected
    public bool ignore;
    //Nonserializeable
    [HideInInspector]
    public Connection connectedConnection;
    [HideInInspector]
    public Section section;
}
[System.Serializable]
public class GenerationEntry
{
    public List<GameObject> sectionPrefabList;
    
    public enum CountType
    {
        minMaxRandom,
        fillLenght,
        probabilityCurve
    }
    [Header("Count")]
    public CountType countType;
    public Vector2Int minMaxCount = Vector2Int.one;
    public LengthType lengthType;
    public float lengthAddition;
    public AnimationCurve countCurve;
    [Header("Other")]
    public bool obligatory;
}
public class StructureMaster : NetworkBehaviour
{
    public static int nextStructureID = 1;
    public static List<StructureMaster> structureMasterList = new List<StructureMaster>();
    private NetworkVariable<int> structureID = new(writePerm: NetworkVariableWritePermission.Server);
    public int GetStructureID(){return structureID.Value;}
    public static StructureMaster GetStructureById(int structureID)
    {
        foreach (StructureMaster structure in structureMasterList)
        {
            if (structure.GetStructureID() == structureID)
                return structure;
        }
        Debug.LogWarning("Didnt find structure with ID: " + structureID);
        return null;
    }
    
    [FormerlySerializedAs("structureList")] 
    public List<StructureGenerator> structureGeneratorList;
    public StructureType structureType;
    public Connection startConnection;
    public LayerMask overlapCheckLayerMask;
    
    [Header("Spawn params")]
    public bool onlyRotateY = true;
    public int maxRetryCount;
    
    [Header("AutoStop")]
    public bool addAutoStop;
    public AutoStopType stopType;
    public float stopOffset;
    
    private int retryCount = 0;

    public static List<StructureMaster> generatingStructures = new List<StructureMaster>();
    public static List<StructureMaster> finnishedStructures = new List<StructureMaster>();

    [HideInInspector]
    public List<Connection> structureConnectionList;
    [HideInInspector]
    public List<Section> structureSectionList;
    private int count;

    [HideInInspector]
    public UnityEvent<Section> onSectionAdded;
    [HideInInspector]
    public UnityEvent onFinishedGenerating;
    
    private StructureSO structureSO;
    private float sectionProgress;
    private TrackSection section;

    private bool IsSynced = false;
    
    public void Generate(float sectionProgress = 0f, TrackSection section = null, StructureSO structureSO = null)
    {
        this.sectionProgress = sectionProgress;
        this.section = section;
        this.structureSO = structureSO;
        
        generatingStructures.Add(this);
        
        //Multiplayer
        if (TryGetComponent(out NetworkObject networkObject))
        {
            IsSynced = true;
            networkObject.Spawn(true);
        }

        GenerateStructure();
    }

    private void Start()
    {
        structureMasterList.Add(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            structureID.Value = nextStructureID;
            nextStructureID++;
        }
    }
    private void GenerateStructure()
    {
        structureConnectionList = new List<Connection>();
        startConnection.connectedConnection = null;
        structureConnectionList.Add(startConnection);
        structureSectionList = new List<Section>();
        count = 0;

        onSectionAdded = new UnityEvent<Section>();

        StartCoroutine(GenerateIEnumerable());

        if (structureType == StructureType.Station)
        {
            //GameStateManager.isStationSpawned = true;
        }
    }
    public IEnumerator GenerateIEnumerable()
    {
        //Debug.Log("Running!");
        bool added = false;
        for (int i = 0; i < structureGeneratorList.Count; i++)
        {
            yield return StartCoroutine(structureGeneratorList[i].Generate(this));
            if (added == false && ((structureGeneratorList[i] is PayloadGenerator && structureType == StructureType.Payload) || (structureGeneratorList[i] is BasicStructureGenerator && structureType != StructureType.Payload)))
            {
                FinishedGenerating();
                added = true;
            }
        }
        if (added == false)
        {
            FinishedGenerating();
        }
        yield break;
    }
    private void FinishedGenerating()
    {
        //Finished generating!
        finnishedStructures.Add(this);
        if (section != null)
        {
            if (addAutoStop)
            {
                section.SetAutoStop(sectionProgress + stopOffset, stopType);
            }
        }
        
        if (IsSynced)
        {
            Debug.Log("Syncing Structure To Clients: " + gameObject.name);
            int syncedSectionCount = 0;
            foreach (Section section in structureSectionList)
            {
                if (section.SyncToClient())
                    syncedSectionCount++;
            }
            
            FinishedGeneratingClientRpc(syncedSectionCount);
        }
        else
        {
            Debug.LogWarning("Not Syncing Structure To Clients: " + gameObject.name);
        }
        onFinishedGenerating.Invoke();
    }
    [Rpc(SendTo.NotServer)]
    private void FinishedGeneratingClientRpc(int sectionCount)
    {
        FinishedGeneratingClient(sectionCount);
    }

    private async void FinishedGeneratingClient(int sectionCount)
    {
        generatingStructures.Add(this);
        //Wait for all sections to sync
        Debug.Log("FinishedGeneratingClient");
        while (sectionCount != structureSectionList.Count)
        {
            Debug.Log("Waiting For Sections " + structureSectionList.Count + "/" + sectionCount);
            await Task.Delay(100);
        }
        
        foreach (Section sectionScript in structureSectionList)
        {
            ConnectNearbyConnections(sectionScript);
        }
        SpawnEndPrefabs();
        finnishedStructures.Add(this);
        
        onFinishedGenerating.Invoke();
    }
    public void OnDestroy()
    {
        generatingStructures.Remove(this);
        finnishedStructures.Remove(this);
        
        DestroyStructure();
        if (structureType == StructureType.Station)
        {
            //GameStateManager.isStationSpawned = false;
        }
    }
    public void DestroyStructure()
    {
        foreach (Section section in structureSectionList)
        {
            section.DestroySection();
        }
    }
    public void RestartGeneration()
    {
        retryCount++;
        Debug.LogWarning("Restarting Generation: " + transform.name);
        StopAllCoroutines();
        foreach (StructureGenerator structure in structureGeneratorList)
        {
            structure.StopGenerating();
        }
        DestroyStructure();
        if (retryCount > maxRetryCount)
        {
            Debug.LogWarning("Reached max retries!");
            NetworkObject.Despawn(true);
            return;
        }
        GenerateStructure();
    }
    public float GetLength(LengthType lengthType)
    {
        if (lengthType == LengthType.PlayerTrainLength)
            return PlayerTrain.active != null ? PlayerTrain.active.GetConsistLenght() : 50f;
        else if(lengthType == LengthType.MaxChosenTrainLength)
        {
            float maxLength = 0f;
            for (int i = 0; i < HaulingJobManager.generatedHaulingJobList.Count; i++)
            {
                float l = HaulingJobManager.generatedHaulingJobList[i].GetConsistLength() + PlayerTrain.active.CalculateConsistLength(true);
                if (l > maxLength) maxLength = l;
            }
            return maxLength;
        }

        Debug.LogWarning("Couldnt get length");
        return 0f;
    }
    public void SpawnEndPrefabs()
    {
        foreach (Connection connection in structureConnectionList)
        {
            List<GameObject> endList = connection.useGlobalEnds ? connection.section.globalEndPrefabList : connection.endPrefabList;
            List<GameObject> connectedList = connection.useGlobalEnds ? connection.section.globalConnectedPrefabList : connection.connectedPrefabList;

            Transform copy = null;
            if((connection.connectedConnection == null || connection.connectedConnection == startConnection) && endList.Count > 0)
            {
                int randomIndex = Util.SeedToRandomIndex(Mathf.Round(connection.connectionTransform.position.magnitude), endList.Count - 1);
                copy = Instantiate(endList[randomIndex]).transform;
            } 
            else if (connectedList.Count > 0)
            {
                int randomIndex = Util.SeedToRandomIndex(Mathf.Round(connection.connectionTransform.position.magnitude), connectedList.Count - 1);
                copy = Instantiate(connectedList[randomIndex]).transform;
            }
            else 
                continue;

            copy.SetParent(connection.connectionTransform);
            copy.localPosition = Vector3.zero;
            copy.localRotation = Quaternion.identity;
        }
    }
    public IEnumerator SpawnSection(GameObject prefab)
    {
        GameObject sectionObject = Instantiate(prefab, transform);
        Transform sectionTransform = sectionObject.transform;
        Section sectionScript = sectionObject.GetComponent<Section>();

        sectionScript.Initialize(this);

        //Get all valid connections
        List<Connection> sectionConnectionList = new List<Connection>();
        List<List<Connection>> validConnectionList = new List<List<Connection>>();

        foreach (Connection connection in sectionScript.GetConnectingConnectionList())
        {
            List<Connection> connectionList = GetValidConnectionsForConnection(connection);
            //Debug.Log("c: " + connectionList.Count);

            if (connectionList.Count > 0 && connection.ignore == false) 
            {
                sectionConnectionList.Add(connection);
                validConnectionList.Add(connectionList);
            }
        }

        //Chose connection
        if(validConnectionList.Count == 0 && !sectionScript.targetSpecialConnection)
        {
            Debug.LogWarning("Cant find valid connection: validConnectionList.Count == 0, " + sectionObject.name +", sectionCount: " + structureSectionList.Count + ", connectionCount: " + structureConnectionList.Count);
            DestroyImmediate(sectionObject);
            yield break;
        }
        if (sectionScript.targetSpecialConnection)
        {
            int tries = 5;
            while (tries >= 0)
            {
                if (tries == 0)
                {
                    Debug.Log("Couldnt spawn " + sectionObject.name);
                    DestroyImmediate(sectionObject);
                    yield break; //return;
                }

                if (sectionScript.connectToTrackInterval != Vector2.zero)
                {
                    Vector3 pos;
                    Vector3 dir;
                    if (section != null)
                    {
                        TrackManager.active.GetTrackPositionAndDirVectorFromProgress(
                            sectionProgress + Random.Range(sectionScript.connectToTrackInterval.x,
                                sectionScript.connectToTrackInterval.y), section, out pos, out dir);
                    }
                    else
                    {
                        pos = transform.position + transform.forward *
                            Random.Range(sectionScript.connectToTrackInterval.x,
                                sectionScript.connectToTrackInterval.y);
                        dir = transform.forward;
                    }

                    sectionTransform.position = pos;
                    sectionTransform.LookAt(pos + new Vector3(dir.x, 0, dir.z).normalized);
                }

                if (DoesSectionOverlap(sectionScript))
                {
                    tries--;
                    continue;
                }
                else
                {
                    break;
                }
            }

            foreach (StructureGenerator structureGenerator in sectionScript.GetComponentsInChildren<StructureGenerator>())
            {
                yield return structureGenerator.Generate(this);
            }
        }
        while (sectionConnectionList.Count > 0 && validConnectionList.Count > 0)
        {
            int connectionListIndex = Random.Range(0, validConnectionList.Count);
            int connectionIndex = Random.Range(0, validConnectionList[connectionListIndex].Count);
            Connection sectionConnection = sectionConnectionList[connectionListIndex];
            Connection otherSectionConnection = validConnectionList[connectionListIndex][connectionIndex];
            //Set position
            //sectionTransform.SetParent(otherSectionConnection.connectionTransform);
            //sectionTransform.localRotation = Quaternion.identity;
            //sectionTransform.localPosition = -sectionConnection.connectionTransform.localPosition;
            //sectionTransform.RotateAround(sectionConnection.connectionTransform.position, Vector3.up, otherSectionConnection.connectionTransform.eulerAngles.y - sectionConnection.connectionTransform.eulerAngles.y + 180f);
            //sectionTransform.SetParent(transform);
            sectionTransform.rotation = otherSectionConnection.connectionTransform.rotation;
            sectionTransform.position =
                otherSectionConnection.connectionTransform.TransformPoint(-sectionConnection.connectionTransform
                    .localPosition);
            sectionTransform.RotateAround(sectionConnection.connectionTransform.position, Vector3.up,
                otherSectionConnection.connectionTransform.eulerAngles.y -
                sectionConnection.connectionTransform.eulerAngles.y + 180f);


            //Reset overlapping bool
            sectionScript.isOverlapping = false;

            //Rename strings
            count++;
            foreach (Connection connection in sectionScript.GetAllConnectionList())
            {
                //rename
                connection.connectionTransform.name = RenameString(connection.connectionTransform.name, count);
            }

            //Connect only this section
            if (otherSectionConnection.dontConnect == false)
                sectionConnection.connectedConnection = otherSectionConnection;

            foreach (StructureGenerator structureGenerator in sectionScript.GetComponentsInChildren<StructureGenerator>())
            {
                if(structureGenerator is not TerrainModifier)
                    yield return structureGenerator.Generate(this);
            }

            //Check overlap
            if (DoesSectionOverlap(sectionScript))
            {
                //try another connection

                validConnectionList[connectionListIndex].RemoveAt(connectionIndex);
                if(validConnectionList[connectionListIndex].Count == 0)
                {
                    validConnectionList.RemoveAt(connectionListIndex);
                    sectionConnectionList.RemoveAt(connectionListIndex);
                }

                if(sectionConnectionList.Count == 0 || validConnectionList.Count == 0)
                {
                    Debug.Log("Couldnt spawn " + sectionObject.name);
                    DestroyImmediate(sectionObject);
                    yield break; //return;
                }
                sectionConnection.connectedConnection = null;
                continue;
            }

            //Connect other section
            if (sectionConnection.dontConnect == false)
                otherSectionConnection.connectedConnection = sectionConnection;
            
            foreach (StructureGenerator structureGenerator in sectionScript.GetComponentsInChildren<TerrainModifier>())
            {
                //if (structureGenerator is not TerrainModifier)
                yield return structureGenerator.Generate(this);
            }
            break;
        }

        if (sectionScript.randomRotation != Vector2.one)
        {
            sectionScript.transform.rotation *= Quaternion.Euler(0, Random.Range(sectionScript.randomRotation.x, sectionScript.randomRotation.y), 0);
        }
        
        //Debug.DrawLine(sectionConnection.connectionTransform.position, otherSectionConnection.connectionTransform.position, Color.purple, 60f);
        //Debug.DrawRay(sectionConnection.connectionTransform.position, Vector3.up, Color.purple, 60f);


        //Connect connections close to each other
        ConnectNearbyConnections(sectionScript);

        structureConnectionList.AddRange(sectionScript.GetAllConnectionList());
        structureSectionList.Add(sectionScript);


        onSectionAdded.Invoke(sectionScript); //return sectionScript;
    }

    private void ConnectNearbyConnections(Section sectionScript)
    {
        //Connect connections close to each other
        foreach (Connection connection in sectionScript.GetAllConnectionList())
        {
            //connect
            foreach (Connection connectionB in structureConnectionList)
            {
                if (sectionScript.GetAllConnectionList().Contains(connectionB) == false)
                {
                    if (Vector3.Distance(connection.connectionTransform.position,
                            connectionB.connectionTransform.position) < 0.05f)
                    {
                        if (connectionB.dontConnect == false)
                            connection.connectedConnection = connectionB;
                        if (connection.dontConnect == false)
                            connectionB.connectedConnection = connection;
                    }
                }
            }
        }
    }
    public List<Connection> GetValidConnectionsForConnection(Connection connection)
    {
        List<Connection> connectionList = new List<Connection>();
        foreach (Connection otherConnection in structureConnectionList)
        {
            if (((otherConnection.connectedConnection == null && connection.ignoreConnections == false) || connection.ignoreConnections) && DoesStringListMatch(connection.validConnectionNameList, otherConnection.connectionTransform.name))
            {
                connectionList.Add(otherConnection);
            }
        }
        return connectionList;
    }
    public bool DoesSectionOverlap(Section section)
    {
        //This is set to true by section's scripts
        if (section.isOverlapping) return true;

        BoxCollider[] boxColliderArray = section.GetBoxColliderArray();
        foreach (BoxCollider boxCollider in boxColliderArray)
        {
            LayerMask modifiedLayerMask = ~boxCollider.excludeLayers & (overlapCheckLayerMask | boxCollider.includeLayers);
            
            Collider[] overlapingColiderArray = Util.PhysicsBoxColliderOverlap(boxCollider, modifiedLayerMask);
            foreach (Collider collider in overlapingColiderArray)
            {
                if (collider.TryGetComponent(out TerrainModifier terrainModifier)) 
                    continue;

                bool canSpawn = false;
                foreach (BoxCollider sectionBoxCollider in boxColliderArray)
                {
                    if (collider == sectionBoxCollider) 
                    {
                        canSpawn = true;
                        break;
                    }
                }
                if (canSpawn == false)
                {
                    Debug.Log(section.gameObject.name + " overlaps " + collider.gameObject.name);
                    Debug.DrawLine(section.transform.TransformPoint(boxCollider.center), collider.transform.position, Color.red, 60f);
                    return true;
                }
            }
        }
        return false;
    }
    public bool DoesStringListMatch(List<string> stringList, string targetString)
    {
        foreach (string str in stringList)
        {
            bool stringsMatch = DoStringsMatch(str, targetString);
            //Debug.Log(str + ", " + targetString + " : " + stringsMatch);

            if (stringsMatch) return true;
        }
        return false;
    }

    public bool DoStringsMatch(string str, string targetStr)
    {
        str = str.Trim();
        targetStr = targetStr.Trim();

        if (str == targetStr) return true;

        //Special Characters
        int starIndex = str.IndexOf('*');
        if(starIndex != -1)
        {
            if(targetStr.Length >= starIndex)
                return str[0..starIndex] == targetStr[0..starIndex];
        }

        return false;
    }
    public string RenameString(string str, int countIndex)
    {
        int index = str.IndexOf("_");
        if (index != -1)
        {
            str = str[0..index] + countIndex;
        }
        return str;
    }
}
