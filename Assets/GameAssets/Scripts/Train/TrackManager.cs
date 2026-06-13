using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PathPoint
{
    public Vector3 position;
    public float distance;
    public bool bridge;
    public PathPoint(Vector3 position,float distance)
    {
        this.position = position;
        this.distance = distance;
        //bridge = false;
    }
}
public enum AutoStopType
{
    Front,
    TenderHatch,
    Supersonic,
    Station,
    Structure,
    SlowDown
}
public class AutoStop
{
    public float distance;
    public AutoStopType stopType;
    public bool ignore;
}
public class TrackSection
{
    private static int nextID = 0;
    
    public Point pointA;
    public Point pointB;
    public List<PathPoint> path;
    public TrackSection nextSection;
    public TrackSection previousSection;

    public AutoStop autoStop;

    public int sectionID;
    
    public float length
    {
        get
        {
            return path[^1].distance;
        }
    }
    
    public TrackSection(Point pointA,Point pointB,List<PathPoint> path)
    {
        this.pointA = pointA;
        this.pointB = pointB;
        this.path = path;

        sectionID = nextID;
        nextID++;
    }

    public List<GameObject> associatedObjects = new List<GameObject>();
    public Dictionary<ObjectPool, List<GameObject>> pooledAssociatedObjects;
    public void AddObject(GameObject obj)
    {
        associatedObjects.Add(obj);
    }
    public void AddObject(ObjectPool targetPool, GameObject obj)
    {
        if (pooledAssociatedObjects == null)
            pooledAssociatedObjects = new Dictionary<ObjectPool, List<GameObject>>();

        if(pooledAssociatedObjects.TryGetValue(targetPool, out List<GameObject> objectList))
        {
            objectList.Add(obj);
        }
        else
        {
            pooledAssociatedObjects.Add(targetPool, new List<GameObject>() { obj });
        }
    }
    public void SetNextSection(TrackSection section)
    {
        if (section == null) return;
        
        nextSection = section;
        section.previousSection = this;
    }
    public void SetPreviousSection(TrackSection section)
    {
        if (section == null) return;
        
        previousSection = section;
        section.nextSection = this;
    }
    public TrackSection GetNextSection()
    {
        return nextSection;
    }
    public TrackSection GetPreviousSection()
    {
        return previousSection;
    }
    public void SetAutoStop(float distance, AutoStopType type = AutoStopType.Front)
    {
        autoStop = new AutoStop();
        autoStop.distance = distance;
        autoStop.stopType = type;
    }
}
public class TrackManager : NetworkBehaviour
{
    public static TrackManager active;
    //Run Time
    [NonSerialized] 
    public List<TrackSection> trackSectionList = new List<TrackSection>();
    [NonSerialized] 
    public float progressOffset = 0f;
    
    private struct TrackSectionNetworkData : INetworkSerializable
    {
        public int sectionID;
        public int previousSectionID;
        public int nextSectionID;
        
        public Vector3 pointAPos;
        public Vector3 pointAHandleForward;
        //private Vector3 pointAHandleBackward;
    
        public Vector3 pointBPos;
        //private Vector3 pointBHandleForward;
        public Vector3 pointBHandleBackward;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref sectionID);
            serializer.SerializeValue(ref previousSectionID);
            serializer.SerializeValue(ref nextSectionID);
            serializer.SerializeValue(ref pointAPos);
            serializer.SerializeValue(ref pointAHandleForward);
            //serializer.SerializeValue(ref pointAHandleBackward);
            serializer.SerializeValue(ref pointBPos);
            //serializer.SerializeValue(ref pointBHandleForward);
            serializer.SerializeValue(ref pointBHandleBackward);
        }

        public TrackSectionNetworkData(TrackSection section)
        {
            sectionID = section.sectionID;
            nextSectionID = -1;
            if (section.nextSection != null) nextSectionID = section.nextSection.sectionID;
            previousSectionID = -1;
            if(section.previousSection != null) previousSectionID = section.previousSection.sectionID;

            pointAPos = section.pointA.position;
            pointAHandleForward = section.pointA.handleForward;
            
            pointBPos = section.pointB.position;
            pointBHandleBackward = section.pointB.handleBackward;
        }
    }
    private void Awake()
    {
        active = this;
        
    }

    private void Start()
    {
        NetworkManager.Singleton.OnConnectionEvent += OnConnection;
    }

    private void OnConnection(NetworkManager networkManager, ConnectionEventData connectionEventData)
    {
        if (IsServer && connectionEventData.ClientId != NetworkManager.Singleton.LocalClientId)
        {
            //Client connected. Send track data
            Debug.Log("Sending track data to client");
            foreach (TrackSection section in trackSectionList)
            {
                CreateTrackSectionFromNetworkDataRpc(new TrackSectionNetworkData(section),RpcTarget.Single(connectionEventData.ClientId, RpcTargetUse.Temp));
            }
        }
    }
    //Track Section Functions
    public TrackSection CreateTrackSection(Point pointA,Point pointB)
    {
        //Dose not automaticlly find next/previous sections

        Spline.DEBUG_DrawPointGizmos(pointA,60f);
        Spline.DEBUG_DrawPointGizmos(pointB,60f);
        
        CalculatePath(pointA,pointB,Spline.CalculateSplineLenght(pointA, pointB),out List<PathPoint> path);
        CalculatePath(path,out List<PathPoint> newPath,1,true, 0.0175f);
        //Debug.Log("OldPathLenght: " + path[^1].distance + " NewPathLenght: " + newPath[^1].distance);
        DEBUG_DrawPath(path);
        DEBUG_DrawPath(newPath);
        
        TrackSection section = new TrackSection(pointA, pointB, newPath);
        
        section.AddObject(Spline.active.GenerateMeshAlongTrackSection(section)); //Track mesh
        
        trackSectionList.Add(section);
        
        return section;
    }
    //Multiplayer
    [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
    private void CreateTrackSectionFromNetworkDataRpc(TrackSectionNetworkData networkData, RpcParams rpcParams = default)
    {
        Point pointA = Spline.CreatePoint(networkData.pointAPos,networkData.pointAHandleForward,true);
        Point pointB = Spline.CreatePoint(networkData.pointBPos,networkData.pointBHandleBackward,false);

        TrackSection section = CreateTrackSection(pointA, pointB);
        section.sectionID = networkData.sectionID;
        
        if(networkData.nextSectionID >= 0)
            section.SetNextSection(FindTrackSectionByID(networkData.nextSectionID));
        if(networkData.previousSectionID >= 0)
            section.SetPreviousSection(FindTrackSectionByID(networkData.nextSectionID));
    }
    
    public TrackSection FindTrackSectionByID(int sectionID)
    {
        for (int i = 0; i < trackSectionList.Count; i++)
        {
            if (trackSectionList[i].sectionID == sectionID)
                return trackSectionList[i];
        }

        return null;
    }
    //Functions with trackSectionList
    public void GetTrackPositionFromProgress(float sectionProgress, TrackSection section, out Vector3 position)
    {
        if (GetTrackSectionFromProgress(sectionProgress, section, out TrackSection newSection, out float newSectionProgress))
        {
            position = GetPathPosition(newSection.path, newSectionProgress);
        }
        else
        {
            Debug.LogWarning("End of track");
            position = GetPathPosition(newSection.path, newSectionProgress);
        }
    }
    public void GetTrackPositionAndDirVectorFromProgress(float sectionProgress, TrackSection section, out Vector3 position, out Vector3 dir)
    {
        if (GetTrackSectionFromProgress(sectionProgress, section, out TrackSection newSection, out float newSectionProgress))
        {
            position = GetPathPosition(newSection.path, newSectionProgress);
            dir = GetPathDirectionVector(newSection, newSectionProgress);
        }
        else
        {
            Debug.LogWarning("End of track");
            position = GetPathPosition(newSection.path, newSectionProgress);
            dir = GetPathDirectionVector(newSection, newSectionProgress);
        }
    }
    public bool GetTrackSectionFromProgress(float sectionProgress, TrackSection section, out TrackSection newSection, out float newSectionProgress)
    {
        newSection = section;
        newSectionProgress = sectionProgress;

        while (newSectionProgress > newSection.length)
        {    
            TrackSection nextSection = newSection.GetNextSection();
            if(nextSection != null)
            {
                newSectionProgress -= newSection.length;
                newSection = nextSection;
            }
            else
            {
                newSectionProgress = Mathf.Clamp(newSectionProgress, 0f, newSection.length);
                return false;
            }
        }

        while (newSectionProgress < 0)
        {
            TrackSection previousSection = newSection.GetPreviousSection();
            if (previousSection != null)
            {
                newSectionProgress = previousSection.length + newSectionProgress;
                newSection = previousSection;
            }
            else
            {
                newSectionProgress = Mathf.Clamp(newSectionProgress, 0f, newSection.length);
                return false;
            }
        }

        return true;
    }
    public bool GetNearestAutoStop(float sectionProgress, TrackSection trackSection, int maxTrackSectionCheck, out AutoStop nearestAutoStop, out float distanceToAutoStop, List<AutoStopType> filterList = null)
    {
        nearestAutoStop = null;
        distanceToAutoStop = 0f;

        for (int i = 0; i < maxTrackSectionCheck; i++)
        {
            if (trackSection == null)
                return false;

            if(i == 0)
            {
                if(trackSection.autoStop != null &&  trackSection.autoStop.ignore == false && trackSection.autoStop.distance >= sectionProgress && (filterList == null || filterList.Contains(trackSection.autoStop.stopType)))
                {
                    nearestAutoStop = trackSection.autoStop;
                    distanceToAutoStop = trackSection.autoStop.distance - sectionProgress;
                    return true;
                }
                else
                {
                    distanceToAutoStop = trackSection.length - sectionProgress;
                    trackSection = trackSection.GetNextSection(); 
                }
            }
            else
            {
                if (trackSection.autoStop != null && trackSection.autoStop.ignore == false && (filterList == null || filterList.Contains(trackSection.autoStop.stopType)))
                {
                    nearestAutoStop = trackSection.autoStop;
                    distanceToAutoStop += trackSection.autoStop.distance;
                    return true;
                }
                else
                {
                    distanceToAutoStop += trackSection.length;
                    trackSection = trackSection.GetNextSection();     
                }
            }
        }
        return false;
    }
    //public TrackSection RemoveAtIndexAndReturn(int index = 0)
    //{
    //    TrackSection section = trackSectionList[index];
    //    trackSectionList.RemoveAt(index);
    //    return section;
    //}
    public void DestroyTrackSection(TrackSection section)
    {
        trackSectionList.Remove(section);
        if (section.nextSection != null) section.nextSection.previousSection = null;
        if (section.previousSection != null) section.previousSection.nextSection = null;

        foreach (var obj in section.associatedObjects)
        {
            Destroy(obj);
        }
        if (section.pooledAssociatedObjects != null)
        {
            foreach (KeyValuePair<ObjectPool, List<GameObject>> poolObjectPair in section.pooledAssociatedObjects)
            {
                foreach (GameObject obj in poolObjectPair.Value)
                {
                    poolObjectPair.Key.Add(obj);
                }
            }
        }
    }
    //Get Position On Path
    public static Vector3 GetPathPosition(List<PathPoint> path, float progress)
    {
        for (int i = 1; i < path.Count; i++)
        {
            if (path[i].distance >= progress || i == path.Count - 1)
            {
                float time = (progress - path[i - 1].distance) / (path[i].distance - path[i - 1].distance);
                return Vector3.Lerp(path[i - 1].position, path[i].position, time);
            }
        }

        Debug.LogWarning("Couldn't Get Path Pos. " + path[^1].distance + " , " + progress);
        return Vector3.zero;
    }
    public static Vector3 GetPathDirectionVector(TrackSection section, float progress)
    {
        if (progress == 0f)
            return (section.pointA.handleForward - section.pointA.position).normalized;
        
        if (progress >= section.length - 0.001f)
            return (section.pointB.handleForward - section.pointB.position).normalized;

        for (int i = 1; i < section.path.Count; i++)
        {
            if (section.path[i].distance >= progress)
            {
                return (section.path[i].position -section.path[i - 1].position).normalized;
            }
        }
        Debug.LogWarning("Couldn't Get Path Direction Vector." + section + ", " + progress);
        return Vector3.zero;
    }
    public static Vector3 GetPathDirectionVector(List<PathPoint> path, float progress)
    {
        //Handle out of range progress
        progress = Mathf.Clamp(progress, 0f, path[^1].distance);

        for (int i = 1; i < path.Count; i++)
        {
            if (path[i].distance >= progress)
            {
                Vector3 dir = (path[i].position - path[i - 1].position).normalized;

                //sometimes some ammount of pathpoints have the same pos. this is a crappy fix
                if (dir == Vector3.zero)
                {
                    int count = 2;
                    while(dir == Vector3.zero && i - count >= 0)
                    {
                        dir = (path[i].position - path[i - count].position).normalized;
                        count++;
                    }
                }
                return dir;
            }
        }

        Debug.LogWarning("Couldn't Get Path Direction Vector. " + progress);
        return Vector3.zero;
    }
    //Generating Path Functions
    static public void CalculatePath(Point pointA, Point pointB, float splineLenght, out List<PathPoint> path, float resolution = 2.5f)
    {
        path = new List<PathPoint>();

        float increment = 1f / (float)Mathf.RoundToInt(splineLenght * resolution);

        Vector3 lastPos = pointA.position;
        float pathLenght = 0;

        float time = 0f;
        while (time <= 1f)
        {
            Vector3 pos = Spline.CalculateSplinePosition(pointA, pointB, time);
            pathLenght += Vector3.Distance(lastPos, pos);
            
            PathPoint point = new PathPoint(pos,pathLenght);
            path.Add(point);

            lastPos = pos;
            
            //Making sure the last point Gets Generated
            if (time < 1f && time + increment > 1f)
                time = 1f;
            else
                time += increment;
        }

        //Debug.Log("PointDist: " + pathLenght + " SplineDist: " + splineLenght);
    }
    static public void CalculatePath(List<PathPoint> path, out List<PathPoint> newPath, float resolution = 1f, bool recalculateDistance = false, float randomOffset = 0f)
    {
        newPath = new List<PathPoint>();

        if (recalculateDistance)
        {
            path[0].distance = 0;
            for (int i = 1; i < path.Count; i++)
            {
                path[i].distance = path[i - 1].distance + Vector3.Distance(path[i].position, path[i - 1].position);
            }
        }

        //New thing I learned: ^1 = path.Count-1
        float pathLenght = path[^1].distance;

        float increment = pathLenght / (float)Mathf.RoundToInt(pathLenght * resolution);

        Vector3 lastPos = path[0].position;
        float newPathLenght = 0;

        float progress = 0f;
        while (progress <= pathLenght)
        {
            Vector3 pos = GetPathPosition(path, progress);

            if (randomOffset != 0f && progress > 0.05f && progress < pathLenght - 0.05f)
            {
                pos += new Vector3(Random.Range(-randomOffset, randomOffset), Random.Range(-randomOffset, randomOffset) * 0.5f, Random.Range(-randomOffset, randomOffset));
            }
            
            newPathLenght += Vector3.Distance(lastPos, pos);
            
            PathPoint point = new PathPoint(pos,newPathLenght);
            newPath.Add(point);

            lastPos = pos;
            
            //Making sure the last point Gets Generated
            if (progress < pathLenght && progress + increment > pathLenght)
                progress = pathLenght;
            else
                progress += increment;
        }

        //Debug.Log("OldPathLenght: " + pathLenght + " NewPathLenght: " + newPathLenght);
    }
    
    public void GetClosestTrackSection(Vector3 pos, out TrackSection trackSection, out float distance)
    {
        Vector2 pos2D = new Vector2(pos.x, pos.z);
        trackSection = null;
        distance = float.MaxValue;

        foreach (TrackSection section in trackSectionList)
        {
            Vector3 middlePoint = section.path[section.path.Count / 2].position;
            float dist = Vector2.Distance(pos2D, new Vector2(middlePoint.x, middlePoint.z));
            if(dist < distance)
            {
                trackSection = section;
                distance = dist;
            }
        }
    }
    public float GetDistanceFromPath(List<PathPoint> path, Vector3 pos)
    {
        float distance = float.MaxValue;

        int failCount = 0;
        for (int i = 0; i < path.Count; i++)
        {
            float dist = Vector2.Distance(new Vector2(path[i].position.x, path[i].position.z), new Vector2(pos.x, pos.z));
            if(dist < distance)
            {
                distance = dist;
            }
            else
            {
                failCount++;
                if(failCount > 3)
                {
                    //Distance is growing
                    Debug.DrawLine(path[i].position, pos);
                    break;
                }
            }
        }
        return distance;
    }
    public float GetDistanceFromPath(List<PathPoint> path, Vector3 pos, out PathPoint pathPoint, bool getPreciseDistance = false)
    {
        float distance = float.MaxValue;

        int failCount = 0;
        pathPoint = null;
        for (int i = 0; i < path.Count; i++)
        {
            float dist = Vector2.Distance(new Vector2(path[i].position.x, path[i].position.z), new Vector2(pos.x, pos.z));
            if (dist < distance)
            {
                distance = dist;
                pathPoint = path[i];
            }
            else if(getPreciseDistance == false)
            {
                failCount++;
                if (failCount > 3)
                {
                    //Distance is growing
                    Debug.DrawLine(path[i].position, pos);
                    break;
                }
            }
        }
        return distance;
    }
    public float GetDistanceFromTrack(Vector3 pos)
    {
        GetClosestTrackSection(pos, out TrackSection section, out float estimatedDist);
        return GetDistanceFromPath(section.path, pos);
    }
    public bool IsLeftOfPath(List<PathPoint> path, Vector3 pos)
    {
        float distance = float.MaxValue;

        int failCount = 0;
        int index = 0;
        for (int i = 0; i < path.Count; i++)
        {
            float dist = Vector3.Distance(path[i].position, pos);
            if (dist < distance)
            {
                distance = dist;
                index = i;
            }
            else
            {
                failCount++;
                if (failCount > 3)
                {
                    //Distance is growing
                    Debug.DrawLine(path[i].position, pos);
                    break;
                }
            }
        }
        //Debug.Log(pos.x + ", " + path[index].position.x);
        return pos.x < path[index].position.x;
    }
    //Debug Functions
    private void DEBUG_DrawPath(List<PathPoint> path, float duration = 60f)
    {
        Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        for (int i = 0; i < path.Count; i++)
        {
            Debug.DrawRay(path[i].position,Vector3.up *1.5f + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f)),color,duration);
        }
    }
}
