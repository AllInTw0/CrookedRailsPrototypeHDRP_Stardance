using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class ElectricGuardUpgrade : TrainUpgrade
{
    public Health targetHealth;
    public List<GameObject> EnableDisableGameObjectList = new List<GameObject>();
    [Header("Debuff")] 
    public LayerMask debuffLayerMask;
    public BoxCollider debuffCollider;
    [Header("Stats")]
    public float cooldown = 5f;
    public int damage = 20;
    public float force = 100f;
    public float debuffDuration = 2f;
    [Header("Sound")] 
    public string dischargeString = "ImpactHorn";
    public string chargeString = "PowerUp";
    
    private NetworkVariable<int> timeCooldownNetwork = new NetworkVariable<int>();
    private bool enabled = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Start()
    {
        base.Start();
        targetHealth.onTakeDamage.AddListener((damage, force) =>
        {
            if (damage > 0 && enabled && IsUnlocked())
            {
                Shock();
            }
        });
    }
    
    private void Shock()
    {
        SoundManager.active.PlayAtPos(transform.position,dischargeString);
        
        Collider[] colliderArray = Util.PhysicsBoxColliderOverlap(debuffCollider, debuffLayerMask);
        for (int i = 0; i < colliderArray.Length; i++)
        {
            if (colliderArray[i].transform.TryGetComponent(out Health health))
            {
                health.AddDebuff(new Health.Debuff(DebuffType.Electricity,debuffDuration));
                health.TakeDamage(damage,GetForceVector(health.GetOrigin()));
            }
        }

        timeCooldownNetwork.Value = Mathf.RoundToInt(NetworkManager.LocalTime.TimeAsFloat + cooldown);
    }

    private Vector3 GetForceVector(Vector3 pos)
    {
        Vector2 dir2D = new Vector2(pos.x - transform.position.x, pos.z - transform.position.z).normalized;
        Vector2 trainDirRight2D = new Vector2(transform.right.x, transform.right.z).normalized;
        
        if(Vector2.Dot(dir2D, trainDirRight2D) > 0) // dir2D is to the right of the train
            return new Vector3(trainDirRight2D.x, 1f, trainDirRight2D.y).normalized * force;
        else// dir2D is to the left of the train
            return new Vector3(-trainDirRight2D.x, 1f, -trainDirRight2D.y).normalized * force;
        
    }
    private void Update()
    {
        if(!IsUnlocked())
            return;
        if (enabled != NetworkManager.LocalTime.TimeAsFloat > timeCooldownNetwork.Value)
        {
            enabled = NetworkManager.LocalTime.TimeAsFloat > timeCooldownNetwork.Value;
            for (int i = 0; i < EnableDisableGameObjectList.Count; i++)
            {
                EnableDisableGameObjectList[i].SetActive(enabled);
            }
            if(enabled)
                SoundManager.active.PlayAtPos(transform.position,chargeString);
        }
    }
}
