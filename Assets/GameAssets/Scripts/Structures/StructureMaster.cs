using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
    Station
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
public class StructureMaster : MonoBehaviour
{
    public List<StructureGenerator> structureList;
    public StructureType structureType;
    public Connection startConnection;
    public LayerMask overlapCheckLayerMask;

    public int maxRetryCount;
    private int retryCount = 0;

    public static int generatingStructures;
    public static int finnishedStructures;

    [HideInInspector]
    public List<Connection> structureConnectionList;
    [HideInInspector]
    public List<Section> structureSectionList;
    private int count;

    [HideInInspector]
    public UnityEvent<Section> onSectionAdded;

    private StructureSO structureSO;
    private float sectionProgress;
    private TrackSection section;
    public void Generate(float sectionProgress = 0f, TrackSection section = null, StructureSO structureSO = null)
    {
        this.sectionProgress = sectionProgress;
        this.section = section;
        this.structureSO = structureSO;

        generatingStructures++;
        GenerateStructure();
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
        foreach (StructureGenerator structure in structureList)
        {
            yield return StartCoroutine(structure.Generate(this));
            if (structure is BasicStructureGenerator && added == false)
            {
                FinnishedGenerating();
                added = true;
            }
        }
        if (added == false)
        {
            FinnishedGenerating();
        }
        yield break;
    }
    private void FinnishedGenerating()
    {
        //Finnished generating!
        finnishedStructures++;
        if (section != null && structureSO != null)
        {
            if (structureSO.addAutoStop)
            {
                section.SetAutoStop(sectionProgress, structureSO.stopType);
            }
        }
    }
    public void OnDestroy()
    {
        if (structureType == StructureType.Station)
        {
            //GameStateManager.isStationSpawned = false;
        }
    }
    public void DestroyStructure()
    {
        foreach (Section section in structureSectionList)
        {
            DestroyImmediate(section.gameObject);
        }
    }
    public void RestartGeneration()
    {
        retryCount++;
        Debug.LogWarning("Restarting Generation: " + transform.name);
        StopAllCoroutines();
        foreach (StructureGenerator structure in structureList)
        {
            structure.StopGenerating();
        }
        DestroyStructure();
        if (retryCount > maxRetryCount)
        {
            Destroy(gameObject);
            Debug.LogWarning("Reached max retries!");
            finnishedStructures++;
            return;
        }
        GenerateStructure();
    }
    public float GetLength(LengthType lengthType)
    {
        if (lengthType == LengthType.PlayerTrainLength)
            return Train.playerTrain != null ? Train.playerTrain.GetConsistLenght() : 50f;
        else if(lengthType == LengthType.MaxChosenTrainLength)
        {
            float maxLength = 0f;
            for (int i = 0; i < HaulingJobManager.generatedHaulingJobList.Count; i++)
            {
                float l = HaulingJobManager.generatedHaulingJobList[i].GetConsistLength() + Train.playerTrain.CalculateConsistLength(true);
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
                copy = Instantiate(endList[Random.Range(0, endList.Count)]).transform;

            } 
            else if (connectedList.Count > 0)
            {
                copy = Instantiate(connectedList[Random.Range(0, connectedList.Count)]).transform;
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

        sectionScript.Initialize();

        //Get all valid connections
        List<Connection> sectionConnectionList = new List<Connection>();
        List<List<Connection>> validConnectionList = new List<List<Connection>>();

        foreach (Connection connection in sectionScript.GetConnectingConnectionList())
        {
            List<Connection> connectionList = GetValidConnectionsForConnection(connection);
            //Debug.Log("c: " + connectionList.Count);

            if (connectionList.Count > 0) 
            {
                sectionConnectionList.Add(connection);
                validConnectionList.Add(connectionList);
            }
        }

        //Chose connection
        if(validConnectionList.Count == 0)
        {
            Debug.LogWarning("Cant find valid connection: validConnectionList.Count == 0, " + sectionObject.name +", sectionCount: " + structureSectionList.Count + ", connectionCount: " + structureConnectionList.Count);
            DestroyImmediate(sectionObject);
            yield break;
        }
        while (sectionConnectionList.Count > 0 && validConnectionList.Count > 0)
        {
            int connectionListIndex = Random.Range(0, validConnectionList.Count);
            int connectionIndex = Random.Range(0, validConnectionList[connectionListIndex].Count);
            Connection sectionConnection = sectionConnectionList[connectionListIndex];
            Connection otherSectionConnection = validConnectionList[connectionListIndex][connectionIndex];


            //Set position
            sectionTransform.SetParent(otherSectionConnection.connectionTransform);
            sectionTransform.localRotation = Quaternion.identity;
            sectionTransform.localPosition = -sectionConnection.connectionTransform.localPosition;
            sectionTransform.RotateAround(sectionConnection.connectionTransform.position, Vector3.up, otherSectionConnection.connectionTransform.eulerAngles.y - sectionConnection.connectionTransform.eulerAngles.y + 180f);
            sectionTransform.SetParent(transform);

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
        //Debug.DrawLine(sectionConnection.connectionTransform.position, otherSectionConnection.connectionTransform.position, Color.purple, 60f);
        //Debug.DrawRay(sectionConnection.connectionTransform.position, Vector3.up, Color.purple, 60f);


        //Connect connections close to each other
        foreach (Connection connection in sectionScript.GetAllConnectionList())
        {
            //connect
            foreach (Connection connectionB in structureConnectionList)
            {
                if (Vector3.Distance(connection.connectionTransform.position, connectionB.connectionTransform.position) < 0.01f)
                {
                    if (connectionB.dontConnect == false)
                        connection.connectedConnection = connectionB;
                    if (connection.dontConnect == false)
                        connectionB.connectedConnection = connection;
                }
            }
        }

        structureConnectionList.AddRange(sectionScript.GetAllConnectionList());
        structureSectionList.Add(sectionScript);


        onSectionAdded.Invoke(sectionScript); //return sectionScript;
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
