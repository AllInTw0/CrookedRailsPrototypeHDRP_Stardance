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
        
        float distance = Util.O1RangeToVector2Range(Mathf.PerlinNoise1D(Mathf.Round(TrackManager.GetPathPosition(path, 0f).magnitude)), minMaxStartDistance);
        distance = Mathf.Clamp(distance, minMaxStartDistance.x, pathLength);

        while (distance <= pathLength)
        {
            PrefabEntry prefabEntry = prefabList[Util.SeedToRandomIndex(Mathf.Round(TrackManager.GetPathPosition(path, distance).magnitude),prefabList.Count-1)];
            GameObject copy = Instantiate(prefabEntry.prefab, transform);

            Vector3 dirVector = TrackManager.GetPathDirectionVector(path, distance);
            copy.transform.position = TrackManager.GetPathPosition(path, distance) + dirVector * prefabEntry.offset.z + new Vector3(-dirVector.z, dirVector.y, dirVector.x) * prefabEntry.offset.x + Vector3.up * prefabEntry.offset.y;
            copy.transform.LookAt(copy.transform.position + dirVector);
            copy.transform.rotation *= Quaternion.Euler(prefabEntry.rotation);

            distance += Util.O1RangeToVector2Range(Mathf.PerlinNoise1D(Mathf.Round(TrackManager.GetPathPosition(path, distance).magnitude)), minMaxRepeatingDistance);
        }
    }
}
