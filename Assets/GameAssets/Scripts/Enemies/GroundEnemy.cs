using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(PathFindingGroundMovement))]
public class GroundEnemy : Enemy
{
    private PathFindingGroundMovement movement;
    
    
    //Animation
    public float walkCycleSpeedMult;
    public float rotationSpeed;
    [Header("Spawning")]
    public float spawnFreezeTime;

    private void Start()
    {
        movement = GetComponent<PathFindingGroundMovement>();
        Freeze();
        Invoke(nameof(UnFreeze), spawnFreezeTime);
    }

    public override void FixedUpdateCall()
    {
        movement.FixedUpdateMovement();
    }
    public override void UpdateCall()
    {
        //movement.UpdateNavigationBehaviour();
        movement.UpdateNavMovement();
        Animate();
    }
    public override void UpdateBehaviour()
    {
        bool targetReached = movement.HasReachedTarget();
        //Debug.Log(targetReached);

        switch (state)
        {
            case State.Idle:
                //Pack handling
                if(IsInPack() == false)
                {
                    foreach (EnemyPack pack in EnemyManager.active.activePackList)
                    {
                        float distance = Vector3.Distance(transform.position, pack.centerPos);
                        if(distance <= pack.affectRadius)
                        {
                            movement.SetTarget(pack, GetRandomOffset(pack.sizeRadius), 1f);
                        }
                    }
                }

                //Check for nearby targets
                if (CheckForTargets(out Health closestHealth))
                {
                    if (IsInPack()) movement.targetPack.AngerPack(closestHealth.transform);
                    else Anger(closestHealth.transform,5f);
                }
                else
                {
                    //Roam around
                    if (targetReached)
                    {
                        timmer -= EnemyManager.active.behaviourRefreshRate;
                    }
                    if (timmer <= 0)
                    {
                        if (IsInPack()) movement.SetTarget(movement.targetPack, GetRandomOffset(movement.targetPack.sizeRadius), 1f);
                        else movement.SetTarget(GetRandomPosition(4f), 1f);
                        timmer = Random.Range(1.5f, 6f);
                    }
                }

                break;
            case State.Attacking:
                //Attack player / train
                if (CheckForTargets(out Health closestHealth1))
                {
                    movement.SetTarget(closestHealth1.transform, 1f);
                    timmer = 5f;
                }
                else
                {
                    timmer -= EnemyManager.active.behaviourRefreshRate;
                    if(timmer <= 0)
                    {
                        state = State.Idle;

                    }
                }
                    break;
            default:
                break;
        }

        movement.UpdateNavigationBehaviour();
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
    public override void Anger(Transform target, float intrestTime)
    {
        base.Anger(target, intrestTime);
        movement.SetTarget(target, 1f);
    }
    public override bool DoIWantToAttack()
    {
        return movement.distanceFromTarget <= 5f;
    }

    public override bool IsInPack()
    {
        return movement.IsInPack();
    }

    public override void Freeze()
    {
        movement.Freeze();
    }

    public override void UnFreeze()
    {
        movement.UnFreeze();
    }

    private void Animate()
    {
        animator.SetFloat("Velocity", movement.velocity);
        animator.SetFloat("SpeedMult", movement.velocity * walkCycleSpeedMult);
        if(movement.targetDirVector != Vector3.zero)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(movement.targetDirVector.x,0, movement.targetDirVector.z)), Time.deltaTime * rotationSpeed);
    }
}
