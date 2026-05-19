using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using Random = UnityEngine.Random;
public class Point
{
    public Vector3 position;
    public Vector3 handleForward;
    public Vector3 handleBackward;
}
[System.Serializable]
public struct SplineMeshInfo
{
    public Mesh mainMesh;
    public List<Mesh> repeatingMeshList;
    public float repeatingMeshInterval;
    public float repeatingMeshMinDistanceFromTrack;
    public Material material;
    public int layer;
    public Vector3 scale;
    [System.NonSerialized]
    public Vector3 meshSize;
}
public class Spline : MonoBehaviour
{
    public static Spline active;
    [SerializeField]
    public List<SplineMeshInfo> splineMeshList = new List<SplineMeshInfo>();
    private void Awake()
    {
        active = this;
        for (int i = 0; i < splineMeshList.Count; i++)
        {
            //splineMeshList[i].meshSize = calculateMeshSize(splineMeshList[i].mainMesh);
            //struct version
            SplineMeshInfo splineMeshInfo = splineMeshList[i];
            splineMeshInfo.meshSize = calculateMeshSize(splineMeshList[i].mainMesh);
            splineMeshList[i] = splineMeshInfo;
        }
    }
    
    //Point Functions
    public static Point CreatePoint(Vector3 position, Vector3 handle,bool forwardHandleGiven = true)
    {
        Point point;
        if (forwardHandleGiven)
        {
            point = new Point
            {
                position = position,
                handleForward = handle
            };
        }
        else
        {
            point = new Point
            {
                position = position,
                handleBackward = handle
            };
        }
            
        return CalculateOtherHandle(point, forwardHandleGiven);
    }
    private static Point CalculateOtherHandle(Point point, bool forwardHandleGiven = true)
    {
        if(forwardHandleGiven)
            point.handleBackward = point.position + (point.position - point.handleForward);
        else
            point.handleForward = point.position + (point.position - point.handleBackward);
        return point;
    }
    
    //Spline Functions
    public static Vector3 CalculateSplinePosition(Point pointA, Point pointB, float time)
    {
        return Mathf.Pow((1f - time), 3f) * pointA.position + 3f * Mathf.Pow((1f - time), 2f) * time * pointA.handleForward + 3f * (1f - time) * Mathf.Pow(time, 2f) * pointB.handleBackward + Mathf.Pow(time, 3f) * pointB.position;
    }

    public static Vector3 CalculateSplineDirectionVector(Point pointA, Point pointB, float time)
    {
        Vector3 posA = CalculateSplinePosition(pointA, pointB, time);
        Vector3 posB = CalculateSplinePosition(pointA, pointB, time + 0.00001f);
        return (posB - posA).normalized;
    }

    public static float CalculateSplineLenght(Point pointA, Point pointB)
    {
        float dist = (pointB.position - pointA.position).magnitude;
        float point_dist = (pointA.position - pointA.handleForward).magnitude + (pointB.handleBackward - pointA.handleForward).magnitude + (pointB.position - pointB.handleBackward).magnitude;

        return (point_dist + dist) / 2f;
    }
    
    //Mesh Generation Functions
    public GameObject GenerateMeshAlongSpline(Point pointA, Point pointB, SplineMeshInfo? _splineMesh = null, float fromTime = 0f, float toTime = 1f, bool addCollision = true)
    {
        SplineMeshInfo splineMesh;
        if (_splineMesh.HasValue)
            splineMesh = _splineMesh.Value;
        else
            splineMesh = splineMeshList[0];

        Mesh segmentMesh = new Mesh();
        int last_vert_count = 0;
        List<Vector3> vert_list = new List<Vector3>();
        List<Vector2> uv_list = new List<Vector2>();
        List<int> tris_list = new List<int>();

        //Object for mesh combinations
        GameObject obj = new GameObject();
        Transform objTransform = obj.transform;

        CombineInstance instance = new CombineInstance();
        //
        //Sleepers
        List<CombineInstance> combineMeshes = new List<CombineInstance>();
        //

        float splineLenght = CalculateSplineLenght(pointA,pointB);
        splineLenght *= toTime - fromTime;
        int segments = Mathf.RoundToInt(splineLenght / (splineMesh.meshSize.z * splineMesh.scale.z));
        float repeatingMeshCounter = 0f;
        float increment = (splineLenght / (float)segments) / (splineLenght / (toTime - fromTime));
        for (int i = 0; i < segments; i++)
        {
            //time
            float startTime = fromTime + i * increment;
            repeatingMeshCounter += 1f / splineMesh.repeatingMeshInterval;
            //

            int index = 0;
            foreach (var vert in splineMesh.mainMesh.vertices)
            {
                float zTime = vert.z / splineMesh.meshSize.z;
                
                Vector3 pos = CalculateSplinePosition(pointA, pointB, startTime + increment * zTime);
                Vector3 dirZ = CalculateSplineDirectionVector(pointA, pointB, startTime + increment * zTime);
                Vector3 dirX = new Vector3(-dirZ.z, 0, dirZ.x);
                float xTime = vert.x * splineMesh.scale.x;

                pos += xTime * dirX;

                pos.y += vert.y * splineMesh.scale.y;


                //Debug.DrawRay(pos, Vector3.up * 0.1f, Color.black, 60f);
                vert_list.Add(pos);
                uv_list.Add(splineMesh.mainMesh.uv[index]);
                index++;
            }
            foreach (var tris in splineMesh.mainMesh.triangles)
            {
                index = tris + last_vert_count;
                tris_list.Add(index);
            }
            last_vert_count = vert_list.Count;
            //Debug.DrawRay(posStart, Vector3.up, Color.yellow, 60f);

            //Repeating Meshes (Sleepers)
            int meshCount = Mathf.FloorToInt(repeatingMeshCounter);
            repeatingMeshCounter -= meshCount;
            float meshTimeIncrement = 1f / (float)meshCount;
            for (int a = 0; a < meshCount; a++)
            {
                instance.mesh = splineMesh.repeatingMeshList[Random.Range(0, splineMesh.repeatingMeshList.Count - 1)];
                Vector3 pos = CalculateSplinePosition(pointA, pointB, startTime + meshTimeIncrement * a * increment);
                Vector3 dirZ = CalculateSplineDirectionVector(pointA, pointB, startTime + meshTimeIncrement * a * increment);
                objTransform.position = pos;
                objTransform.rotation = Quaternion.LookRotation(dirZ, Vector3.up);
                instance.transform = objTransform.localToWorldMatrix;
                combineMeshes.Add(instance);
            }
            //
        }
        segmentMesh.vertices = vert_list.ToArray();
        segmentMesh.uv = uv_list.ToArray();
        tris_list.Reverse();
        segmentMesh.triangles = tris_list.ToArray();


        GameObject meshObject = new GameObject("GeneratedTrackMesh");
        meshObject.layer = splineMesh.layer;
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();


        Mesh combinedMesh = new Mesh();
        objTransform.position = Vector3.zero;
        objTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
        instance.mesh = segmentMesh;
        instance.transform = objTransform.localToWorldMatrix;
        combineMeshes.Add(instance);

        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combineMeshes.ToArray());
        combinedMesh.RecalculateNormals();

        meshFilter.sharedMesh = combinedMesh;
        meshRenderer.material = splineMesh.material;

        if (addCollision)
        {
            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
            //meshCollider.convex = true;
            meshCollider.sharedMesh = combinedMesh;
        }

        Destroy(obj);
        return meshObject;
    }
    public GameObject GenerateMeshAlongTrackSection(TrackSection section, SplineMeshInfo? _splineMesh = null, float fromProgress = 0f, float toProgress = -1f, bool addCollision = true)
    {
        List<PathPoint> path = section.path;

        SplineMeshInfo splineMesh;
        if (_splineMesh.HasValue)
            splineMesh = _splineMesh.Value;
        else
            splineMesh = splineMeshList[0];

        if (toProgress < 0f)
            toProgress = path[^1].distance;
        
        Mesh segmentMesh = new Mesh();
        int last_vert_count = 0;
        List<Vector3> vert_list = new List<Vector3>();
        List<Vector2> uv_list = new List<Vector2>();
        List<int> tris_list = new List<int>();

        //Object for mesh combinations
        GameObject obj = new GameObject();
        Transform objTransform = obj.transform;

        CombineInstance instance = new CombineInstance();
        //
        //Sleepers
        List<CombineInstance> combineMeshes = new List<CombineInstance>();
        //

        float pathLenght = toProgress - fromProgress;
        
        int segments = Mathf.RoundToInt(pathLenght / (splineMesh.meshSize.z * splineMesh.scale.z));
        
        float repeatingMeshCounter = 0f;
        float increment = pathLenght / (float)segments;
        
        for (int i = 0; i < segments; i++)
        {
            //time
            float startProgress = fromProgress + i * increment;
            repeatingMeshCounter += 1f / splineMesh.repeatingMeshInterval;
            //

            int index = 0;
            foreach (var vert in splineMesh.mainMesh.vertices)
            {
                float zTime = vert.z / splineMesh.meshSize.z;

                Vector3 pos = TrackManager.GetPathPosition(path, startProgress + increment * zTime);
                Vector3 dirZ = TrackManager.GetPathDirectionVector(section, startProgress + increment * zTime);
                Vector3 dirX = new Vector3(-dirZ.z, 0, dirZ.x);
                float xTime = vert.x * splineMesh.scale.x;

                pos += xTime * dirX;

                pos.y += vert.y * splineMesh.scale.y;


                //Debug.DrawRay(pos, Vector3.up * 0.1f, Color.black, 60f);
                vert_list.Add(pos);
                uv_list.Add(splineMesh.mainMesh.uv[index]);
                index++;
            }
            foreach (var tris in splineMesh.mainMesh.triangles)
            {
                index = tris + last_vert_count;
                tris_list.Add(index);
            }
            last_vert_count = vert_list.Count;
            //Debug.DrawRay(posStart, Vector3.up, Color.yellow, 60f);

            //Repeating Meshes (Sleepers)
            int meshCount = Mathf.FloorToInt(repeatingMeshCounter);
            repeatingMeshCounter -= meshCount;
            float meshTimeIncrement = 1f / (float)meshCount;
            for (int a = 0; a < meshCount; a++)
            {
                instance.mesh = splineMesh.repeatingMeshList[Random.Range(0, splineMesh.repeatingMeshList.Count - 1)];
                Vector3 pos = TrackManager.GetPathPosition(path, startProgress + meshTimeIncrement * a * increment);
                Vector3 dirZ = TrackManager.GetPathDirectionVector(section, startProgress + meshTimeIncrement * a * increment);
                objTransform.position = pos;
                objTransform.rotation = Quaternion.LookRotation(dirZ, Vector3.up);
                instance.transform = objTransform.localToWorldMatrix;
                combineMeshes.Add(instance);
            }
            //
        }
        segmentMesh.vertices = vert_list.ToArray();
        segmentMesh.uv = uv_list.ToArray();
        tris_list.Reverse();
        segmentMesh.triangles = tris_list.ToArray();


        GameObject meshObject = new GameObject("GeneratedTrackMesh");
        meshObject.layer = splineMesh.layer;
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh();
        objTransform.position = Vector3.zero;
        objTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
        instance.mesh = segmentMesh;
        instance.transform = objTransform.localToWorldMatrix;
        combineMeshes.Add(instance);

        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combineMeshes.ToArray());
        combinedMesh.RecalculateNormals();

        meshFilter.sharedMesh = combinedMesh;
        meshRenderer.material = splineMesh.material;

        if (addCollision)
        {
            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
            //meshCollider.convex = true;
            meshCollider.sharedMesh = combinedMesh;
        }

        Destroy(obj);
        return meshObject;
    }
    public GameObject GenerateMeshAlongPath(List<PathPoint> path, SplineMeshInfo? _splineMesh = null, float fromProgress = 0f, float toProgress = -1f, bool addCollision = true)
    {
        SplineMeshInfo splineMesh;
        if (_splineMesh.HasValue)
            splineMesh = _splineMesh.Value;
        else
            splineMesh = splineMeshList[0];

        if (toProgress < 0f)
            toProgress = path[^1].distance;


        Mesh segmentMesh = new Mesh();
        int last_vert_count = 0;
        List<Vector3> vert_list = new List<Vector3>();
        List<Vector2> uv_list = new List<Vector2>();
        List<int> tris_list = new List<int>();

        //Object for mesh combinations
        GameObject obj = new GameObject();
        Transform objTransform = obj.transform;

        CombineInstance instance = new CombineInstance();
        //
        //Sleepers
        List<CombineInstance> combineMeshes = new List<CombineInstance>();
        //

        float pathLenght = toProgress - fromProgress;

        int segments = Mathf.RoundToInt(pathLenght / (splineMesh.meshSize.z * splineMesh.scale.z));

        float repeatingMeshCounter = 0f;
        float increment = pathLenght / (float)segments;

        for (int i = 0; i < segments; i++)
        {
            //time
            float startProgress = fromProgress + i * increment;
            repeatingMeshCounter += 1f / splineMesh.repeatingMeshInterval;
            //

            int index = 0;
            foreach (var vert in splineMesh.mainMesh.vertices)
            {
                float zTime = vert.z / splineMesh.meshSize.z;

                Vector3 pos = TrackManager.GetPathPosition(path, startProgress + increment * zTime);
                Vector3 dirZ = TrackManager.GetPathDirectionVector(path, startProgress + increment * zTime);
                Vector3 dirX = new Vector3(-dirZ.z, 0, dirZ.x);
                float xTime = vert.x * splineMesh.scale.x;

                pos += xTime * dirX;

                pos.y += vert.y * splineMesh.scale.y;


                //Debug.DrawRay(pos, Vector3.up * 0.1f, Color.black, 60f);
                vert_list.Add(pos);
                uv_list.Add(splineMesh.mainMesh.uv[index]);
                index++;
            }
            foreach (var tris in splineMesh.mainMesh.triangles)
            {
                index = tris + last_vert_count;
                tris_list.Add(index);
            }
            last_vert_count = vert_list.Count;
            //Debug.DrawRay(posStart, Vector3.up, Color.yellow, 60f);

            //Repeating Meshes (Sleepers)
            int meshCount = Mathf.FloorToInt(repeatingMeshCounter);
            repeatingMeshCounter -= meshCount;
            float meshTimeIncrement = 1f / (float)meshCount;
            for (int a = 0; a < meshCount; a++)
            {
                Vector3 pos = TrackManager.GetPathPosition(path, startProgress + meshTimeIncrement * a * increment);
                if (splineMesh.repeatingMeshMinDistanceFromTrack > 0 && TrackManager.active.GetDistanceFromTrack(pos) < splineMesh.repeatingMeshMinDistanceFromTrack)
                {
                    continue;
                }

                instance.mesh = splineMesh.repeatingMeshList[Random.Range(0, splineMesh.repeatingMeshList.Count - 1)];
                Vector3 dirZ = TrackManager.GetPathDirectionVector(path, startProgress + meshTimeIncrement * a * increment);
                objTransform.position = pos;
                objTransform.rotation = Quaternion.LookRotation(dirZ, Vector3.up);
                instance.transform = objTransform.localToWorldMatrix;
                combineMeshes.Add(instance);
            }
            //
        }
        segmentMesh.vertices = vert_list.ToArray();
        segmentMesh.uv = uv_list.ToArray();
        tris_list.Reverse();
        segmentMesh.triangles = tris_list.ToArray();


        GameObject meshObject = new GameObject("GeneratedTrackMesh");
        meshObject.layer = splineMesh.layer;
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh();
        objTransform.position = Vector3.zero;
        objTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
        instance.mesh = segmentMesh;
        instance.transform = objTransform.localToWorldMatrix;
        combineMeshes.Add(instance);

        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combineMeshes.ToArray());
        combinedMesh.RecalculateNormals();

        meshFilter.sharedMesh = combinedMesh;
        meshRenderer.material = splineMesh.material;

        if (addCollision)
        {
            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
            //meshCollider.convex = true;
            meshCollider.sharedMesh = combinedMesh;
        }

        Destroy(obj);
        return meshObject;
    }
    private Vector3 calculateMeshSize(Mesh mesh)
    {
        float minX = 9999f; float maxX = -9999f;
        float minY = 9999f; float maxY = -9999f;
        float minZ = 9999f; float maxZ = -9999f;
        foreach (var vert in mesh.vertices)
        {
            //X Axis
            if (vert.x < minX)
                minX = vert.x;
            if (vert.x > maxX)
                maxX = vert.x;
            //

            //Y Axis
            if (vert.y < minY)
                minY = vert.y;
            if (vert.y > maxY)
                maxY = vert.y;
            //

            //Z Axis
            if (vert.z < minZ)
                minZ = vert.z;
            if (vert.z > maxZ)
                maxZ = vert.z;
            //

        }
        return new Vector3(Mathf.Abs(minX) + Mathf.Abs(maxX), Mathf.Abs(minY) + Mathf.Abs(maxY), Mathf.Abs(minZ) + Mathf.Abs(maxZ));
    }
    //Debug Functions
    public static void DEBUG_DrawPointGizmos(Point point, float duration = 60f)
    {
        Debug.DrawRay(point.position, Vector3.up,Color.white, duration);
        Debug.DrawLine(point.handleForward, point.position, Color.green, duration);
        Debug.DrawLine(point.handleBackward, point.position, Color.red,duration);
    }
    private void DEBUG_DrawSpline(Point pointA, Point pointB, float duration = 60f,int segments = 10)
    {
        float timeIncrement = 1f / segments;
        for (float i = 0; i < segments; i++)
        {
            Vector3 posA = CalculateSplinePosition(pointA, pointB, timeIncrement * i);
            Vector3 posB = CalculateSplinePosition(pointA, pointB, timeIncrement * (i+1f));
            //Debug.Log(posA + " => " + posB);
            Debug.DrawLine(posA, posB, Color.blue, duration);
        }     
    }
}
