using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class HealerEnemy : GroundEnemy
{
    [Header("Tether")]
    [SerializeField]
    private float maxHealDistance = 5f;
    [SerializeField]
    private float sightDistance = 15f;
    [SerializeField]
    private int maxLinkedCount = 3;
    [SerializeField]
    private GameObject healLinkPrefab;
    [Header("Healing")] 
    [SerializeField] 
    private float healInterval;
    [SerializeField] 
    private int healAmount;
    [SerializeField]
    private GameObject healParticlePrefab;
    
    private float healTimer;
    
    private List<HealLink> linkList = new List<HealLink>();
    
    private class HealLink
    {
        public Health hostHealth;
        public Health targetHealth;
        public GameObject linkObject;
        
        private LineRenderer lineRenderer;
        
        public HealLink(Health hostHealth, Health targetHealth, GameObject healLinkPrefab)
        {
            this.hostHealth = hostHealth;
            this.targetHealth = targetHealth;
            linkObject = Instantiate(healLinkPrefab);
            lineRenderer = linkObject.GetComponent<LineRenderer>();
            Update();
        }

        public void Update()
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, ((EnemyHealth)hostHealth).GetBehaviour().centerTransform.position + Vector3.up);
            lineRenderer.SetPosition(1, ((EnemyHealth)targetHealth).GetBehaviour().centerTransform.position + Vector3.up * 0.5f);
        }

        public void DestroyLink()
        {
            Destroy(linkObject);
        }
    }

    public override void UpdateCall()
    {
        base.UpdateCall();
        
        for (int j = 0; j < linkList.Count; j++)
        {
            if (linkList[j].targetHealth.GetHealth() <= 0f)
            {
                linkList[j].DestroyLink();
                linkList.RemoveAt(j);
                j--;
            }
            else
                linkList[j].Update();
        }

        healTimer += Time.deltaTime;
        if (healTimer >= healInterval)
        {
            healTimer -= healInterval;

            foreach (HealLink link in linkList)
            {
                if (link.targetHealth.GetHealth() < link.targetHealth.maxHealth)
                {
                    link.targetHealth.TakeDamage(-healAmount);
                    GameObject effect = Instantiate(healParticlePrefab, link.targetHealth.transform);
                    effect.transform.position = ((EnemyHealth)link.targetHealth).GetBehaviour().centerTransform.position;
                    Destroy(effect, 5f);
                }
            }
            
        }
    }

    public override void UpdateBehaviour()
    {
        bool targetReached = movement.HasReachedTarget();
        //Debug.Log(targetReached);
        List<Health> healthList = GetHealthInRadius(maxHealDistance, HealthType.Enemy);
        bool sorted = false;
        while (sorted == false)
        {
            sorted = true;
            for (int i = 0; i < healthList.Count; i++)
            {
                Debug.Log(((EnemyHealth)healthList[i]).GetBehaviour().GetType() + ", " + ((((EnemyHealth)healthList[i]).GetBehaviour() as HealerEnemy) != null));
                if (healthList[i].CanBeSeenBy(typeof(HealerEnemy)) == false || healthList[i].GetHealth() <= 0f || (((EnemyHealth)healthList[i]).GetBehaviour() as HealerEnemy) != null)
                {
                    healthList.RemoveAt(i);
                    sorted = false;
                    i--;
                }
                //Factor in health and distance. hp * dist
                else if (i+1 < healthList.Count && healthList[i].GetHealth() * Vector3.Distance(transform.position,healthList[i].transform.position) > healthList[i + 1].GetHealth() * Vector3.Distance(transform.position,healthList[i+1].transform.position) )
                {
                    (healthList[i], healthList[i + 1]) = (healthList[i + 1], healthList[i]);
                    sorted = false;
                }
            }
        }

        int linkCount = Mathf.Min(healthList.Count, maxLinkedCount);
        List<HealLink> newLinkList = new List<HealLink>();
        for (int i = 0; i < linkCount; i++)
        {
            bool createNew = true;
            for (int j = 0; j < linkList.Count; j++)
            {
                if (healthList[i] == linkList[j].targetHealth)
                {
                    createNew = false;
                    newLinkList.Add(linkList[j]);
                }
            }

            if (createNew)
            {
                newLinkList.Add(new HealLink(health, healthList[i], healLinkPrefab));
            }
        }
        
        for (int j = 0; j < linkList.Count; j++)
        {
            if (newLinkList.Contains(linkList[j]) == false)
            {
                linkList[j].DestroyLink();
            }
        }
        
        linkList = newLinkList;
        
        switch (state)
        {
            case State.Idle:
               
                if (healthList.Count > 0)
                {
                    //Follow least healthy nearby enemy
                    movement.SetTarget(healthList[0].transform, maxHealDistance * 0.5f);
                    if (movement.HasReachedTarget() && GetTimeStandingStill() >= Random.Range(1f,1.5f))
                    {
                        movement.SetTarget(healthList[0].transform.position + GetRandomOffset(1.5f), maxHealDistance * 0.5f);
                        ResetTimeStandingStill();
                    }
                }
                else
                {
                    if (CheckForTargets(new List<EnemySO.HealthWeightParams>() { new EnemySO.HealthWeightParams { healthType = HealthType.Enemy, importanceWeight = 1f, sightDistance = sightDistance } }, out Health closestHealth))
                    {
                        movement.SetTarget(closestHealth.transform, maxHealDistance * 0.5f);
                    }
                    //Roam around
                    else
                    {
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
                            movement.SetTarget(GetRandomPosition(4f), 1f);
                            timmer = Random.Range(1.5f, 6f);
                        }
                    }
                    
                }

                break;
            default:
                state = State.Idle;
                break;
        }
        
        base.UpdateBehaviour();
    }

    protected override void OnIsDestroying()
    {
        base.OnIsDestroying();
        foreach (HealLink link in linkList)
            link.DestroyLink();
    }
}
