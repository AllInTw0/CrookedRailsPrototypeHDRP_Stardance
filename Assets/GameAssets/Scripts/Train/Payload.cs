using System;
using UnityEngine;

public class Payload : Train
{
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

    private float stunTime;
    private void Start()
    {
        payloadHealth.onTakeDamage.AddListener((dmg, vector3) =>
        {
            stunTime = onDamageStunTime;
        });
    }

    public override void Update()
    {
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
    }
}
