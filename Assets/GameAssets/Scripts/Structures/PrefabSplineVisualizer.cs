using System.Collections.Generic;
using UnityEngine;

public class PrefabSplineVisualizer : SplineVisualizer
{
    [System.Serializable]
    public class PrefabEntry
    {
        public GameObject prefab;
        public Vector3 rotation;
        public Vector3 offset;
    }
    [SerializeField]
    private List<PrefabEntry> prefabList;
    [SerializeField]
    private Vector2 minMaxStartDistance;
    [SerializeField]
    private Vector2 minMaxRepeatingDistance;
    public override void Visualize(List<PathPoint> path)
    {
        float pathLength = path[^1].distance;
        float distance = 0f;

        if (pathLength < distance)
        {
            Debug.LogWarning("Path to short to place prefab");
            return;
        }

        distance = Random.Range(minMaxStartDistance.x, minMaxStartDistance.y);

        while (distance < pathLength)
        {
            PrefabEntry prefabEntry = prefabList[Random.Range(0, prefabList.Count)];
            GameObject copy = Instantiate(prefabEntry.prefab, transform);

            Vector3 dirVector = TrackManager.GetPathDirectionVector(path, distance);
            copy.transform.position = TrackManager.GetPathPosition(path, distance) + dirVector * prefabEntry.offset.z + new Vector3(-dirVector.z, dirVector.y, dirVector.x) * prefabEntry.offset.x + Vector3.up * prefabEntry.offset.y;
            copy.transform.LookAt(copy.transform.position + dirVector);
            copy.transform.rotation *= Quaternion.Euler(prefabEntry.rotation);

            distance += Random.Range(minMaxRepeatingDistance.x, minMaxRepeatingDistance.y);
        }
    }
}
