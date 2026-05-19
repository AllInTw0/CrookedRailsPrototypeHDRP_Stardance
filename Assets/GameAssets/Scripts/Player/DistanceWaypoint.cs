using UnityEngine;

public class DistanceWaypoint : MonoBehaviour
{
    public float maxDistance = 30f;
    public bool spawnFoliage = true;

    void Start()
    {
        GameStateManager.waypointList.Add(this);
    }
    public void SpawnFoliage()
    {
        //if (spawnFoliage)
            //StartCoroutine(GenerationManager.active.GenerateFoliageAroundPoint(transform.position));
    }
    private void OnDestroy()
    {
        GameStateManager.waypointList.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
