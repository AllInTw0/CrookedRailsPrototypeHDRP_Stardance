using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RailCarHealth : Health
{
    
    
    [Header("Railcar Health")]
    [SerializeField]
    private RailCar linkedRailCar;
    [SerializeField]
    private List<EventInteractable> rerailInteractableList;
    [SerializeField]
    private List<GameObject> brokenDownParticles = new List<GameObject>();
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
    
    private NetworkVariable<bool> derailed = new(writePerm:NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> brokenDown = new(writePerm:NetworkVariableWritePermission.Server);

    public void SetDerailed(bool _derailed)
    {
        derailed.Value = _derailed;
    }
    public bool IsDerailed()
    {
        return derailed.Value;
    }
    public bool IsBroken()
    {
        return brokenDown.Value;
    }
    private void OnDerailedChanged(bool previous, bool current)
    {
        foreach (EventInteractable interactable in rerailInteractableList)
        {
            interactable.gameObject.SetActive(current);
        }

        if (current)
        {
            Override title = new Override("Title", OverrideType.Text, "WARNING");
            Override message = new Override("Message", OverrideType.Text, "Train DERAILED!");
            Override subText = new Override("SubText", OverrideType.Text, linkedRailCar.railCarSO.GetName());
            MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Message", new List<Override>() { title, message, subText }));
        }
    }

    private void OnBrokenDownChanged(bool previous, bool current)
    {
        foreach (GameObject obj in brokenDownParticles)
        {
            obj.SetActive(current);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        derailed.OnValueChanged += OnDerailedChanged;
        brokenDown.OnValueChanged += OnBrokenDownChanged;
    }

    private void Start()
    {
        AddHealthToGlobalList();

        foreach (EventInteractable interactable in rerailInteractableList)
        {
            interactable.interactEventServer.AddListener((ulong clientID) =>
            {
                derailed.Value = false;
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
            float startHealth = healthStateList[i].startHealthPercent * GetMaxHealth();
            float endHealth = -1;
            if(i+1 < healthStateList.Count)
                endHealth =  healthStateList[i+1].startHealthPercent * GetMaxHealth();
            
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
                obj.SetActive(false);
            }
        }
        foreach (GameObject obj in healthStateList[currentState].enableOnChange)
        {
            obj.SetActive(true);
        }
        
        if (healthStateList[currentState].spawnExplosionOnEnable)
        {
            BulletManager.active.SpawnExplosion(healthStateList[currentState].explosionSpawnPoint.position,healthStateList[currentState].explosionDamage,healthStateList[currentState].explosionRadius,healthStateList[currentState].explosionForce, explosionParticleIndex: healthStateList[currentState].explosionTypeIndex, doDamage: IsServer);
        }
        
        lastHealthStateIndex = currentState;
    }
    public override void HealthReachedZero(Vector3 force = default)
    {
        if (IsBroken()) return;

        if (IsServer)
        {
            brokenDown.Value = true;
            derailed.Value = true;
        }

        base.HealthReachedZero();
    }

    public void ResetHealth()
    {
        derailed.Value = false;
        brokenDown.Value = false;
        TakeDamage(-999999999); //health = maxHealth;
    }
}
