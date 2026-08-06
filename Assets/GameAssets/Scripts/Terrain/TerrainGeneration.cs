using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static TerrainGeneration;
using static Util;

public class TerrainGeneration : NetworkBehaviour
{
    public static TerrainGeneration active;

    [Header("Size params")]
    [SerializeField]
    public int chunkSize;
    [SerializeField]
    private int chunkVertBorderCount; 
    private float vertSpacing;

    [Header("Material")]
    [SerializeField]
    public Material terrainMaterial;

    [Header("Terrain Params")]
    [SerializeField]
    private List<NoiseSettings> noiseSettingsList;
    [SerializeField]
    public int terrainLayer;
    [SerializeField]
    public RenderingLayerMask terrainRenderingLayerMask;
    [System.Serializable]
    private class FoliageData
    {
        public ObjectPool objectPool;
        public Vector2 minMaxTerrainHeight;
        public Vector2 heightClamp;
        public float minNormalY;
    }
    [Header("Foliage")]
    [SerializeField]
    private List<NoiseSettings> foliageNoiseSettingsList;
    [SerializeField]
    private List<ProbabilityListElement<FoliageData>> foliageProbabilityElementList = new List<ProbabilityListElement<FoliageData>>();
    [SerializeField]
    private int foliageDensity;
    [SerializeField]
    private AnimationCurve foliageSpawnProbability_DistanceFromTrack;
    [SerializeField]
    private LayerMask foliageRaycastLayerMask;
    [SerializeField]
    private LayerMask foliageSpawnLayerMask;
    
    private Vector2 foliageNoiseOffset;
    private ProbabilityList<FoliageData> foliageProbabilityList;
    [Header("Render Distance")]
    [SerializeField]
    private float renderDistanceMeters;
    [SerializeField]
    private float foliageRenderDistanceMeters;
    private bool updateRenderDistance = false;
    private Vector2Int lastUpdateCoord;
    [Header("Navigation")]
    public LayerMask navMeshLayerMask;
    [Header("Seed")]
    [SerializeField]
    private int seed;
    [SerializeField]
    private bool randomSeed;

    private NetworkVariable<int> networkSeed = new (writePerm: NetworkVariableWritePermission.Server);
    
    [Header("Editor Previews")]
    [SerializeField]
    public bool autoUpdate;
    //Runtime
    private Dictionary<Vector2Int, Chunk> chunkDictionary = new Dictionary<Vector2Int, Chunk>();

    public static Noise terrainNoise;
    public static Noise foliageNoise;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (randomSeed)
                seed = Random.Range(0, 999999999);
            networkSeed.Value = seed;
        }
        SetValues();
    }
    private void Update()
    {
        UpdateRenderDistance(false, true, false, true);
    }
    private void SetValues()
    {
        active = this;
        vertSpacing = (float)chunkSize / (float)chunkVertBorderCount;
        lastUpdateCoord = new Vector2Int(-1000000, -1000000);

        foreach (ProbabilityListElement<FoliageData> entry in foliageProbabilityElementList)
        {
            entry.element.objectPool.Init(entry.element.objectPool.maxCapacity, entry.element.objectPool.defaultCapacity);
        }
        foliageProbabilityList = new ProbabilityList<FoliageData>(foliageProbabilityElementList);

        chunkDictionary = new Dictionary<Vector2Int, Chunk>();
        
        System.Random rng = new System.Random(networkSeed.Value);
        foliageNoiseOffset = new Vector2(rng.Next(-10000,10000), rng.Next(-10000,10000));
        
        terrainNoise = new Noise(networkSeed.Value, noiseSettingsList);
        foliageNoise = new Noise(networkSeed.Value, foliageNoiseSettingsList);
    }
    public void SetUpdateRenderDistance(bool value)
    {
        updateRenderDistance = value;
    }
    public void GeneratePreviewEditor()
    {
        foreach (Chunk chunk in chunkDictionary.Values)
        {
            chunk.meshData.Destroy();
        }
        foreach (Transform transform in GetComponentsInChildren<Transform>())
        {
            if (transform != this.transform)
                DestroyImmediate(transform.gameObject);
        }

        SetValues();
        UpdateRenderDistance(true,true,true,false);
    }
    private void UpdateRenderDistance(bool regenerateHeightMesh = false, bool createMesh = false, bool alwaysUpdate = false, bool threadHeightMapGen = false)
    {
        Vector2Int chunk = Vector2Int.zero;
        if(Player.localPlayer)
            chunk = GetChunkCoord(Player.localPlayer.transform.position);

        if (alwaysUpdate || (updateRenderDistance && chunk != lastUpdateCoord))
        {
            int renderDistanceChunks = Mathf.RoundToInt(renderDistanceMeters / chunkSize);
            int foliageRenderDistanceChunks = Mathf.RoundToInt(foliageRenderDistanceMeters/ chunkSize);
            
            for (int x = -renderDistanceChunks - 1; x <= renderDistanceChunks; x++)
            {
                for (int y = -renderDistanceChunks - 1; y <= renderDistanceChunks; y++)
                {
                    bool spawnFoliage = Mathf.Abs(x) <= foliageRenderDistanceChunks &&
                                       Mathf.Abs(y) <= foliageRenderDistanceChunks;
                    Chunk updatedChunk = CreateOrUpdateChunk(new Vector2Int(chunk.x + x, chunk.y + y), regenerateHeightMesh, createMesh, threadHeightMapGen,spawnFoliage);
                    if (spawnFoliage)
                    {
                        StartCoroutine(updatedChunk.SpawnFoliage());
                    }
                }
            }
            foreach (KeyValuePair<Vector2Int,Chunk> keyValuePair in chunkDictionary)
            {
                if(keyValuePair.Key.x < chunk.x - renderDistanceChunks - 2 || keyValuePair.Key.x > chunk.x + renderDistanceChunks + 2 ||
                   keyValuePair.Key.y < chunk.y - renderDistanceChunks - 2 || keyValuePair.Key.y > chunk.y + renderDistanceChunks + 2)
                {
                    keyValuePair.Value.Destroy();
                }
            }
            lastUpdateCoord = chunk;
        }
    }
    public Chunk CreateOrGetChunk(Vector2Int coord)
    {
        if (chunkDictionary.TryGetValue(coord, out Chunk chunk))
        {
            return chunk;
        }
        else
        {
            Chunk newChunk = new Chunk(coord);
            chunkDictionary.Add(coord, newChunk);
            return newChunk;
        }
    }

    public bool HasChunkGenerated(Vector2Int coord)
    {
        if (chunkDictionary.TryGetValue(coord, out Chunk chunk))
        {
            if (chunk.threading)
                return false;
            if (chunk.meshData != null)
                return true;
            return false;
        }
        else
        {
            return false;
        }
    }
    public Chunk CreateOrUpdateChunk(Vector2Int coord, bool regenerateHeightMesh = false, bool createMesh = false, bool threadHeightMapGen = false, bool spawnFoliage = false)
    {
        if (chunkDictionary.TryGetValue(coord, out Chunk chunk))
        {
            chunk.onMeshRecievedSpawnFoliage = spawnFoliage;
            if (regenerateHeightMesh)
            {
                chunk.GenerateHeightMap();
                if(createMesh)
                    chunk.GenerateMesh();
            }
            else if(createMesh && chunk.meshData == null)
                chunk.GenerateMesh();

            return chunk;
        }
        else
        {
            Chunk newChunk = new Chunk(coord, threadHeightMapGen);
            newChunk.onMeshRecievedSpawnFoliage = spawnFoliage;
            if (createMesh)
                newChunk.GenerateMesh();
            chunkDictionary.Add(coord, newChunk);
            return newChunk;
        }
    }
    public Chunk FindFittingChunk(float targetAverage, float permissibleThreshold, float maxHeight, float minHeight, Vector2Int coord, int maxSearchDistance)
    {
        float minAverage = targetAverage - permissibleThreshold;
        float maxAverage = targetAverage + permissibleThreshold;

        Chunk mostFitting = null;
        float distFromTarget = float.MaxValue;
        bool TestChunk(Vector2Int coord, out Chunk chunk)
        {
            float heightMax = float.MinValue;
            float heightMin = float.MaxValue;
            float heightAverage = 0f;
            for (int chunkX = -1; chunkX <= 1; chunkX++)
            {
                for (int chunkY = -1; chunkY <=1; chunkY++)
                {
                    Chunk chunk1 = CreateOrGetChunk(new Vector2Int(coord.x + chunkX, coord.y + chunkY));
                    maxHeight = Mathf.Max(maxHeight,chunk1.heightMax);
                    minHeight = Mathf.Max(minHeight, chunk1.heightMin);
                    heightAverage += chunk1.heightAverage;
                }
            }
            heightAverage = heightAverage / 9f;

            chunk = CreateOrGetChunk(coord);
            if (heightAverage >= minAverage && heightAverage <= maxAverage)
            {
                return true;
            }

            float distance = Mathf.Abs(targetAverage - heightAverage);
            if (distance < distFromTarget)
            {
                if (heightMax <= maxHeight && heightMin >= minHeight)
                {
                    mostFitting = chunk;
                    distFromTarget = distance;
                }
            }
            return false;
        }

        for (int chunkX = -maxSearchDistance; chunkX < maxSearchDistance; chunkX++)
        {
            for (int chunkY = -maxSearchDistance; chunkY < maxSearchDistance; chunkY++)
            {
                if(TestChunk(new Vector2Int(coord.x + chunkX, coord.y + chunkY), out Chunk chunk))
                {
                    //experiment
                    //return chunk;
                }
            }
        }

        return mostFitting;
    }
    public Vector2Int GetChunkCoord(Vector3 worldPos)
    {
        return GetChunkCoord(worldPos.x, worldPos.z);
    }
    public Vector2Int GetChunkCoord(float worldPosX, float worldPosZ)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPosX / (float)chunkSize), Mathf.FloorToInt(worldPosZ / (float)chunkSize));
    }
    public float GetHeight(Vector3 worldPos)
    {
        return GetHeight(worldPos.x, worldPos.z);
    }
    public float GetHeight(float worldPosX, float worldPosZ)
    {
        Vector2Int coord = GetChunkCoord(worldPosX, worldPosZ);
        if (chunkDictionary.TryGetValue(coord, out Chunk chunk) && chunk.heightMap != null)
        {
            int vertX = Mathf.FloorToInt((worldPosX - coord.x * chunkSize) / vertSpacing);
            int vertY = Mathf.FloorToInt((worldPosZ - coord.y * chunkSize) / vertSpacing);

            return chunk.heightMap[vertX, vertY];
        }
        else
        {
            return terrainNoise.SampleNoise(worldPosX, worldPosZ);
        }
    }
    public Color GetColor(float worldPosX, float worldPosZ)
    {
        Vector2Int coord = GetChunkCoord(worldPosX, worldPosZ);
        if (chunkDictionary.TryGetValue(coord, out Chunk chunk) && chunk.heightMap != null)
        {
            int vertX = Mathf.FloorToInt((worldPosX - coord.x * chunkSize) / vertSpacing);
            int vertY = Mathf.FloorToInt((worldPosZ - coord.y * chunkSize) / vertSpacing);

            return chunk.colorMap[vertX, vertY];
        }
        else
        {
            return new Color();
        }
    }
    public class Chunk
    {
        public float[,] heightMap;
        public Color[,] colorMap;
        public Vector2Int worldPos;
        public Vector2Int coord;

        //Height statistic
        public float heightAverage;
        public float heightMax;
        public float heightMin;

        //Mesh
        public MeshData meshData;
        public Dictionary<ObjectPool, List<GameObject>> poolDictionary;

        public bool onMeshRecievedSpawnFoliage;
        public bool foliageSpawned;
        public bool threading = false;
        public Chunk(Vector2Int coord, bool threadHeightMapGen = false)
        {
            this.coord = coord;
            worldPos = coord * active.chunkSize;
            poolDictionary = new Dictionary<ObjectPool, List<GameObject>>();
            if (threadHeightMapGen)
                GenerateHeightMapThreaded();
            else
                GenerateHeightMap();
        }
        public void GenerateHeightMapThreaded()
        {
            if(threading)
            {
                Debug.Log("Is currently therading heightMap. Ignoreing");
                return;
            }
            threading = true;
            ThreadManager.AddThreadJob(() => GenerateHeightMapThread(), OnHeightMapRecieved);
        }
        public object GenerateHeightMapThread()
        {
            float[,] heightMap = new float[active.chunkVertBorderCount + 1, active.chunkVertBorderCount + 1];
            heightAverage = 0f;
            heightMax = float.MinValue;
            heightMin = float.MaxValue;
            for (int x = 0; x < active.chunkVertBorderCount + 1; x++)
            {
                for (int y = 0; y < active.chunkVertBorderCount + 1; y++)
                {
                    float height = terrainNoise.SampleNoise(worldPos.x + x * active.vertSpacing, worldPos.y + y * active.vertSpacing);
                    heightMap[x, y] = height;
                    if (height < heightMin)
                        heightMin = height;
                    if (height > heightMax)
                        heightMax = height;
                    heightAverage += height;
                }
            }
            heightAverage = heightAverage / ((active.chunkVertBorderCount + 1) * (active.chunkVertBorderCount + 1));
            return heightMap;
        }
        public void GenerateHeightMap()
        {
            heightMap = (float[,])GenerateHeightMapThread();
            colorMap = new Color[heightMap.GetLength(0), heightMap.GetLength(1)];
        }
        public void OnHeightMapRecieved(object heightMapObj)
        {
            heightMap = (float[,])heightMapObj;
            colorMap = new Color[heightMap.GetLength(0), heightMap.GetLength(1)];
            threading = false;
        }
        public void GenerateMesh()
        {
            if (threading)
            {
                Debug.Log("Is currently therading. Ignoreing");
                return;
            }

            if (meshData != null)
                meshData.Destroy();

            threading = true;
            ThreadManager.AddThreadJob(() => GenerateMeshThread(),OnMeshRecieved);
        }
        private object GenerateMeshThread()
        {
            float[,] extendedHeightMap = new float[heightMap.GetLength(0) + 2, heightMap.GetLength(1) + 2];
            Color[,] extendedColorMap = new Color[heightMap.GetLength(0) + 2, heightMap.GetLength(1) + 2];
            for (int x = 0; x < extendedHeightMap.GetLength(0); x++)
            {
                for (int y = 0; y < extendedHeightMap.GetLength(1); y++)
                {
                    if (x > 0 && x < extendedHeightMap.GetLength(0) - 1 && y > 0 && y < extendedHeightMap.GetLength(1) - 1)
                    {
                        //Debug.Log(x + ", " + y);
                        extendedHeightMap[x, y] = heightMap[x - 1, y - 1];
                        extendedColorMap[x, y] = colorMap[x - 1, y - 1];
                    }
                    else
                    {
                        extendedHeightMap[x, y] = active.GetHeight(worldPos.x + (x - 1) * active.vertSpacing, worldPos.y + (y - 1) * active.vertSpacing);
                        extendedColorMap[x, y] = active.GetColor(worldPos.x + (x - 1) * active.vertSpacing, worldPos.y + (y - 1) * active.vertSpacing);
                    }
                }
            }
            return MeshGenerator.GenerateTerrainMesh(extendedHeightMap, extendedColorMap, active.vertSpacing);
        }
        private void OnMeshRecieved(object meshDataObj)
        {
            meshData = (MeshData)meshDataObj;
            meshData.CreateMesh();
            meshData.CreateMeshObject(worldPos);
            threading = false;

            if(onMeshRecievedSpawnFoliage)
                active.StartCoroutine(SpawnFoliage(true));   
        }
        
        public IEnumerator SpawnFoliage(bool forceRegenerate = false)
        {
            if (forceRegenerate)
            {
                DestroyFoliage();
            }
            else
            {
                if (foliageSpawned || threading || meshData == null)
                    yield break;
            }

            foliageSpawned = true;
            yield return new WaitForSeconds(1f);

            float increment = active.chunkSize / active.foliageDensity;
            for (int x = 0; x < active.foliageDensity; x++)
            {
                for (int y = 0; y < active.foliageDensity; y++)
                {
                    if (coord.y > 3)
                        yield return new WaitForSeconds(0.05f);
                    SpawnFoliageAtPos(new Vector3(worldPos.x, 0, worldPos.y) + new Vector3(x * increment, 0, y * increment));
                }
            }
        }

        private void SpawnFoliageAtPos(Vector3 pos)
        {
            //Check distance from track
            float noise = foliageNoise.SampleNoise(pos.x, pos.z);
            TrackManager.active.GetClosestTrackSection(pos, out TrackSection trackSection, out float distance);
            float dist = TrackManager.active.GetDistanceFromPath(trackSection.path, pos);
            if (noise > active.foliageSpawnProbability_DistanceFromTrack.Evaluate(dist))
                return;

            System.Random rng = new System.Random(Mathf.RoundToInt(noise * 1000));
            
            //Debug.DrawLine(pos, pos + Vector3.up * 20f, Color.coral, 60f);
            Vector2 randomDir = new Vector2((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f);
            pos += new Vector3(randomDir.x * active.foliageDensity, 0, randomDir.y * active.foliageDensity);

            if (Physics.SphereCast(new Vector3(pos.x, 50f, pos.z), 2.5f, Vector3.down, out RaycastHit hit, 75f, active.foliageRaycastLayerMask))
            {
                //Check hit layer
                if((active.foliageSpawnLayerMask & (1 << hit.transform.gameObject.layer)) == 0)
                    return;
                
                //Enable/Disable probability list elements
                for (int i = 0; i < active.foliageProbabilityList.probabilityList.Count; i++)
                {
                    ProbabilityListElement<FoliageData> listElement = active.foliageProbabilityList.probabilityList[i];
                    if (hit.point.y >= listElement.element.minMaxTerrainHeight.x && 
                        hit.point.y <= listElement.element.minMaxTerrainHeight.y &&
                        hit.normal.y >= listElement.element.minNormalY)
                    {
                        listElement.Enable();
                    }
                    else
                    {
                        listElement.Disable();
                    }
                }
                
                
                FoliageData foliageData = active.foliageProbabilityList.PickNext(true,Mathf.RoundToInt(foliageNoise.SampleNoise(pos.x,pos.z) * 10000));
                if (foliageData == null) return;
                
                
                Transform copy = foliageData.objectPool.Get().transform;
                copy.position = hit.point - new Vector3(0f, 0.1f, 0f);
                copy.position = new Vector3(copy.position.x,
                    Mathf.Clamp(copy.position.y, foliageData.heightClamp.x, foliageData.heightClamp.y),
                    copy.position.z);
                
                copy.rotation = Quaternion.Euler(rng.Next(-5,5), rng.Next(0,360), rng.Next(-5,5));

                if (poolDictionary.TryGetValue(foliageData.objectPool, out List<GameObject> objectList))
                {
                    objectList.Add(copy.gameObject);
                }
                else
                {
                    poolDictionary.Add(foliageData.objectPool, new List<GameObject>() { copy.gameObject });
                }
            }
        }

        public Vector2Int GetVertex(Vector3 pos)
        {
            return new Vector2Int(Mathf.FloorToInt((pos.x - worldPos.x) / active.vertSpacing), Mathf.FloorToInt((pos.z - worldPos.y) / active.vertSpacing));
        }
        //public Vector3 GetNormal()
        //{
        //
        //}
        public Vector3 GetVertexWorldPos(int x, int y)
        {
            return new Vector3(x * active.vertSpacing + worldPos.x, heightMap[Mathf.Clamp(x,0, heightMap.GetLength(0) - 1), Mathf.Clamp(y, 0, heightMap.GetLength(1) - 1)], y * active.vertSpacing + worldPos.y);
        }
        public Vector3 GetWorldPos(float timeX, float timeZ)
        {
            return new Vector3(worldPos.x + timeX * active.chunkSize, GetVertexWorldPos(Mathf.FloorToInt(timeX * (heightMap.GetLength(0)-1f)), Mathf.FloorToInt(timeZ * (heightMap.GetLength(1) - 1f))).y, worldPos.y + timeZ * active.chunkSize); 
        }

        private void DestroyFoliage()
        {
            foreach (KeyValuePair<ObjectPool, List<GameObject>> poolObjectPair in poolDictionary)
            {
                foreach (GameObject obj in poolObjectPair.Value)
                {
                    poolObjectPair.Key.Add(obj);
                }
            }
        }
        public void Destroy()
        {
            if (meshData == null)
                return;

            DestroyFoliage();

            meshData.Destroy();
            meshData = null;
            foliageSpawned = false;
            onMeshRecievedSpawnFoliage = false;
            
            poolDictionary = new Dictionary<ObjectPool, List<GameObject>>();
        }
    }
}
