using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
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
    
    public virtual void FixedUpdateCall() // FixedUpdate()
    {
        
    }
    
    public virtual void UpdateCall() // Update()
    {
        
    }
    
    public virtual void UpdateBehaviour() // Called in intervals
    {

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
                if (health == this.health || health.health <= 0f || (healthWeight.healthType == HealthType.Train && Train.playerTrain.GetPlayerDist() > enemyInfo.playerDistanceFromTrainToAttack))
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
