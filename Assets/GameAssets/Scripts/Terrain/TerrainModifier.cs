using System;
using System.Collections;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class TerrainModifier : StructureGenerator
{
    public BoxCollider boxCollider;
    public AnimationCurve shapeBasedOnDistance;
    public float influenceDistance;
    public bool modifyOnStructureGenEnd;
    private void Awake()
    {
        if(boxCollider) boxCollider.enabled = false;
    }
    
    public override IEnumerator Generate(StructureMaster structureMaster)
    {
        if (modifyOnStructureGenEnd)
        {
            structureMaster.onFinishedGenerating.AddListener(() =>
            {
                ModifyGround(transform.position, influenceDistance, shapeBasedOnDistance, boxCollider);
            });
        }
        else
            ModifyGround(transform.position, influenceDistance, shapeBasedOnDistance, boxCollider);
        yield break;
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer == false)
            ModifyGround(transform.position, influenceDistance, shapeBasedOnDistance, boxCollider);
    }

    public static void ModifyGround(Vector3 worldPos, float influenceDistance, AnimationCurve shapeBasedOnDistance = null, BoxCollider boxCollider = null, Vector2 minMaxTerrainOffset = default)
    {
        Vector2Int chunkCoord = TerrainGeneration.active.GetChunkCoord(worldPos);

        int exploreDist = Mathf.RoundToInt(influenceDistance / TerrainGeneration.active.chunkSize + 0.5f);
        if (exploreDist < 1)
            exploreDist = 1;

        for (int chunkX = -exploreDist; chunkX <= exploreDist; chunkX++)
        {
            for (int chunkY = -exploreDist; chunkY <= exploreDist; chunkY++)
            {
                TerrainGeneration.Chunk chunk = TerrainGeneration.active.CreateOrGetChunk(new Vector2Int(chunkCoord.x + chunkX, chunkCoord.y + chunkY));

                for (int x = 0; x < chunk.heightMap.GetLength(0); x++)
                {
                    for (int y = 0; y < chunk.heightMap.GetLength(1); y++)
                    {
                        Vector3 vertWorldPos = chunk.GetVertexWorldPos(x, y);

                        float distance = Vector2.Distance(new Vector2(vertWorldPos.x, vertWorldPos.z), new Vector2(worldPos.x, worldPos.z));

                        if (distance < influenceDistance && (boxCollider == null || Util.PointInBox(vertWorldPos, boxCollider)))
                        {
                            //interesting test
                            float addition = 0f;
                            if (minMaxTerrainOffset != default)
                            {
                                addition = TerrainGeneration.terrainNoise.SampleNoise(vertWorldPos.x, vertWorldPos.z);
                                int saftey = 50;
                                while (addition < minMaxTerrainOffset.x || addition > minMaxTerrainOffset.y)
                                {
                                    if (addition < minMaxTerrainOffset.x)
                                    {
                                        addition = minMaxTerrainOffset.x +(minMaxTerrainOffset.x - addition) * 0.9f;
                                    }
                                    if (addition > minMaxTerrainOffset.y)
                                    {
                                        addition = minMaxTerrainOffset.y + (minMaxTerrainOffset.y - addition) * 0.9f;
                                    }

                                    saftey--;
                                    if (saftey < 0)
                                    {
                                        Debug.LogWarning("EXEDED TERRAIN MODIFIER SAFTEY");
                                        break;
                                    }
                                }
                            }
                            float time = 1f;
                            if(shapeBasedOnDistance != null)
                                time = shapeBasedOnDistance.Evaluate(distance);
                            chunk.heightMap[x, y] = Mathf.Lerp(vertWorldPos.y, worldPos.y, time) + addition;
                        }
                        Debug.DrawRay(vertWorldPos, Vector3.up, Color.blue, 20f);
                    }
                }
            }
        }
    }
}
