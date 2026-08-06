using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static Util;
using Random = UnityEngine.Random;

public class GenerationManager : NetworkBehaviour
{
    public static GenerationManager active;

    //Variables
    [Header("Scripts")]
    [SerializeField]
    private TerrainGeneration terrainGeneration;
    [SerializeField]
    private TrackGeneration trackGeneration;
    [Header("Player Train")]
    public Train playerTrain;
    [Header("Starting Location")]
    [SerializeField]
    private StructureSO startingLocationSO;
    [SerializeField]
    private float startingLocationStructureTrackLength;
    [Header("Generation")] 
    public float waterHeight;
    [Header("Stations")]
    [SerializeField]
    private AnimationCurve stationSpawnDistance;
    [SerializeField]
    private float stationSpeedTrackDistance = 200f;
    [SerializeField]
    private StructureSO stationSO;
    [SerializeField]
    private StructureSO preStationSO;

    public enum StructureDistanceType
    {
        setDistance,
        fuelDistance
    }
    [System.Serializable]
    public class StructureSpawnParams
    {
        public List<StructureSO> structureInfoList;
        public StructureDistanceType distanceType;
        public Vector2 distanceTypeValue; // setDistance = distance; fuelDistance = fuelDistance * value
        public bool repeating;
        public float minStationDistance;
        public void ResetDistance(bool resetSpawningDisabled = false)
        {
            if (resetSpawningDisabled || repeating) spawningDisabled = false;

            if (spawningDisabled == false)
            {
                if (distanceType == StructureDistanceType.setDistance)
                {
                    distanceTillStructure = Random.Range(distanceTypeValue.x, distanceTypeValue.y);
                }
                else if (distanceType == StructureDistanceType.fuelDistance)
                {
                    float minDistance = float.MaxValue;
                    foreach (Train.FuelSource fuelSource in Train.playerTrain.GetFuelSourceList())
                    {
                        minDistance = Mathf.Min(fuelSource.targetHealth.maxHealth / TrainUpgradeHandler.active.GetStatValue(fuelSource.consumptionRate), minDistance);
                    }
                    distanceTillStructure = minDistance * Random.Range(distanceTypeValue.x, distanceTypeValue.y);
                }
            }
        }
        //Run time
        //[HideInInspector]
        public float distanceTillStructure;
        //[HideInInspector]
        public bool spawningDisabled;
    }
    [Header("Structures")]
    [SerializeField]
    private List<StructureSpawnParams> strutureSpawnParamList;
    [SerializeField]
    private float distanceBufferBetweenStructures;
    private enum DebugTrainStartLocation
    {
        TrainRide,
        StartingLocation,
        Station
    }

    [Header("Debug")] 
    [SerializeField] 
    private DebugTrainStartLocation trainStartLocation;
    [Header("Temp")]
    [SerializeField]
    private bool disableNavMeshGen;
    //Run Time
    private float currentGeneratedDistance = 0f;
    private float lastAngle;
    private Point lastPoint;
    private TrackSection lastSection;
    private Vector3 currentStationPos;
    //Station stuff
    private bool stationGenerated;
    private float distanceLeftTillNextStation;
    //structures
    private float currentStructureDistanceBuffer;

    private List<PathPoint> generatedPath;
    
    //Navigation
    private Vector4 navigationMeshBoundsVector;

    private void Start()
    {
        active = this;
    }
    public override void OnNetworkSpawn()
    {
        if(isActiveAndEnabled == false)
            return;
        //Debug.Log("Is Server: " + IsServer);
        WaitForScripts();
    }

    private async void WaitForScripts()
    {
        try
        {
            while (true)
            {
                if (TerrainGeneration.terrainNoise != null)
                {
                    if(IsServer)
                        GenerateStart();
                    else
                        terrainGeneration.SetUpdateRenderDistance(true);
                    return;
                }
                await Task.Delay(50);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    //Generation functions
    public void GenerateTillNextStation()
    {
        if(IsServer == false)
            return;
        
        LoadingScreen.active.Enable("Generating");
        
        navigationMeshBoundsVector = Vector4.zero;
        
        if (GameStateManager.currentHaulingJob == null)
        {
            GenerateStartRpc(false);
            FindPathsRequest(9, 3, 0.4f, OnFindPaths);
        }
        else
        {
            GenerateStartRpc(true);
            DestroySectionsBehindTrain();
            //Generate
            OnFindPaths(GameStateManager.currentHaulingJob.GetLinkedTrack());
        }
    }
    [Rpc(SendTo.NotServer)]
    private void GenerateStartRpc(bool destroySectionsBehind)
    {
        LoadingScreen.active.Enable("Generating");
        LoadingScreen.active.SetProgress(0, "Waiting For Host");

        if(destroySectionsBehind) DestroySectionsBehindTrain();
    }

    private void DestroySectionsBehindTrain()
    {
        //Destroy sections behind
        TrackSection section = playerTrain.frontTrackSection.previousSection.previousSection;
        while (section != null)
        {
            TrackSection refrence = section;
            section = section.previousSection;

            TrackManager.active.DestroyTrackSection(refrence);
        }
    }
    public void FindPathsRequest(int searchCount, int returnCount, float loadingBarTime, Action<object> callBack)
    {
        List<int> pathSeeds = new List<int>();
        for (int i = 0; i < searchCount; i++)
        {
            pathSeeds.Add(Random.Range(0, 999999));
        }
        ThreadManager.AddThreadJob(() => FindPathThread(pathSeeds, returnCount, loadingBarTime), callBack);
    }
    public object FindPathThread(List<int> pathSeeds, int returnCount, float loadingBarTime)
    {
        Vector3 targetPos;
        lock (stationSpawnDistance)
        {
            targetPos = lastPoint.position + new Vector3(0, 0, stationSpawnDistance.Evaluate(GameStateManager.currentLevel.Value));
        }
        TerrainGeneration.Chunk chunk = terrainGeneration.FindFittingChunk(0.5f, 0.4f, 3f, -2f, terrainGeneration.GetChunkCoord(targetPos), 2);
        List<NodePath> nodePathList = trackGeneration.FindPaths(lastPoint.position, chunk.GetWorldPos(0.5f, 0.5f), pathSeeds, returnCount, loadingBarTime);
        return nodePathList;
    }
    public void OnFindPaths(object pathObj)
    {
        LoadingScreen.active.SetProgress(0.5f, "Generating Track");

        NodePath nodepath = null;
        if (pathObj is List<NodePath>)
        {
            nodepath = ((List<NodePath>)pathObj)[0];
            for (int i = 0; i < ((List<NodePath>)pathObj).Count; i++)
            {
                Debug.Log("Ordered path length: " + ((List<NodePath>)pathObj)[i]);
            }
        }
        else
            nodepath = (NodePath)pathObj;
        
        //Add nodePath positions to navigation gen
        foreach (Node node in nodepath.path)
        {
            AddWorldPosToNavGen(node.worldPos);
        }
        
        trackGeneration.CreateTrackAlongNodes(nodepath, out List<PathPoint> path, out TrackSection lastTrackSectionOut, lastPoint,null, lastSection);

        currentStationPos = lastTrackSectionOut.path[^1].position;

        //Structures
        foreach (StructureSpawnParams structureSpawnParams in strutureSpawnParamList)
        {
            structureSpawnParams.ResetDistance(true);
        }
        while (lastSection != lastTrackSectionOut)
        {
            lastSection = lastSection.nextSection;
            UpdateStructureDistances(lastSection);
        }

        //lastSection = lastTrackSectionOut;
        lastPoint = lastSection.pointB;
        generatedPath = path;
        GenerateStation();
        StartCoroutine(WaitForStructuresCoroutine());
    }
    public IEnumerator WaitForStructuresCoroutine()
    {
        while (StructureMaster.generatingStructures != StructureMaster.finnishedStructures)
        {
            LoadingScreen.active.SetProgress(0.55f + (StructureMaster.finnishedStructures / (float)StructureMaster.generatingStructures) * 0.25f, $"Generating Structures {StructureMaster.finnishedStructures}/{StructureMaster.generatingStructures}");
            yield return null;
        }

        LoadingScreen.active.SetProgress(0.9f, "Modifying Ground");
        ThreadManager.AddThreadJob(delegate { trackGeneration.ModifyTerrainToFollowPath(generatedPath, true); }, delegate { OnFinishedModifyingGround(); });
        yield break;
    }
    public void OnFinishedModifyingGround()
    {
        LoadingScreen.active.SetProgress(0.9f, "Generating Navigation Mesh");
        ThreadManager.AddMainThreadJob(delegate { GenerateNavMesh();});
    }

    public void AddWorldPosToNavGen(Vector3 worldPos)
    {
        Vector2Int chunk = terrainGeneration.GetChunkCoord(worldPos);
        if (navigationMeshBoundsVector == Vector4.zero)
        {
            navigationMeshBoundsVector.x = chunk.x;
            navigationMeshBoundsVector.y = chunk.y;
            navigationMeshBoundsVector.z = chunk.x;
            navigationMeshBoundsVector.w = chunk.y;
        }
        else
        {
            if (chunk.x < navigationMeshBoundsVector.x)
            {
                navigationMeshBoundsVector.x = chunk.x;
            }
            if (chunk.y < navigationMeshBoundsVector.x)
            {
                navigationMeshBoundsVector.y = chunk.y;
            }
            if (chunk.x > navigationMeshBoundsVector.z)
            {
                navigationMeshBoundsVector.z = chunk.x;
            }
            if (chunk.y > navigationMeshBoundsVector.w)
            {
                navigationMeshBoundsVector.w = chunk.y;
            }
        }
    }
    public void GenerateNavMesh()
    {
        navigationMeshBoundsVector.x += -2;
        navigationMeshBoundsVector.y += -2;
        navigationMeshBoundsVector.z += 2;
        navigationMeshBoundsVector.w += 2;

        float xSize = navigationMeshBoundsVector.z - navigationMeshBoundsVector.x;
        float ySize = navigationMeshBoundsVector.w - navigationMeshBoundsVector.y;
        float xCenter = (navigationMeshBoundsVector.x + navigationMeshBoundsVector.z) * 0.5f;
        float yCenter = (navigationMeshBoundsVector.y + navigationMeshBoundsVector.w) * 0.5f;
        
        //Load in chunks
        List<Vector2Int> chunkCoordList = new List<Vector2Int>();
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                Vector2Int chunkCoord = new Vector2Int(Mathf.RoundToInt(x + navigationMeshBoundsVector.x), Mathf.RoundToInt(y + navigationMeshBoundsVector.y));
                //Non Threaded Generation
                terrainGeneration.CreateOrUpdateChunk(chunkCoord, false, true, false, false);
                chunkCoordList.Add(chunkCoord);
            }
        }
        
        //Delete last navmesh
        Transform previousNavMesh = transform.Find("NavMesh");
        if(previousNavMesh != null) Destroy(previousNavMesh.gameObject);
        
        //Create NavMesh
        NavMeshSurface navMeshSurface = new GameObject("NavMesh").AddComponent<NavMeshSurface>(); 
        navMeshSurface.transform.SetParent(transform);
        navMeshSurface.transform.position = Vector3.zero;

        navMeshSurface.size = new Vector3(xSize * TerrainGeneration.active.chunkSize, 40f, ySize * TerrainGeneration.active.chunkSize);
        navMeshSurface.center = new Vector3(xCenter * TerrainGeneration.active.chunkSize, 10f, yCenter * TerrainGeneration.active.chunkSize);
        
        navMeshSurface.agentTypeID = NavMesh.GetSettingsByIndex(1).agentTypeID;//enemy

        navMeshSurface.collectObjects = CollectObjects.Volume;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navMeshSurface.layerMask = TerrainGeneration.active.navMeshLayerMask;
        navMeshSurface.defaultArea = NavMesh.GetAreaFromName("Walkable");

        navMeshSurface.overrideTileSize = true;
        navMeshSurface.tileSize = 64;

        navMeshSurface.overrideVoxelSize = true;
        navMeshSurface.voxelSize = 0.15f;
        
        //Dont generate nav in water 
        NavMeshModifierVolume modifierVolume = navMeshSurface.gameObject.AddComponent<NavMeshModifierVolume>();
        modifierVolume.size = new Vector3(navMeshSurface.size.x, 10f, navMeshSurface.size.z);
        modifierVolume.center = new Vector3(navMeshSurface.center.x, -5f + waterHeight, navMeshSurface.center.z);

        modifierVolume.area = NavMesh.GetAreaFromName("Water");
        modifierVolume.AffectsAgentType(navMeshSurface.agentTypeID); //Enemy
        
        StartCoroutine(WaitForChunkGen(navMeshSurface,chunkCoordList));
    }

    private IEnumerator WaitForChunkGen(NavMeshSurface navMeshSurface, List<Vector2Int> chunkCoordList)
    {
        LoadingScreen.active.SetProgress(0.95f, "Generating Navigation Mesh");
        while (true)
        {
            bool finishedGenerating = true;
            foreach (Vector2Int chunkCoord in chunkCoordList)
            {
                //Threaded Generation
                if (terrainGeneration.HasChunkGenerated(chunkCoord) == false)
                {
                    finishedGenerating = false;
                    break;
                }
            }
            if(finishedGenerating)
                break;
            Debug.Log("Waiting For Chunks!");
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSecondsRealtime(1f);
        
        //Generate
        navMeshSurface.BuildNavMesh();
        
        WaitForClients();
    }

    private int clientsFinishedGenerating = 0;
    private async void WaitForClients()
    {
        try
        {
            ServerFinishedGeneratingRpc();
            LoadingScreen.active.SetProgress(1f, "Waiting For Clients");
            while (clientsFinishedGenerating < NetworkManager.Singleton.ConnectedClients.Count-1)
            {
                LoadingScreen.active.SetProgress(1f, $"Waiting For Clients {clientsFinishedGenerating}/{NetworkManager.Singleton.ConnectedClients.Count-1}");
                await Task.Delay(50);
            }
            
            GameStateManager.gameStarted.Value = true;
            FinishGenerationRpc();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
    [Rpc(SendTo.NotServer)]
    private void ServerFinishedGeneratingRpc()
    {
        ClientFinishedGeneratingRpc();
        //Move this to when the client is waiting for structures to finish generating
        TrackSection section = Train.playerTrain.frontTrackSection;
        List<PathPoint> generatedPath = new List<PathPoint>();

        while (section.nextSection != null)
        {
            generatedPath.AddRange(section.nextSection.path);
            section = section.nextSection;
        }
        LoadingScreen.active.SetProgress(0.9f, "Modifying Ground");
        ThreadManager.AddThreadJob(delegate { trackGeneration.ModifyTerrainToFollowPath(generatedPath, true); }, delegate { ClientFinishedGeneratingRpc(); });
    }
    [Rpc(SendTo.Server)]
    private void ClientFinishedGeneratingRpc()
    {
        clientsFinishedGenerating++;
    }
    [Rpc(SendTo.Everyone)]
    public void FinishGenerationRpc()
    {
        EnemySpawner.ResetWaveValues(5f);
        terrainGeneration.SetUpdateRenderDistance(true);
        LoadingScreen.active.Disable();
    }

    //Generation functions end
    private void UpdateStructureDistances(TrackSection generatedSection)
    {
        currentStructureDistanceBuffer -= generatedSection.length;

        foreach (StructureSpawnParams structureSpawnParams in strutureSpawnParamList)
        {
            if (structureSpawnParams.spawningDisabled) continue;

            structureSpawnParams.distanceTillStructure -= generatedSection.length;
            
            if(currentStructureDistanceBuffer <= 0f && structureSpawnParams.distanceTillStructure <= 0f)
            {
                float sectionProgress = Mathf.Clamp01(Mathf.Min(-currentStructureDistanceBuffer, -structureSpawnParams.distanceTillStructure) / generatedSection.length) * generatedSection.length;

                Vector3 pos = TrackManager.GetPathPosition(generatedSection.path,sectionProgress);
                if (Vector2.Distance(new Vector2(currentStationPos.x, currentStationPos.z),new Vector2(pos.x, pos.z)) >= structureSpawnParams.minStationDistance)
                {
                    SpawnStructureNearTrack(sectionProgress, generatedSection, structureSpawnParams.structureInfoList[Random.Range(0, structureSpawnParams.structureInfoList.Count)]);
                    currentStructureDistanceBuffer = distanceBufferBetweenStructures;

                    structureSpawnParams.spawningDisabled = true;
                    structureSpawnParams.ResetDistance();
                }
                else
                {
                    Debug.Log("Did not spawn structure becasue to close to station");
                }
            }
        }
    }
    private void SpawnStructureNearTrack(float sectionProgress, TrackSection section, StructureSO structureInfo)
    {
        //Ran only on server
        TrackManager.active.GetTrackPositionAndDirVectorFromProgress(sectionProgress, section, out Vector3 position, out Vector3 dir);

        GameObject structure = Instantiate(structureInfo.structurePrefab);
        
        structure.transform.position = position;
        structure.transform.LookAt(position + (structureInfo.onlyRotateY ? new Vector3(dir.x, 0, dir.z) : dir));
        
        section.AddObject(structure); //Adds it for deletion when track section is deleted

        //Initialize structure generation
        if(structure.TryGetComponent(out StructureMaster structureMaster))
        {
            structureMaster.Generate(sectionProgress, section, structureInfo);
        }
    }
    private void GenerateStart()
    {
        //Ran only on server
        
        //GameStateManager.isStartingLocationSpawned = true;
        GameStateManager.currentLevel.Value = 0;

        TerrainGeneration.Chunk chunk = terrainGeneration.FindFittingChunk(0.5f, 0.6f, 3f, -2f, Vector2Int.zero, 5);

        if (chunk == null)
        {
            chunk = terrainGeneration.CreateOrGetChunk(Vector2Int.zero);
        }
        
        Vector3 pos = chunk.GetWorldPos(0.5f, 1f);
        if (pos.y < 0f)
            pos.y = 0f;

        Point pointA = Spline.CreatePoint(pos - Vector3.forward * startingLocationStructureTrackLength, pos - Vector3.forward * startingLocationStructureTrackLength * 0.5f);
        Point pointB = Spline.CreatePoint(pos, pos + Vector3.forward * 15f);

        TrackSection section = TrackManager.active.CreateTrackSection(pointA, pointB);
        currentGeneratedDistance += section.length;

        SpawnStructureNearTrack(section.length, section, startingLocationSO);
        
        section.SetAutoStop(playerTrain.GetConsistLenght() + 1.2f, AutoStopType.StartingLocation);
        
        
        lastPoint = pointB;
        lastSection = section;

        //Track to starting location
        trackGeneration.GenerateTrack(section.pointA.position + Vector3.back * 150f, section.pointA.position, out TrackSection lastTrackSectionOut, null, section.pointA);
        lastTrackSectionOut.SetNextSection(section);
        
        //playerTrain.Initialize(playerTrain.GetConsistLenght() + 1.2f, section,false);
        
        TrackSection findLastSection = lastTrackSectionOut;
        while (findLastSection.previousSection != null)
            findLastSection = findLastSection.previousSection;

        if (trainStartLocation == DebugTrainStartLocation.TrainRide)
        {
            playerTrain.Initialize(playerTrain.GetConsistLenght() + 1f, findLastSection, true);
        }
        else if (trainStartLocation == DebugTrainStartLocation.StartingLocation ||
                 trainStartLocation == DebugTrainStartLocation.Station)
        {
            playerTrain.Initialize(playerTrain.GetConsistLenght(), section, true);
        }
        
        terrainGeneration.SetUpdateRenderDistance(true);
        
        Spline.DEBUG_DrawPointGizmos(pointA);
        Spline.DEBUG_DrawPointGizmos(pointB);
        
    }
    private void GenerateStation()
    {
        GameStateManager.currentLevel.Value++;

        List<PathPoint> ListOfPathPoints = new List<PathPoint>();

        //MOVED TO HaulinJobMonitorHandler.cs
        //Generate new hauling jobs
        //HaulingJobManager.active.GenerateNewHaulingJobList(3);
        
        List<TrackSection> generatedSectionList = new List<TrackSection>();
        //Pre Station Track
        TrackSection generatedSection = GenerateStraightSection(playerTrain.GetConsistLenght() + 12.5f);
        generatedSectionList.Add(generatedSection);
        SpawnStructureNearTrack(generatedSection.length, generatedSection, preStationSO);
        //generatedSection.SetAutoStop(generatedSection.length - 2.5f,AutoStopType.Front);

        //Speed Track Enter
        generatedSection = GenerateStraightSection(stationSpeedTrackDistance);
        generatedSectionList.Add(generatedSection);
        ListOfPathPoints.AddRange(generatedSection.path);

        //Station Track
        float startionTrackLength = playerTrain.GetConsistLenght() + 10f;
        generatedSection = GenerateStraightSection(startionTrackLength,startionTrackLength * 0.4f);
        generatedSectionList.Add(generatedSection);
        ListOfPathPoints.AddRange(generatedSection.path);
        SpawnStructureNearTrack(generatedSection.length - 2, generatedSection, stationSO);
        Vector3 middlePos = TrackManager.GetPathPosition(generatedSection.path, generatedSection.length * 0.5f);

        if (trainStartLocation == DebugTrainStartLocation.Station)
        {
            playerTrain.Initialize(generatedSection.length - 3f, generatedSection,false);
        }
        
        //generatedSection.SetAutoStop(generatedSection.length - 5f, AutoStopType.Front);

        //Speed Track Exit
        //Try not to build into a mountain by checking for height differences in a circle
        float radius = stationSpeedTrackDistance + startionTrackLength * 0.5f;
        float angleRange = 45f;
        float maxHeightDifference = float.MaxValue;
        Vector3 forward = (lastPoint.handleForward - lastPoint.position).normalized;
        Vector3 bestPosition = middlePos + forward * radius;
        Vector3 bestPositionDir = forward;
        for (int i = 0; i < 60; i++)
        {
            float angle = angleRange * (i / 60f) * (i % 2 == 0 ? 1f : -1f);
            Vector3 modifiedForward = Quaternion.AngleAxis(angle, Vector3.up) * forward;

            Vector3 worldPos = middlePos + modifiedForward * radius;
            Debug.DrawRay(worldPos,Vector3.up * 25f,new Color((i / 60f),(i / 60f),(i / 60f)),60f);

            float heightDiff = MathF.Abs(terrainGeneration.GetHeight(worldPos) - worldPos.y);
            if (heightDiff < maxHeightDifference)
            {
                maxHeightDifference = heightDiff;
                bestPosition = worldPos;
                bestPositionDir = modifiedForward;
            }
            if(heightDiff < 2.5f)
                break;
        }

        Point point = Spline.CreatePoint(bestPosition, bestPosition - bestPositionDir * (radius * 0.3f), false);
        point.handleForward = bestPosition + bestPositionDir * 15f;
        generatedSection = TrackManager.active.CreateTrackSection(lastPoint,point);
        currentGeneratedDistance += generatedSection.length;

        lastSection.SetNextSection(generatedSection);
        generatedSectionList.Add(generatedSection);
        lastSection = generatedSection;
        lastPoint = point;
        
        ListOfPathPoints.AddRange(generatedSection.path);
        generatedSection.SetAutoStop(generatedSection.length - 1f, AutoStopType.SlowDown);

        //Modify Ground
        ModifyGroundRpc(new Vector3(middlePos.x, middlePos.y - 100f, middlePos.z), startionTrackLength * 0.5f + stationSpeedTrackDistance);
        
        ModifyGroundRpc(new Vector3(middlePos.x, middlePos.y, middlePos.z), startionTrackLength * 0.5f + stationSpeedTrackDistance * 0.5f);

        ThreadManager.AddThreadJob(delegate { trackGeneration.ModifyTerrainToFollowPath(ListOfPathPoints, true); }, delegate {  });


        for (int i = 0; i < generatedSectionList.Count; i++)
        {
            TrackManager.active.SyncTrackSection(generatedSectionList[i],true);
        }
        
        stationGenerated = true;
    }
    [Rpc(SendTo.Everyone)]
    private void ModifyGroundRpc(Vector3 worldPos, float influenceDistance)
    {
        AnimationCurve newAnimationCurve = new AnimationCurve();
        newAnimationCurve.keys = new Keyframe[] { new Keyframe(0f, 1f), new Keyframe(influenceDistance - 30f, 1f), new Keyframe(influenceDistance, 0f) };
        TerrainModifier.ModifyGround(worldPos, influenceDistance, newAnimationCurve);
    }
    private TrackSection GenerateStraightSection(float distance, float handleLength = 10f)
    {
        Vector3 forward = (lastPoint.handleForward - lastPoint.position).normalized;
        Point nextPoint = Spline.CreatePoint(lastPoint.position + forward * (distance), lastPoint.position + forward * (distance + handleLength), true);

        Spline.DEBUG_DrawPointGizmos(nextPoint);

        TrackSection generatedSection = TrackManager.active.CreateTrackSection(lastPoint, nextPoint);
        currentGeneratedDistance += generatedSection.length;

        lastSection.SetNextSection(generatedSection);
        lastSection = generatedSection;
        lastPoint = nextPoint;

        return generatedSection;
    }
    
}
