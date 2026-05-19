using UnityEngine;

public class EnemyHealth : Health
{
    [Header("Enemy")]
    [SerializeField] 
    private Enemy behaviour;
    [SerializeField] 
    private float destroyDelay;
    [SerializeField]
    private Collider collision;
    [SerializeField]
    private GameObject ragdollPrefab;

    private void Start()
    {
        AddHealthToGlobalList();
    }

    private void Update()
    {
        UpdateCall();
    }
    public override void HealthReachedZero(Vector3 force = default)
    {
        collision.enabled = false;
        SpawnRagdoll(behaviour.animator.gameObject, ragdollPrefab, force, 10f);
        EnemyManager.active.RemoveEnemy(behaviour);
        //behaviour.Freeze();
        Destroy(gameObject,0f);
    }
}
