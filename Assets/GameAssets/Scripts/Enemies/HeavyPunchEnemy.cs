using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyPunchEnemy : GroundEnemy
{

    [Header("Punch")]
    [SerializeField] 
    private float sightDistance;
    [SerializeField] 
    private float punchVelocity;
    [SerializeField] 
    private float punchDelay;
    [SerializeField] 
    private float stunTime;
    
    private Health targetHealth;

    private Health targetPunchHealth;

    public override void Start()
    {
        base.Start();
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
                if (CheckForTargets(enemyInfo.HealthWeightParamsList, out Health closestHealth))
                {
                    targetHealth = closestHealth;
                    state = State.Attacking;
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
                    targetHealth = closestHealth1;
                    timmer = 5f;
                }
                else
                {
                    timmer -= EnemyManager.active.behaviourRefreshRate;
                    if(timmer <= 0)
                    {
                        state = State.Idle;
                        targetHealth = null;
                        targetPunchHealth = null;
                        break;
                    }
                }

                if (targetPunchHealth == null || targetPunchHealth.GetHealth() <= 0)
                {
                    List<Health> healthList = GetHealthInRadius(sightDistance, HealthType.Enemy);
                    for (int i = 0; i < healthList.Count; i++)
                    {
                        if (healthList[i].CanBeSeenBy(typeof(HeavyPunchEnemy)) == false || (((EnemyHealth)healthList[i]).GetBehaviour() as MeleeEnemy) == null)
                        {
                            healthList.RemoveAt(i);
                            i--;
                        }
                    }
                    bool sorted = false;
                    while (sorted == false)
                    {
                        sorted = true;
                        for (int i = 0; i < healthList.Count-1; i++)
                        {
                            if (Vector3.Distance(targetHealth.transform.position,healthList[i].transform.position) < Vector3.Distance(targetHealth.transform.position,healthList[i+1].transform.position))
                            {
                                (healthList[i], healthList[i + 1]) = (healthList[i + 1], healthList[i]);
                                sorted = false;
                            }
                        }
                    }
                    if(healthList.Count > 0)
                        targetPunchHealth = healthList[0];
                }

                if (targetPunchHealth != null && targetPunchHealth.GetHealth() > 0)
                {
                    Vector3 dir = (targetHealth.transform.position - targetPunchHealth.transform.position).normalized;

                    movement.SetTarget(targetPunchHealth.transform, 0.5f, -dir);
                    if (movement.HasReachedTarget())
                    {
                        targetPunchHealth.UpdateVisibilityCooldown(typeof(HeavyPunchEnemy),4f);
                        GroundEnemy groundEnemy = (GroundEnemy)((EnemyHealth)targetPunchHealth).GetBehaviour();
                        groundEnemy.movement.Freeze();
                        Freeze();
                        
                        state = State.Stunned;
                        timmer = stunTime;
                        movement.SetTarget(targetHealth.transform); // Look
                        
                        animator.SetTrigger("HeavyPunch");
                        StartCoroutine(PunchDelayed(groundEnemy, punchDelay));
                    }
                    else
                    {
                        targetPunchHealth.UpdateVisibilityCooldown(typeof(HeavyPunchEnemy),1f);
                    }
                }
                else
                {
                    targetPunchHealth = null;
                    state = State.Idle;
                }
                
                break;
            case State.Stunned:
                timmer -= EnemyManager.active.behaviourRefreshRate;
                if (timmer <= 0)
                {
                    state = State.Idle;
                    UnFreeze();
                    targetPunchHealth = null;
                }
                break;
            default:
                break;
        }
        
        base.UpdateBehaviour();
    }

    public override void Anger(Transform target, float intrestTime)
    {
        targetHealth = target.GetComponent<Health>();
    }

    public IEnumerator PunchDelayed(GroundEnemy punchEnemy, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (punchEnemy != null && targetHealth != null)
        {
            Vector3 dir3D = (targetHealth.transform.position - punchEnemy.transform.position).normalized;
            Vector2 dir2D = new Vector2(dir3D.x, dir3D.z).normalized;
            
            //Util.SolveTrajectory(punchVelocity, punchEnemy.transform.position, targetHealth.transform.position, false,out float angle);
            float angle = 45f;
            float velocity = Util.SolveVelocityForAngle(angle, punchEnemy.transform.position, targetHealth.transform.position);
            //Debug.Log("PUNCH! " + angle);
            Vector3 punchDir =  new Vector3(dir2D.x, Mathf.Tan(angle * Mathf.Deg2Rad), dir2D.y).normalized;
            punchEnemy.UnFreeze();
            punchEnemy.movement.rb.linearVelocity = punchDir * velocity;
            punchEnemy.Anger(targetHealth.transform,1f);
        }
    }
}
