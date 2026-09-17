using UnityEngine;

public class MeleeEnemy : GroundEnemy
{
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
                if (CheckForTargets(enemyInfo.HealthWeightParamsList, out Health closestHealth))
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
                    else if(GetTimeStandingStill() > 0.5f)
                    {
                        timmer = 0f;
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
                if (CheckForTargets(enemyInfo.HealthWeightParamsList, out Health closestHealth1))
                {
                    movement.SetTarget(closestHealth1.transform, 0.5f);
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
        
        base.UpdateBehaviour();
    }
}
