using System;
using Unity.Netcode;
using UnityEngine;

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
    
    //Behaviour
    public enum State
    {
        Idle,
        Attacking,
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
            transform.position = current.position;
            GetComponent<Rigidbody>().linearVelocity = current.velocity;
            state = (State)current.currentState;

            float timeDiff = NetworkManager.Singleton.LocalTime.TimeAsFloat - current.time; 
            Debug.Log("Time Diff:" + timeDiff);
            timmer = current.stateTimmer - timeDiff;
        }
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
            networkData.Value = new EnemyNetworkData()
            {
                position = transform.position,
                velocity = GetComponent<Rigidbody>().linearVelocity,
                currentState = (int)state,
                stateTimmer = timmer,
                time = NetworkManager.Singleton.LocalTime.TimeAsFloat
                
            };
        }
    }
    
    //Other behaviour calls
    public bool CheckForTargets(out Health closestHealth)
    {
        closestHealth = null;
        float closestDistance = float.PositiveInfinity;
        foreach (EnemySO.HealthWeightParams healthWeight in enemyInfo.HealthWeightParamsList)
        {
            foreach (Health health in EnemyManager.active.healthTypeArray[(int)healthWeight.healthType])
            {
                bool canAttackTrain = Train.playerTrain == null || Train.playerTrain.GetPlayerDist() <= enemyInfo.playerDistanceFromTrainToAttack;
                
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
}
