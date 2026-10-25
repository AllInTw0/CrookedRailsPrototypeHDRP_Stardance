using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Payload : Train
{
    private StructureMaster linkedStructure;
    public static List<Payload> activePayloadList =  new List<Payload>();
    [Header("Payload")] 
    [SerializeField] 
    private Trigger playerTrigger;
    [SerializeField] 
    private float payloadAcceleration;
    [SerializeField] 
    private float payloadDeceleration;
    [SerializeField] 
    private float payloadMaxSpeed;
    [Header("Payload Health")] 
    [SerializeField] 
    private Health payloadHealth;
    [SerializeField] 
    private float onDamageStunTime;
    [Header("Enemy spawning")] 
    [SerializeField]
    private float dangerPerMeter;
    [SerializeField]
    private float distanceForwardSpawnCenter;
    [SerializeField]
    private float spawnRadius;
    [SerializeField]
    private Vector2 waveCooldown;
    [SerializeField]
    private float minDanger;
    [Header("Crane")] 
    [SerializeField]
    private Transform hookPos;
    
    private float stunTime;
    //wave stuff
    private float waveCooldownTimer;
    private float builtUpDanger;
    
    private PayloadCraneRailCar linkedCraneRailCar = null;
    public void LinkCrane(PayloadCraneRailCar crane)
    {
        linkedCraneRailCar = crane;
    }
    public bool CanBePickedUp()
    {
        return linkedCraneRailCar == null && frontTrackSection.GetNextSection() == null;
    }
    public Transform GetHookPos()
    {
        return hookPos;
    }
    private void Start()
    {
        payloadHealth.onTakeDamage.AddListener((dmg, vector3) =>
        {
            stunTime = onDamageStunTime;
        });
        activePayloadList.Add(this);
    }

    public void SetStructure(StructureMaster structureMaster)
    {
        linkedStructure = structureMaster;
    }
    public override void Update()
    {
        if (linkedCraneRailCar != null)
        {
            return;
        }
        base.Update();

        if (playerTrigger.GetColliderCount() > 0 && stunTime <= 0)
        {
            SetAcceleration(payloadAcceleration);
            SetDeceleration(0);
            SetMaxSpeed(payloadMaxSpeed);
        }
        else
        {
            stunTime -= Time.deltaTime;
            SetAcceleration(0);
            SetDeceleration(payloadDeceleration);
            SetMaxSpeed(payloadMaxSpeed);
        }
        
        //Enemy spawning
        builtUpDanger += distanceTravelled * dangerPerMeter;
        waveCooldownTimer -= Time.deltaTime;
        if (builtUpDanger >= minDanger && waveCooldownTimer <= 0)
        {
            TrackManager.active.GetTrackPositionFromProgress(sectionProgress + distanceForwardSpawnCenter, frontTrackSection, out Vector3 spawnCenter);
            List<EnemySpawnPoint> spawnPointList = new List<EnemySpawnPoint>(EnemySpawnPoint.activeSpawnPointDict[linkedStructure]);
            Debug.Log("[Payload] spawning wave! spawnPoints: " + spawnPointList.Count);
            for (int i = 0; i < spawnPointList.Count; i++)
            {
                float distance = Vector3.Distance(spawnCenter, spawnPointList[i].transform.position);
                if (distance > spawnRadius)
                {
                    spawnPointList.RemoveAt(i);
                    i--;
                }
            }

            if (spawnPointList.Count > 0)
            {
                builtUpDanger *= EnemySpawner.GetDangerMultiplier();
                
                float spreadDanger = builtUpDanger / spawnPointList.Count;
                for (int i = 0; i < spawnPointList.Count; i++)
                {
                    spawnPointList[i].SpawnDanger(spreadDanger);
                }

                builtUpDanger = 0f;
                waveCooldownTimer = Random.Range(waveCooldown.x, waveCooldown.y);
            }
            else
            {
                Debug.Log("[Payload] No spawnpoints in range! spawnPoints: " + spawnPointList.Count);
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        activePayloadList.Remove(this);
    }
}
