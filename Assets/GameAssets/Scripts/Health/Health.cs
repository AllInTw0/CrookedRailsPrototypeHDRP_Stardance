using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public enum HealthType
{
    None,
    Player,
    Train,
    Enemy,
    Sentry,
    Robot,
    Payload
}
public enum DebuffType
{
    Fire,
    Invulnerability,
    Electricity
}
public class Health : NetworkBehaviour
{
    private NetworkVariable<int> networkMaxHealth = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<HealthNetworkData> healthNetworkData = new(writePerm: NetworkVariableWritePermission.Server);
    public struct HealthNetworkData : INetworkSerializable
    {
        public int health;
        public Vector3 force;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref health);
            serializer.SerializeValue(ref force);
        }
    }

    public void SetHealth(int value)
    {
        healthNetworkData.Value = new HealthNetworkData() { health = value };
    }
    public int GetHealth()
    {
        return healthNetworkData.Value.health - clientSideDamage;
    }

    public void SetMaxHealth(int value)
    {
        networkMaxHealth.Value = value;
    }
    public int GetMaxHealth()
    {
        return networkMaxHealth.Value;
    }
    
    [Header("Health")]
    public HealthType healthType;
    [SerializeField]
    private int startingHealth = 100;
    [SerializeField]
    [FormerlySerializedAs("maxHealth")] 
    private int startingMaxHealth = 100;
    public string audioOnDamageTaken;
    [Header("Origin")]
    public Transform origin;
    [Header("Debug")] 
    public bool invulnerable;
    
    private Vector3 forceSum;
    private int damageSum;
    public int clientSideDamage;
    
    private bool isVisibleGlobally = true;
    private Dictionary<Type, float> visibilityDict  = new Dictionary<Type, float>();
    
    public bool CanBeSeenBy(Type type)
    {
        if(isVisibleGlobally == false)
            return false;
        if (visibilityDict.TryGetValue(type, out float time))
        {
            if (time > 0)
                return false;
            visibilityDict.Remove(type);
            return true;
        }

        return true;
    }
    public void SetGlobalVisibility(bool visible)
    {
        isVisibleGlobally = visible;
    }

    public void UpdateVisibilityCooldown(Type type, float timeInvisible)
    {
        if (visibilityDict.ContainsKey(type))
            visibilityDict[type] = timeInvisible;
        else
            visibilityDict.Add(type, timeInvisible);
    }
    
    public class Debuff
    {
        public DebuffType type;
        public float duration;
        public float value;
        
        public bool owner;

        public Debuff(DebuffType type, float duration = 0f, float value = 0f, bool owner = true)
        {
            this.type = type;
            this.duration = duration;
            this.value = value;
            this.owner = owner;
        }
    }
    [System.Serializable]
    public class DebuffVisualizer
    {
        public DebuffType type;
        public List<ParticleSystem> ParticleSystemList = new List<ParticleSystem>();
        public List<VisualEffect> visualEffectList = new List<VisualEffect>();
        public void ChangeState(bool enabled)
        {
            for (int i = 0; i < ParticleSystemList.Count; i++)
            {
                if(enabled)
                    ParticleSystemList[i].Play();
                else
                    ParticleSystemList[i].Stop();
            }
            for (int i = 0; i < visualEffectList.Count; i++)
            {
                visualEffectList[i].enabled = enabled;
            }
        }
    }

    public UnityEvent<Debuff> onDebuffAdded;
    public UnityEvent<Debuff> onDebuffRemoved;
    private List<Debuff> debuffList = new List<Debuff>();
    [SerializeField]
    private List<DebuffVisualizer> debuffVisualizerList = new List<DebuffVisualizer>();
    public void AddDebuff(Debuff debuff)
    {
        debuffList.Add(debuff);
        for (int i = 0; i < debuffVisualizerList.Count; i++)
        {
            if (debuffVisualizerList[i].type == debuff.type)
            {
                debuffVisualizerList[i].ChangeState(true);
            }
        }
        onDebuffAdded.Invoke(debuff);
    }
    public Debuff GetDebuff(DebuffType type)
    {
        Debuff debuff = null;
        for (int i = 0; i < debuffList.Count; i++)
        {
            if (debuffList[i].type == type)
            {
                if (debuff == null)
                    debuff = new Debuff(type);

                debuff.duration += debuffList[i].duration;
                debuff.value += debuffList[i].value;
            }
        }
        return debuff;
    }
    
    public UnityEvent<float,Vector3> onTakeDamage;
    public UnityEvent<Vector3> onHealthReachedZero;
    private void Awake()
    {
        onDebuffAdded = new UnityEvent<Debuff>();
        onDebuffRemoved = new UnityEvent<Debuff>();
        
        onTakeDamage = new UnityEvent<float,Vector3>();
        
        for (int i = 0; i < debuffVisualizerList.Count; i++)
        {
            debuffVisualizerList[i].ChangeState(false);
        }
    }
    public virtual void Start()
    {
        AddHealthToGlobalList();
        InvokeRepeating(nameof(DebuffTick),1f,1f);
    }

    private void HealthChanged(HealthNetworkData previous, HealthNetworkData current)
    {
        InvokeEvents(previous.health - clientSideDamage, current.health, current.force);
        clientSideDamage = 0;
    }

    private void InvokeEvents(int previousHealth, int currentHealth, Vector3 currentForce)
    {
        
        onTakeDamage.Invoke(previousHealth - currentHealth, currentForce);

        
        if (previousHealth > currentHealth)
        {
            if (audioOnDamageTaken != "")
                SoundManager.active.PlayAtPos(transform.position,audioOnDamageTaken);
        }
        
        if (previousHealth > 0 && currentHealth <= 0)
        {
            HealthReachedZero(currentForce);
        }
    }
    public override void OnNetworkSpawn()
    {
        healthNetworkData.OnValueChanged += HealthChanged;
        
        if (IsServer)
        {
            healthNetworkData.Value = new HealthNetworkData()
            {
                health = startingHealth
            };
            networkMaxHealth.Value = startingMaxHealth;
        }
        else
        {
            InvokeEvents(GetHealth(), GetHealth(), Vector3.zero);
        }
    }

    public void AddHealthToGlobalList()
    {
        if(EnemyManager.active != null)
            EnemyManager.active.healthTypeArray[(int)healthType].Add(this);
    }
    
    public virtual void Update()
    {
        if (damageSum != 0)
        {
            TakeDamageServerRpc(damageSum, forceSum);
            damageSum = 0;
            forceSum = Vector3.zero;
        }

        foreach (Type key in visibilityDict.Keys.ToList())
        {
            visibilityDict[key] -= Time.deltaTime;
        }

        for (int i = 0; i < debuffList.Count; i++)
        {
            debuffList[i].duration -= Time.deltaTime;
            if (debuffList[i].duration <= 0f)
            {
                for (int j = 0; j < debuffVisualizerList.Count; j++)
                {
                    if (debuffVisualizerList[j].type == debuffList[i].type)
                    {
                        debuffVisualizerList[j].ChangeState(false);
                    }
                }
                onDebuffRemoved.Invoke(debuffList[i]);
                debuffList.RemoveAt(i);
                i--;
            }
        }
    }

    public void DebuffTick()
    {
        Debuff fire = GetDebuff(DebuffType.Fire);
        if (fire != null && fire.owner)
        {
            TakeDamage(fire.value);
            HitSound.active.HandleHitSound(this, fire.value, transform.localPosition + Vector3.up);
        }
    }
    public int TakeDamage(float damage, Vector3 force = default)
    {
        if (GetHealth() == 0 && damage > 0)
            return 0;

        int previousHealth = GetHealth();
        
        int damageRounded = Mathf.RoundToInt(damage);
        int damageDone = ClampDamage(damageRounded);
        
        //Debug.Log(damageRounded+","+damageDone + "," + GetHealth());
        
        damageSum += damageDone;
        if(IsServer == false)
            clientSideDamage += damageDone;
        forceSum += force;
        
        InvokeEvents(previousHealth, GetHealth(), force);
        
        return damageDone;
    }
    [Rpc(SendTo.Server)]
    private void TakeDamageServerRpc(int damage, Vector3 force = default)
    {
        damage = ClampDamage(damage);

        healthNetworkData.Value = new HealthNetworkData()
        {
            health = GetHealth() - damage,
            force = force
        };
    }
    
    private int ClampDamage(int damage)
    {
        Vector2Int minMax = new Vector2Int(0, GetMaxHealth());
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
        onHealthReachedZero.Invoke(force);
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

    public int GetHealthPercent()
    {
        return Mathf.RoundToInt(((float)GetHealth() / (float)GetMaxHealth()) * 100f);
    }
    public float GetHealthTime()
    {
        return (float)GetHealth() / (float)GetMaxHealth();
    }
}
