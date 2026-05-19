using System.Collections.Generic;
using UnityEngine;

public class Grenade : Bullet
{
    [SerializeField]
    public float velocity;
    [SerializeField]
    private Rigidbody rb;
    [Header("Explosion trigger")]
    [SerializeField]
    private float collisionDrag;
    [SerializeField]
    private float timer;
    [SerializeField]
    private LayerMask setOffLayerMask;
    [Header("Explosion")]
    [SerializeField]
    private float range;
    [SerializeField]
    private float force;
    [SerializeField]
    private float damage;
    [SerializeField]
    private List<HealthType> healthTypeFilter = new List<HealthType>();

    private ShooterType shooterType;
    private bool colliding = false;
    public override void Initialize(ShooterType shooterType)
    {
        rb.linearVelocity = transform.forward * velocity;
        this.shooterType = shooterType;
    }
    private void Update()
    {
        timer -= Time.deltaTime;
        if(timer <= 0f)
        {
            Explode();
        }
    }
    private void LateUpdate()
    {
        if (colliding)
            rb.linearVelocity *= collisionDrag;
        colliding = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //( mask & (1 << layer)) != 0 returns true if mask has the layer
        if ((setOffLayerMask & (1 << collision.gameObject.layer)) != 0)
        {
            Explode(collision.transform);
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        colliding = true;
    }
    private void Explode(Transform hitTransform = null)
    {
        BulletManager.active.SpawnExplosion(transform.position, damage, range, force, hitTransform, healthTypeFilter, shooterType);
        Destroy(gameObject);
    }
}
