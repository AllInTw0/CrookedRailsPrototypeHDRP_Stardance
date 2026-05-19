using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackGeneration : MonoBehaviour
{
    public static TrackGeneration active;
    [Header("Pathfinding")]
    [SerializeField]
    private AStar.AStarSettings pathFindingSettings;
    [Header("Spline Gen")]
    [SerializeField]
    private int trackSplineNodeIncrement;
    [SerializeField]
    private int splineHandleLength;
    [SerializeField]
    private float ballastInfluenceDistance;
    [SerializeField]
    private AnimationCurve ballastShapeBasedOnDistance;
    [SerializeField]
    private float heightDffrenceToCreateBridge;
    [SerializeField]
    private int bridgeSplineMeshIndex;
    [SerializeField]
    private int minPathPointCountToCreateBridge;
    [SerializeField]
    private float waterHeight;
    private void Awake()
    {
        active = this;
    }
    public void GenerateTrack(Vector3 start, Vector3 end, Point lastSplinePoint = null)
    {
        AStar aStar = new AStar(start, end, pathFindingSettings);
        NodePath nodePath = aStar.FindPath(aStar.GetNode(start), aStar.GetNode(end));
        CreateTrackAlongNodes(nodePath, out List<PathPoint> allPathPoints, out TrackSection lastTrackSectionOut, lastSplinePoint);
    } 
    public List<NodePath> FindPaths(Vector3 start, Vector3 end, List<int> pathSeeds, int count = 3, float loadingBarTime = 0.4f)
    {
        List<NodePath> nodePathList = new List<NodePath>();
        for (int i = 0; i < pathSeeds.Count; i++)
        {
            LoadingScreen.active.SetProgress((i / (float)pathSeeds.Count) * loadingBarTime, $"Finding paths {i}/{pathSeeds.Count}");
            pathFindingSettings.seed = pathSeeds[i];
            AStar aStar = new AStar(start, end, pathFindingSettings);
            nodePathList.Add(aStar.FindPath(aStar.GetNode(start), aStar.GetNode(end)));
            //ThreadManager.AddMainThreadJob(delegate { Debug.Log("Path found! Length: " + nodePathList[^1].length); });
        }
        LoadingScreen.active.SetProgress(loadingBarTime, $"Finding paths {pathSeeds.Count}/{pathSeeds.Count}");
        //Never again
        //IOrderedEnumerable<NodePath> sortedList = nodePathList.OrderBy(nodePath => nodePath.length);
        bool sorted = false;
        while (sorted == false)
        {
            sorted = true;
            for (int i = 1; i < nodePathList.Count; i++)
            {
                if (nodePathList[i].length < nodePathList[i - 1].length)
                {
                    NodePath temp = nodePathList[i];
                    nodePathList[i] = nodePathList[i - 1];
                    nodePathList[i - 1] = temp;
                    sorted = false;
                }
            }
        }
        for (int i = 0; i < nodePathList.Count; i++)
        {
            float length = nodePathList[i].length;
            ThreadManager.AddMainThreadJob(delegate { Debug.Log("Sorred Path Length: " + length); });
        }
        if (count == 1)
            return new List<NodePath>() { nodePathList[0] };

        List <NodePath> chosenPathList = new List<NodePath>();
        float step = nodePathList.Count / ((float)count-1f);
        for (int i = 0; i < count; i++)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt(step * (float)i), 0, nodePathList.Count - 1);
            ThreadManager.AddMainThreadJob(delegate { Debug.Log("Sorred Path Index: " + index); });
            chosenPathList.Add(nodePathList[index]);
        }
        return chosenPathList;
    }
    public void CreateTrackAlongNodes(NodePath nodePath, out List<PathPoint> allPathPoints, out TrackSection lastTrackSectionOut, Point lastSplinePoint = null, TrackSection lastTrackSection = null)
    {
        for (int i = 0; i < nodePath.path.Count; i++)
        {
            nodePath.path[i].worldPos.y += 0.4f;
            if (nodePath.path[i].worldPos.y < 0)
                nodePath.path[i].worldPos.y = 0;
        }

        if (lastSplinePoint == null) 
        {
            lastSplinePoint = Spline.CreatePoint(nodePath.path[0].worldPos, nodePath.path[0].worldPos + Vector3.forward * splineHandleLength);
        }
        allPathPoints = new List<PathPoint>();
        for (int i = trackSplineNodeIncrement; i < nodePath.path.Count; i+= trackSplineNodeIncrement)
        {
            Node currentNode = nodePath.path[i];

            Vector3 averageDir = (currentNode.worldPos - nodePath.path[i - 1].worldPos).normalized;
            if(i + 1 < nodePath.path.Count)
            {
                averageDir += (nodePath.path[i + 1].worldPos - currentNode.worldPos).normalized;
                averageDir *= 0.5f;
            }

            if (i + trackSplineNodeIncrement >= nodePath.path.Count) //If last track point flatten it out
                averageDir = new Vector3(averageDir.x, 0f, averageDir.z);

            Point newPoint = Spline.CreatePoint(currentNode.worldPos, currentNode.worldPos + averageDir * splineHandleLength);

            if (TrackManager.active)
            {
                TrackSection trackSection = TrackManager.active.CreateTrackSection(lastSplinePoint, newPoint);
                if(lastTrackSection != null)
                {
                    lastTrackSection.SetNextSection(trackSection);
                    //trackSection.SetPreviousSection(lastTrackSection);
                }
                allPathPoints.AddRange(trackSection.path);
                lastTrackSection = trackSection;
            }
            lastSplinePoint = newPoint;
        }
        lastTrackSectionOut = lastTrackSection;
    }
    public void ModifyTerrainToFollowPath(List<PathPoint> path, bool recalculateLength, Vector2 allowedHeightInterval = default, bool createBridges = true)
    {
        if (recalculateLength)
        {
            List<PathPoint> copy = new List<PathPoint>();
            for (int i = 0; i < path.Count; i++)
            {
                copy.Add(new PathPoint(path[i].position, path[i].distance));
            }
            TrackManager.CalculatePath(copy, out List<PathPoint> newPath, 0.5f, true);
            path = newPath;
        }
        List<Vector2Int> chunksToModify = new List<Vector2Int>();
        Vector3[] offsets = {
            new Vector3(ballastInfluenceDistance,0,ballastInfluenceDistance),new Vector3(ballastInfluenceDistance,0,-ballastInfluenceDistance),
            new Vector3(-ballastInfluenceDistance,0, ballastInfluenceDistance),new Vector3(-ballastInfluenceDistance,0, -ballastInfluenceDistance),
            new Vector3(0,0, 0)
        };

        if (createBridges)
        {
            int bridgeStartIndex = -1;
            for (int i = 0; i < path.Count; i++)
            {
                float height = TerrainGeneration.active.GetHeight(path[i].position);
                if ((height < waterHeight || (height < path[i].position.y && path[i].position.y - height >= heightDffrenceToCreateBridge)) && i < path.Count - 1)
                {
                    if (bridgeStartIndex == -1)
                        bridgeStartIndex = i;
                }
                else
                {
                    if (bridgeStartIndex != -1)
                    {
                        if (i - bridgeStartIndex >= minPathPointCountToCreateBridge)
                        {
                            int start = Mathf.Clamp(bridgeStartIndex - 3, 0, path.Count - 1);
                            int end = Mathf.Clamp(i + 3, 0, path.Count);
                            int count = end - start;

                            TrackManager.CalculatePath(path.GetRange(start, count), out List<PathPoint> refactoredPath, 0.5f, true);
                            for (int j = start + 1; j < start + count - 1; j++)
                            {
                                path[j].bridge = true;
                            }
                            ThreadManager.AddMainThreadJob(delegate { Spline.active.GenerateMeshAlongPath(refactoredPath, Spline.active.splineMeshList[bridgeSplineMeshIndex]); });
                        }
                        bridgeStartIndex = -1;
                    }
                }
            }
        }

        //Ballast
        for (int i = 0; i < path.Count; i+=2)
        {
            foreach (Vector3 offset in offsets)
            {
                Vector2Int coord = TerrainGeneration.active.GetChunkCoord(path[i].position + offset);
                if (chunksToModify.Contains(coord) == false)
                {
                    chunksToModify.Add(coord);
                    //Debug.Log("Added: " + coord);
                }
            }
            Debug.DrawRay(path[i].position, Vector3.up, Color.red, 20f);
        }

        foreach (Vector2Int chunkCoord in chunksToModify)
        {
            TerrainGeneration.Chunk chunk = TerrainGeneration.active.CreateOrGetChunk(chunkCoord);
            for (int x = 0; x < chunk.heightMap.GetLength(0); x++)
            {
                for (int y = 0; y < chunk.heightMap.GetLength(1); y++)
                {
                    Vector3 vertWorldPos = chunk.GetVertexWorldPos(x, y);

                    float distance = TrackManager.active.GetDistanceFromPath(path, vertWorldPos, out PathPoint pathPoint, true);

                    float targetY = Mathf.Clamp(vertWorldPos.y, pathPoint.position.y + allowedHeightInterval.x, pathPoint.position.y + allowedHeightInterval.y);

                    if (vertWorldPos.y != targetY && distance < ballastInfluenceDistance && (pathPoint.bridge == false || vertWorldPos.y > targetY))
                    {
                        float time = ballastShapeBasedOnDistance.Evaluate(distance);
                        chunk.heightMap[x, y] = Mathf.Lerp(vertWorldPos.y, targetY, time);
                        if (time >= 0.88f)
                            chunk.colorMap[x, y] = Color.Lerp(chunk.colorMap[x, y], new Color(0f,0f,1f), time);
                    }
                    //Debug.DrawRay(vertWorldPos, Vector3.up, Color.blue, 20f);
                }
            }
        }
        foreach (Vector2Int chunkCoord in chunksToModify)
        {
            TerrainGeneration.active.CreateOrUpdateChunk(chunkCoord);
        }
    }

}

public class NodePath
{
    public float length;
    public List<Node> path;
    public NodePath(List<Node> path, float length)
    {
        this.path = path;
        this.length = length;
    }
}

public class Node
{
    public Vector2Int gridPos;
    public Vector3 worldPos;

    public float gCost; //Cost From Start To This Node
    public float hCost; // Heuristic Cost To The End Node
    public float randomCost;
    public float costSum => gCost + hCost + randomCost;

    public Node previous;
    public Node(Vector2Int gridPos, Vector3 worldPos, AStar aStar)
    {
        this.gridPos = gridPos;
        this.worldPos = new Vector3(worldPos.x, TerrainGeneration.active.GetHeight(worldPos), worldPos.z);

        gCost = 0;
        hCost = 0;
        randomCost = aStar.settings.rng.Next() * aStar.settings.randomnessCostMult;

        Debug.DrawRay(this.worldPos, Vector3.up, Color.white, aStar.settings.debugDrawTime);
    }

}
public class AStar
{
    [System.Serializable]
    public class AStarSettings
    {
        [Header("Nodes")]
        public float nodeSpaceing;
        public int sideNodeCount;

        [Header("Cost Params")]
        public float distMult = 10f;

        public float heightMult = 1f;
        public float heightPow = 2f;

        public float heightChangeMult = 1f;
        public float heightChangePow = 2f;

        public float maxGrade = 5f;
        public float maxGradePenalty = 999999f;

        public float maxTurnAngle = 30f;
        public float maxTurnAnglePenalty = 50f;

        public float waterHeight = 0f;
        public float waterCost = 50f;

        [Header("Randomness")]
        public int seed;
        public float randomnessCostMult;
        public System.Random rng;

        [Header("Debug")]
        public float debugDrawTime;
    }

    public AStarSettings settings;

    public int width, height;
    public Node[,] nodeGrid;

    public Vector3 leftMostBottomPos;
   
    public AStar(Vector3 startPos, Vector3 endPos, AStarSettings settings)
    {
        Debug.DrawRay(startPos, Vector3.up * 10f, Color.red, settings.debugDrawTime);
        Debug.DrawRay(endPos, Vector3.up * 10f, Color.green, settings.debugDrawTime);

        if (endPos.z < startPos.z)
        {
            Vector3 temp = endPos;
            endPos = startPos;
            startPos = temp;
        }

        settings.rng = new System.Random(settings.seed);
        this.settings = settings;

        width = Mathf.FloorToInt((Mathf.Abs(startPos.x - endPos.x)) / settings.nodeSpaceing) + settings.sideNodeCount * 2;
        height = Mathf.FloorToInt(Mathf.Abs(startPos.z - endPos.z) / settings.nodeSpaceing);

        leftMostBottomPos = new Vector3(Mathf.Min(startPos.x, endPos.x) - settings.sideNodeCount * settings.nodeSpaceing, 0, startPos.z);

        nodeGrid = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                nodeGrid[x, y] = new Node(new Vector2Int(x, y), new Vector3(leftMostBottomPos.x + x * settings.nodeSpaceing, 0, leftMostBottomPos.z + y * settings.nodeSpaceing), this);
            }
        }
    }
    public Node GetNode(Vector3 worldPos)
    {
        worldPos -= leftMostBottomPos;

        return nodeGrid[Mathf.Clamp(Mathf.FloorToInt(worldPos.x / settings.nodeSpaceing), 0, width-1), Mathf.Clamp(Mathf.FloorToInt(worldPos.z / settings.nodeSpaceing), 0, height-1)];
    }

    public NodePath FindPath(Node start, Node end)
    {
        List<Node> openSet = new List<Node>() { start };
        HashSet<Node> closedSet = new HashSet<Node>();

        while(openSet.Count > 0)
        {
            Node current = openSet.OrderBy(node => node.costSum).ThenBy(node => node.hCost).First();

            if (current == end)
                return RetracePath(start, end);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbour in GetNeighbours(current))
            {
                if (closedSet.Contains(neighbour))
                    continue;

                //Debug.DrawLine(current.worldPos, neighbour.worldPos, new Color(0.5f, 0.5f, 0.5f, 0.06f), settings.debugDrawTime);
                float gCost = current.gCost + GetCost(current, neighbour);
                if(gCost < neighbour.gCost || openSet.Contains(neighbour) == false)
                {
                    neighbour.gCost = gCost;
                    neighbour.hCost = GetCost(neighbour, end);
                    neighbour.previous = current;

                    if (openSet.Contains(neighbour) == false)
                        openSet.Add(neighbour);
                }
            }
        }

        return null;
    }
    private NodePath RetracePath(Node start, Node end)
    {
        List<Node> path = new List<Node>();
        float length = 0;
        Node current = end;

        while (current != start)
        {
            if (current.previous != null)
            {
                Debug.DrawLine(current.worldPos, current.previous.worldPos, Color.purple, settings.debugDrawTime);
                length += Vector3.Distance(current.worldPos, current.previous.worldPos);
            }

            path.Add(current);
            current = current.previous;
        }

        path.Reverse();
        return new NodePath(path,length);
    }
    private float GetCost(Node nodeA, Node nodeB)
    {
        float totalCost = 0f;

        float deltaX = Mathf.Abs(nodeA.worldPos.x - nodeB.worldPos.x);
        float deltaZ = Mathf.Abs(nodeA.worldPos.z - nodeB.worldPos.z);
        float heightDelta = Mathf.Abs(nodeA.worldPos.y - nodeB.worldPos.y);

        float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        totalCost += distance + settings.distMult;
        totalCost += Mathf.Pow(heightDelta,settings.heightChangePow) + settings.heightChangeMult;
        if (nodeB.worldPos.y > settings.waterHeight)
            totalCost += Mathf.Pow(nodeB.worldPos.y, settings.heightPow) + settings.heightMult;
        else
            totalCost += settings.waterCost;

        //Max grade
        float grade = (heightDelta / distance) * 100f;
        if(grade > settings.maxGrade)
        {
            totalCost += settings.maxGradePenalty;
        }

        //Max Turn Angle
        Vector3 dirA = Vector3.forward;
        if (nodeA.previous != null)
            dirA = nodeA.worldPos - nodeA.previous.worldPos;
        dirA = new Vector3(dirA.x, 0, dirA.z);
        Vector3 dirB = nodeB.worldPos - nodeA.worldPos;
        dirB = new Vector3(dirB.x, 0, dirB.z);

        if (Vector3.Angle(dirA, dirB) > settings.maxTurnAngle)
        {
            totalCost += settings.maxTurnAnglePenalty;
        }

        return totalCost;
    }
    private List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbourList = new List<Node>();

        Vector2Int[] directions = {
            new Vector2Int(0,1),new Vector2Int(1,0),new Vector2Int(-1, 0),new Vector2Int(0, -1),
            new Vector2Int(1, 1),new Vector2Int(1, -1),new Vector2Int(-1, 1),new Vector2Int(-1, -1),
            new Vector2Int(1, 2),new Vector2Int(1, -2),new Vector2Int(-1, 2),new Vector2Int(-1, -2),
            new Vector2Int(2, 1),new Vector2Int(2, -1),new Vector2Int(-2, 1),new Vector2Int(-2, -1),
        };

        foreach (Vector2Int dir in directions)
        {
            int x = node.gridPos.x + dir.x;
            int y = node.gridPos.y + dir.y;

            if (x >= 0 && x < width && y >= 0 && y < height)
                neighbourList.Add(nodeGrid[x, y]);
        }

        return neighbourList;
    }

}
