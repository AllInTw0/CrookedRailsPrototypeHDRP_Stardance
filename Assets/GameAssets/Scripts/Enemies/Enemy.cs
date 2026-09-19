using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Enemy : NetworkBehaviour
{
    public struct EnemyNetworkData : INetworkSerializable
    {
        public Vector3 position;
        public Vector3 velocity;

        public int currentState;
        public float stateTimmer;
        public NetworkObjectReference targetObject;

        public float time;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref currentState);
            serializer.SerializeValue(ref stateTimmer);
            serializer.SerializeValue(ref time);
            serializer.SerializeValue(ref targetObject);
        }
    }

    private NetworkVariable<EnemyNetworkData> networkData = new(writePerm: NetworkVariableWritePermission.Owner);
    
    [Header("Enemy")]
    //Variables
    public EnemySO enemyInfo;
    public Transform centerTransform;
    public Health health;
    //Animation
    [Header("Animation")]
    public Animator animator;
    [Header("Spawning")]
    public float spawnFreezeTime;
    //Behaviour
    public enum State
    {
        Idle,
        Attacking,
        Stunned
    }

    [NonSerialized]public State state = State.Idle;
    [NonSerialized]public float timmer = 0; //Diffrent usses through out the states
    public override void OnNetworkSpawn()
    {
        EnemyManager.active.AddEnemyToList(this);
        networkData.OnValueChanged += NetworkStateChanged;
    }

    private void NetworkStateChanged(EnemyNetworkData previous, EnemyNetworkData current)
    {
        if (IsOwner == false)
        {
            float timeDiff = NetworkManager.Singleton.LocalTime.TimeAsFloat - current.time;
            Debug.Log("Time Diff:" + timeDiff);
            
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.MovePosition(current.position + current.velocity * timeDiff);
            rb.linearVelocity = current.velocity;
            
            state = (State)current.currentState;
            timmer = current.stateTimmer - timeDiff;

            if (this is GroundEnemy && current.targetObject.TryGet(out NetworkObject networkObject))
            {
                ((GroundEnemy)this).movement.SetTarget(networkObject.transform,0.5f);
            }
        }
    }
    
    public virtual void Awake()
    {

    }
    public virtual void Start()
    {
        Freeze();
        Invoke(nameof(UnFreeze), spawnFreezeTime);
    }
    public virtual void FixedUpdateCall() // FixedUpdate()
    {
        
    }
    
    public virtual void UpdateCall() // Update()
    {

    }
    
    public virtual void UpdateBehaviour() // Called in intervals
    {
        if (IsServer)
        {
            Transform target = GetComponent<PathFindingGroundMovement>().GetTargetTransform();
            if (target && target.TryGetComponent(out Player player) && NetworkObject.OwnerClientId != player.OwnerClientId)
            {
                NetworkObject.ChangeOwnership(player.OwnerClientId);
                Debug.Log("Changed ownership");
            }
        }
        if (IsOwner)
        {
            NetworkObject target = null;
            if (this is GroundEnemy && ((GroundEnemy)this).movement.GetTargetTransform() != null)
            {
                target = ((GroundEnemy)this).movement.GetTargetTransform().GetComponent<NetworkBehaviour>().NetworkObject;
            }
            networkData.Value = new EnemyNetworkData()
            {
                position = transform.position,
                velocity = GetComponent<Rigidbody>().linearVelocity,
                currentState = (int)state,
                stateTimmer = timmer,
                time = NetworkManager.Singleton.LocalTime.TimeAsFloat,
                targetObject = target
            };
        }
    }
    
    //Other behaviour calls
    public bool CheckForTargets(List<EnemySO.HealthWeightParams> healthFilterList, out Health closestHealth)
    {
        closestHealth = null;
        float closestDistance = float.PositiveInfinity;
        foreach (EnemySO.HealthWeightParams healthWeight in healthFilterList)
        {
            foreach (Health health in EnemyManager.active.healthTypeArray[(int)healthWeight.healthType])
            {
                bool canAttackTrain = PlayerTrain.active == null || PlayerTrain.active.GetPlayerDist() <= enemyInfo.playerDistanceFromTrainToAttack;
                
                if (health == this.health || health.GetHealth() <= 0f || (healthWeight.healthType == HealthType.Train && canAttackTrain == false))
                    continue;

                float distance = Vector3.Distance(transform.position, health.transform.position);

                if (distance <= healthWeight.sightDistance && (distance / healthWeight.importanceWeight) < closestDistance)
                {
                    closestHealth = health;
                    closestDistance = distance / healthWeight.importanceWeight;
                }
            }
        }

        if (closestHealth)
            return true;
        return false;
    }

    public List<Health> GetHealthInRadius(float radius, HealthType healthType)
    {
        List<Health> healthList = new List<Health>();
        foreach (Health health in EnemyManager.active.healthTypeArray[(int)healthType])
        {
            float distance = Vector3.Distance(transform.position, health.transform.position);

            if (distance <= radius && health != this.health)
            {
                healthList.Add(health);
            }
        }
        return healthList;
    }
    public virtual void Anger(Transform target, float intrestTime)
    {
        state = State.Attacking;
        timmer = intrestTime;
    }

    public virtual bool DoIWantToAttack()
    {
        return false;
    }
    public virtual bool IsInPack()
    {
        return false;
    }
    public void SetState(State state)
    {
        this.state = state;
    }

    public virtual void Freeze()
    {
        
    }
    public virtual void UnFreeze()
    {
        
    }
    public State GetState()
    {
        return state;
    }
    public Vector3 GetRandomPosition(float radius = 4f)
    {
        int safety = 5;
        while (safety > 0)
        {
            Vector3 samplePos = transform.position + GetRandomOffset(radius);

            if (NavMesh.SamplePosition(samplePos,out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            safety--;
        }

        Debug.LogWarning("Exeeded safety while loop limit");
        return transform.position + GetRandomOffset(radius);
    }
    public Vector3 GetRandomOffset(float radius = 4f)
    {
        Vector2 randomDir = Random.insideUnitCircle * radius;
        return new Vector3(randomDir.x, 0, randomDir.y);
    }
    public void OnDestroy()
    {
        EnemyManager.active.RemoveEnemy(this);
    }
}
