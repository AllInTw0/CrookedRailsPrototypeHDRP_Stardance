using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineGenerator : StructureGenerator
{
    [Header("Spline Generator")]
    [SerializeField]
    private Section linkedSection;
    [SerializeField]
    List<SplineVisualizer> splineVisualizerList;
    [Header("Path Shape Randomness")]
    [SerializeField]
    private Vector2 minMaxHandleLenghtMult;
    [SerializeField]
    private AnimationCurve offsetMultAlongPathLength;
    [SerializeField]
    private AnimationCurve limitOffsetAlongPathLength;
    [SerializeField]
    private float noiseFrequency;
    [SerializeField]
    private Vector2 minMaxTargetOffset;
    [Header("Path End Randomness")]
    [SerializeField]
    private Vector2 minMaxDistanceForward;
    [SerializeField]
    private Vector2 minMaxDistanceRight;
    [SerializeField]
    private Vector2 minMaxRotOffset;
    [Header("SphereCheck")]
    [SerializeField]
    private float sphereCheckRadius = -1;
    [SerializeField]
    private Vector3 sphereCheckOffset;
    [Header("Terrain")]
    [SerializeField]
    private bool followTerrain;
    [SerializeField]
    private bool checkForTerrainHeight;
    [SerializeField]
    private Vector2 chunkAverageHeigthMinMax;
    [SerializeField]
    private Vector2 heightMinMax;
    [SerializeField]
    private bool modifyGround;
    [SerializeField]
    private Vector2 allowedHeightInterval;
    [Header("Track")]
    [SerializeField]
    private bool checkDistanceToTrack;
    [SerializeField]
    private float minDistanceFromTrack;
    [Header("Min Length")]
    [SerializeField]
    private float minLength = -1f;
    public override IEnumerator Generate(StructureMaster structureMaster)
    {
        Connection startConnection = null;
        foreach (Connection connection in linkedSection.GetConnectingConnectionList())
        {
            if(connection.connectedConnection != null)
            {
                startConnection = connection;
                break;
            }
        }

        foreach (Connection endConnection in linkedSection.GetAllConnectionList())
        {
            if (endConnection == startConnection) continue;

            List<Connection> validConnections = structureMaster.GetValidConnectionsForConnection(endConnection);
            validConnections.Remove(startConnection.connectedConnection);

            if (validConnections.Count > 0)
            {
                //Connect
                Connection targetEndConnection = validConnections[Random.Range(0, validConnections.Count)];
                endConnection.connectionTransform.position = targetEndConnection.connectionTransform.position;
                endConnection.connectionTransform.rotation = Quaternion.Euler(0f, targetEndConnection.connectionTransform.eulerAngles.y - 180f, 0f);
            }
            else
            {
                //Random
                Vector3 GetRandomOffset()
                {
                    return startConnection.connectionTransform.position + -startConnection.connectionTransform.forward * Random.Range(minMaxDistanceForward.x, minMaxDistanceForward.y) + startConnection.connectionTransform.right * Random.Range(minMaxDistanceRight.x, minMaxDistanceRight.y);
                }
                Vector3 endPos = Vector3.zero;
                int saftey = 50;
                while (saftey > 0)
                {
                    bool passed = true;
                    endPos = GetRandomOffset();
                    Debug.DrawLine(startConnection.connectionTransform.position, endPos, Color.green, 30f);
                    Debug.DrawLine(startConnection.connectionTransform.position, endPos, Color.yellow, 30f);
                    if (checkForTerrainHeight)
                    {
                        TerrainGeneration.Chunk chunk = TerrainGeneration.active.CreateOrGetChunk(TerrainGeneration.active.GetChunkCoord(endPos));
                        if(chunkAverageHeigthMinMax.x <= chunk.heightAverage && chunkAverageHeigthMinMax.y >= chunk.heightAverage)
                        {
                            float pointHeight = TerrainGeneration.active.GetHeight(endPos);
                            if (heightMinMax.x <= pointHeight && heightMinMax.y >= pointHeight)
                            { }
                            else
                            {
                                passed = false;
                            }
                        }
                        else
                        {
                            passed = false;
                        }
                        
                    }
                    if (checkDistanceToTrack && passed)
                    {
                        float dist = TrackManager.active.GetDistanceFromTrack(endPos);
                        if (dist < minDistanceFromTrack)
                            passed = false;

                    }
                    if (passed)
                        break;
                    saftey--;
                }
                if (saftey <= 1)
                {
                    Debug.LogWarning("Exeded saftey! " + saftey);
                    structureMaster.RestartGeneration();
                    yield break;
                }
                endConnection.connectionTransform.position = endPos;
                endConnection.connectionTransform.rotation = Quaternion.Euler(0f, startConnection.connectionTransform.eulerAngles.y + Random.Range(minMaxRotOffset.x, minMaxRotOffset.y) + 180f, 0f);
            }
            CreatePath(startConnection, endConnection);
        }

        yield break;
    }
    public void CreatePath(Connection start, Connection end)
    {
        Transform startTransform = start.connectionTransform;
        Transform endTransform = end.connectionTransform;

        float dist = Vector3.Distance(startTransform.position, endTransform.position);

        Point startPoint = Spline.CreatePoint(startTransform.position, startTransform.position - startTransform.forward * (dist * Random.Range(minMaxHandleLenghtMult.x,minMaxHandleLenghtMult.y)));
        Point endPoint = Spline.CreatePoint(endTransform.position, endTransform.position + endTransform.forward * (dist * Random.Range(minMaxHandleLenghtMult.x, minMaxHandleLenghtMult.y)));

        //Calculate path
        TrackManager.CalculatePath(startPoint, endPoint, Spline.CalculateSplineLenght(startPoint, endPoint), out List<PathPoint> splinePath, 4f);
        TrackManager.CalculatePath(splinePath, out List<PathPoint> path, 4f);

        //Modify path
        float randomness = Random.value * 100f;
        float[] offsetValues = new float[path.Count];
        float maxValue = 0f;
        float pathDist = path[0].distance;
        for (int i = 1; i < path.Count - 1; i++)
        {
            pathDist += path[i].distance;
            float noise = (Mathf.PerlinNoise1D(pathDist * noiseFrequency + randomness) * 2f - 1f) * offsetMultAlongPathLength.Evaluate(i / path.Count);
            //Debug.Log(i + " : " + noise + " : " + pathDist * noiseFrequency);
            if (Mathf.Abs(noise) > maxValue)
                maxValue = Mathf.Abs(noise);
            offsetValues[i] = noise;
        }
        float offsetMult = Random.Range(minMaxTargetOffset.x, minMaxTargetOffset.y) / maxValue;
        //Debug.Log("maxValue: " + maxValue + ", offsetMult: " + offsetMult);
        for (int i = 1; i < path.Count-1; i++)
        {
            //Vector3 dir = (path[i].position - path[i - 1].position + path[i + 1].position - path[i].position) * 0.5f;//Average
            Vector3 dir = path[i + 1].position - path[i].position;
            Vector3 perpendicularDir = new Vector3(-dir.z, dir.y, dir.x);

            path[i].position += perpendicularDir.normalized * (offsetValues[i] < 0f ? Mathf.Max(-limitOffsetAlongPathLength.Evaluate(i / (float)path.Count), offsetValues[i] * offsetMult) : Mathf.Min(limitOffsetAlongPathLength.Evaluate(i / (float)path.Count), offsetValues[i] * offsetMult));
        }

        if (followTerrain)
        {
            for (int i = 0; i < path.Count; i++)
            {
                //if (Physics.Raycast(path[i].position + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, LayerMask.GetMask("Terrain")))
                //{
                //    Debug.Log("Terrai Hit!");
                //    path[i].position = hit.point;
                //}
                //else
                //{
                //    Debug.Log("Terrai Nope!");
                path[i].position = new Vector3(path[i].position.x, TerrainGeneration.active.GetHeight(path[i].position), path[i].position.z);
                //}
            }
            end.connectionTransform.position = path[^1].position;
        }

        for (int i = 0; i < path.Count-1; i++)
        {
            Debug.DrawLine(path[i].position, path[i + 1].position, Color.purple, 30f);
        }
        for (int i = 0; i < splinePath.Count - 1; i++)
        {
            Debug.DrawLine(splinePath[i].position, splinePath[i + 1].position, Color.red, 30f);
        }

        TrackManager.CalculatePath(path, out List<PathPoint> meshPath, 1f, followTerrain);

        if (DoesPathOverlap(meshPath))
        {
            linkedSection.isOverlapping = true;
            return;
        }

        foreach (SplineVisualizer splineVisualizer in splineVisualizerList)
        {
            splineVisualizer.Visualize(meshPath);
        }
        if (modifyGround)
        {
            TrackGeneration.active.ModifyTerrainToFollowPath(meshPath, false, allowedHeightInterval, false);
        }
        //for (float i = 0; i < 1f; i+= 0.05f)
        //{
        //    Debug.DrawLine(Spline.CalculateSplinePosition(startPoint, endPoint, i), Spline.CalculateSplinePosition(startPoint, endPoint, i+ 0.05f), Color.purple,30f);
        //}
    }

    public bool DoesPathOverlap(List<PathPoint> path)
    {
        if (sphereCheckRadius <= 0 || (path[^1].distance <= minLength && minLength > 0)) return false;

        float pathLength = path[^1].distance;
        float distance = 0.1f + sphereCheckRadius;

        while (distance < pathLength - sphereCheckRadius)
        {
            if(Physics.CheckSphere(TrackManager.GetPathPosition(path,distance) + sphereCheckOffset, sphereCheckRadius))
            {
                Debug.DrawRay(TrackManager.GetPathPosition(path, distance) + sphereCheckOffset, Vector3.up * sphereCheckRadius, Color.orangeRed, 60f);
                Debug.LogWarning("Spline Overlapping");
                return true;
            }
            distance += sphereCheckRadius;
        }

        return false;
    }
}
