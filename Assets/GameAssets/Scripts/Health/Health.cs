using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public enum HealthType
{
    None,
    Player,
    Train,
    Enemy,
    Sentry,
    Robot
}

public class Health : NetworkBehaviour
{
    private int health;
    public int GetHealth()
    {
        return health - clientSideDamage;
    }
    
    [Header("Health")]
    public HealthType healthType;
    public int startingHealth = 100;
    public int maxHealth = 100;
    public string audioOnDamageTaken;
    [Header("Origin")]
    public Transform origin;
    [Header("Debug")] 
    public bool invulnerable;
    
    private Vector3 forceSum;
    private int damageSum;
    public int clientSideDamage;
    
    public UnityEvent<float,Vector3> onTakeDamage;
    private void Awake()
    {
        onTakeDamage = new UnityEvent<float,Vector3>();
    }
    private void Start()
    {
        AddHealthToGlobalList();
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer)
            SetHealthRpc(startingHealth);
    }

    public void AddHealthToGlobalList()
    {
        if(EnemyManager.active != null)
            EnemyManager.active.healthTypeArray[(int)healthType].Add(this);
    }
    private void Update()
    {
        UpdateCall();
    }
    public void UpdateCall()
    {
        if (damageSum != 0)
        {
            TakeDamageServerRpc(damageSum, forceSum);
            damageSum = 0;
            forceSum = Vector3.zero;
        }
    }
    public int TakeDamage(float damage, Vector3 force = default)
    {
        if (GetHealth() == 0 && damage > 0)
            return 0;

        int damageRounded = Mathf.RoundToInt(damage);
        int damageDone = ClampDamage(damageRounded);
        Debug.Log(damageRounded+","+damageDone + "," + GetHealth());
        damageSum += damageDone;
        if(IsServer == false)
            clientSideDamage += damageDone;
        forceSum += force;
        
        return damageDone;
    }
    [Rpc(SendTo.Server)]
    private void TakeDamageServerRpc(int damage, Vector3 force = default)
    {
        damage = ClampDamage(damage);
        
        health -= damage;

        SetHealthRpc(health, force);
    }

    [Rpc(SendTo.Everyone)]
    private void SetHealthRpc(int target, Vector3 force = default)
    {
        clientSideDamage = 0;
        
        int previousHealth = health;
        health = target;

        if (previousHealth > health)
        {
            onTakeDamage.Invoke(previousHealth - health,force);
            
            if (audioOnDamageTaken != "")
                SoundManager.active.PlayAtPos(transform.position,audioOnDamageTaken);
        }
        
        if (GetHealth() <= 0)
        {
            HealthReachedZero(force);
        }
    }
    private int ClampDamage(int damage)
    {
        Vector2Int minMax = new Vector2Int(0, maxHealth);
        if (invulnerable)
            minMax.x = 1;

        damage = -damage;
        
        Vector2Int minMaxDamage = new Vector2Int(minMax.x - GetHealth(), minMax.y - GetHealth());
        
        if(damage < minMaxDamage.x)
            return -minMaxDamage.x;
        if (damage > minMaxDamage.y)
            return -minMaxDamage.y;
        
        return -damage;
    }
    public virtual void HealthReachedZero(Vector3 force = default)   
    {
        
    }
    private void OnDestroy()
    {
        if(EnemyManager.active)
            EnemyManager.active.healthTypeArray[(int)healthType].Remove(this);
    }
    public GameObject SpawnRagdoll(GameObject targetObject, GameObject ragdollPrefab, Vector3 startVelocity = default, float destroyTime = -1f)
    {
        GameObject ragdoll = Instantiate(ragdollPrefab, targetObject.transform.position, Quaternion.Euler(0, targetObject.transform.rotation.y, 0));
        if(destroyTime >= 0)
        {
            Destroy(ragdoll, destroyTime);
        }

        Transform[] targetTransformArray = targetObject.GetComponentsInChildren<Transform>();

        foreach (Rigidbody ragdolRigibody in ragdoll.GetComponentsInChildren<Rigidbody>())
        {
            int i = 0;
            for (i = 0; i < targetTransformArray.Length; i++)
            {
                if (targetTransformArray[i].name == ragdolRigibody.name)
                {
                    ragdolRigibody.transform.position = targetTransformArray[i].position;
                    ragdolRigibody.transform.rotation = targetTransformArray[i].rotation;
                    break;
                }
            }
            ragdolRigibody.linearVelocity = startVelocity;

            if (i >= targetTransformArray.Length)
            {
                Debug.LogWarning("Couldnt find limb: " + ragdolRigibody.gameObject.name);
            }
        }

        return ragdoll;
    }
    public Vector3 GetOrigin()
    {
        if (origin)
            return origin.position;
        return transform.position;
    }
}
