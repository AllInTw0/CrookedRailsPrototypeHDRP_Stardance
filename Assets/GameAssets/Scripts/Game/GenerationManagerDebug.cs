using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class GenerationManagerDebug : NetworkBehaviour
{
    
    [Header("Scripts")]
    [SerializeField]
    private TerrainGeneration terrainGeneration;
    [SerializeField]
    private TrackGeneration trackGeneration;
    public float waterHeight = -0.75f;
    
    private Point lastPoint;
    private TrackSection lastSection;
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
    
    private void GenerateStart()
    {
        GameStateManager.currentLevel.Value = 0;

        TerrainGeneration.Chunk chunk = terrainGeneration.FindFittingChunk(0.5f, 0.6f, 3f, -2f, Vector2Int.zero, 5);

        if (chunk == null)
        {
            chunk = terrainGeneration.CreateOrGetChunk(Vector2Int.zero);
        }
        
        Vector3 pos = chunk.GetWorldPos(0.5f, 1f);
        if (pos.y < 0f)
            pos.y = 0f;
        
        trackGeneration.GenerateTrack(pos, pos + Vector3.forward * 250f, out TrackSection lastTrackSectionOut, null, null);
        
        TrackSection findLastSection = lastTrackSectionOut;
        while (findLastSection.previousSection != null)
            findLastSection = findLastSection.previousSection;
        
        PlayerTrain.active.Initialize(PlayerTrain.active.GetConsistLenght() + 1f, findLastSection, false);
        
        terrainGeneration.SetUpdateRenderDistance(true);

        GenerateNavMeshClient(new Vector4(chunk.coord.x - 3, chunk.coord.y - 3, chunk.coord.x + 3, chunk.coord.y + 9));
    }
    
    private async Task GenerateNavMeshClient(Vector4 boundsVector)
    {
        try
        {
            Debug.Log("Generating NavMesh");
            float xSize = boundsVector.z - boundsVector.x;
            float ySize = boundsVector.w - boundsVector.y;
            float xCenter = (boundsVector.x + boundsVector.z) * 0.5f;
            float yCenter = (boundsVector.y + boundsVector.w) * 0.5f;
            
            //Load in chunks
            List<Vector2Int> chunkCoordList = new List<Vector2Int>();
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    Vector2Int chunkCoord = new Vector2Int(Mathf.RoundToInt(x + boundsVector.x), Mathf.RoundToInt(y + boundsVector.y));
                    terrainGeneration.CreateOrUpdateChunk(chunkCoord, false, true, false, false);
                    chunkCoordList.Add(chunkCoord);
                }
            }
            
            //Wait for chunks
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
                await Task.Delay(100);
            }

            await Task.Delay(1000);
            
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
            
            //Generate
            navMeshSurface.BuildNavMesh();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}
