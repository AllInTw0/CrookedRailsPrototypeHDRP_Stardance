using System;
using System.Collections.Generic;
using UnityEngine;

public class RailCarHealth : Health
{
    
    
    [Header("Railcar Health")]
    [SerializeField]
    private RailCar linkedRailCar;
    [SerializeField]
    private List<EventInteractable> rerailInteractableList;
    [SerializeField]
    private List<GameObject> brokenDownParticles;
    [System.Serializable]
    public class HealthState
    {
        public float startHealthPercent; // If set to 1 and no other state is defined interval is 0%-100% of health
        public List<GameObject> enableOnChange; //Enables things in list and Disables everything else defined in other states

        public bool spawnExplosionOnEnable;
        public float explosionDamage;
        public float explosionRadius;
        public float explosionForce;
        public Transform explosionSpawnPoint;
        public int explosionTypeIndex;
    }
    [SerializeField]
    private List<HealthState> healthStateList;
    private int lastHealthStateIndex = -1;
    
    private bool brokenDown;
    private void Start()
    {
        AddHealthToGlobalList();

        foreach (EventInteractable interactable in rerailInteractableList)
        {
            interactable.interactEvent.AddListener(() =>
            {
                Rerail();
            });

            interactable.gameObject.SetActive(false);

            interactable.objectName = linkedRailCar.railCarSO.GetName();
            interactable.actionName = "Rerail";

            interactable.timeToInteract = 4.5f;
        }
        foreach (GameObject obj in brokenDownParticles)
        {
            obj.SetActive(false);
        }
        
        
        onTakeDamage.AddListener((float damage, Vector3 force) =>
        {
            UpdateHealthState();
        });
        UpdateHealthState();
    }
    private void Update()
    {
        UpdateCall();
    }

    private void UpdateHealthState()
    {
        if(healthStateList.Count == 0) return;
        
        //Find current state 
        int currentState = -1;
        for (int i = 0; i < healthStateList.Count; i++)
        {
            float startHealth = healthStateList[i].startHealthPercent * maxHealth;
            float endHealth = -1;
            if(i+1 < healthStateList.Count)
                endHealth =  healthStateList[i+1].startHealthPercent * maxHealth;
            
            if (GetHealth() <= startHealth && (GetHealth() > endHealth || (endHealth == -1 && GetHealth() >= 0f)))
            {
                currentState = i;
                break;
            }
        }

        if (currentState == -1)
        {
            Debug.LogWarning("Couldnt Find State");
            return;
        }
        
        if(lastHealthStateIndex == currentState)
            return;
        
        for(int i = 0; i < healthStateList.Count; i++)
        {
            foreach (GameObject obj in healthStateList[i].enableOnChange)
            {
                obj.SetActive(currentState == i);
            }

            if (currentState == i && healthStateList[i].spawnExplosionOnEnable)
            {
                BulletManager.active.SpawnExplosion(healthStateList[i].explosionSpawnPoint.position,healthStateList[i].explosionDamage,healthStateList[i].explosionRadius,healthStateList[i].explosionForce, explosionParticleIndex: healthStateList[i].explosionTypeIndex);
            }
        }

        lastHealthStateIndex = currentState;
    }
    public override void HealthReachedZero(Vector3 force = default)
    {
        if (brokenDown) return;
        
        brokenDown = true;
        Derail();

        base.HealthReachedZero();
    }

    public void Rerail()
    {
        foreach (EventInteractable interactable in rerailInteractableList)
        {
            interactable.gameObject.SetActive(false);
        }
        linkedRailCar.derailed = false;
    }
    public void Derail()
    {
        foreach (EventInteractable interactable in rerailInteractableList)
        {
            interactable.gameObject.SetActive(true);
        }

        linkedRailCar.derailed = true;

        if (brokenDown)
        {
            foreach (GameObject obj in brokenDownParticles)
            {
                obj.SetActive(true);
            }
        }
    }

    public void ResetHealth()
    {
        Rerail();
        brokenDown = false;
        TakeDamage(-999999999); //health = maxHealth;
        foreach (GameObject obj in brokenDownParticles)
        {
            obj.SetActive(false);
        }
    }
    public bool IsBroken()
    {
        return brokenDown;
    }
}
