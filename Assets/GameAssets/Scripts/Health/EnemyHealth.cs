using Unity.VisualScripting;
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
    
    public Enemy GetBehaviour()
    {
        return behaviour;
    }
    public override void Start()
    {
        base.Start();
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
        
        transform.position = Vector3.zero;
        behaviour.Freeze();
        
        if (IsServer)
        {
            Invoke(nameof(Despawn), 1f);
        }
    }

    public void Despawn()
    {
        NetworkObject.Despawn(true);
    }
}
