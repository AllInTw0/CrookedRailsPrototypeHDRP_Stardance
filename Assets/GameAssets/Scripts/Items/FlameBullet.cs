using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class FlameBullet : Bullet
{
    [SerializeField]
    public float velocity;
    [SerializeField]
    private Rigidbody rb;
    [SerializeField] 
    private float gravity;
    [SerializeField] 
    private float drag = 0.9f;
    [SerializeField]
    private float timer;
    [SerializeField]
    private GameObject flamePrefab;
    [Header("Damage")]
    [SerializeField] private float damage = 5f;
    [SerializeField] private float tickDamage = 1f;
    [SerializeField] private float tickDamageDuration = 5f;
    [Header("Sound")]
    [SerializeField] private string igniteSound;
    private bool canSpawnFlames = false;
    private ShooterType shooterType;
    private bool doDamage = false;
    public override void Initialize(ShooterType shooterType, bool doDamage)
    {
        canSpawnFlames = true;
        this.doDamage = doDamage;
        rb.linearVelocity = transform.forward * velocity;
        this.shooterType = shooterType;
    }
    
    private void Update()
    {
        timer -= Time.deltaTime;
        if(timer <= 0f)
        {
            foreach (ParticleSystem particleSystem in gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                particleSystem.Stop();
                particleSystem.transform.SetParent(null);
                Destroy(particleSystem.gameObject,5f);
            }
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (rb.isKinematic == false)
        {
            rb.AddForce(new Vector3(0f, gravity * Time.fixedDeltaTime, 0f), ForceMode.Acceleration);
            rb.linearVelocity *= drag;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("[Flame] terrain");
        if (canSpawnFlames)
        {
            Instantiate(flamePrefab, collision.contacts[0].point, Quaternion.Euler(0f,Random.Range(0f, 360f),0f));
            SoundManager.active.PlayAtPos(collision.contacts[0].point, igniteSound);
            timer = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        
        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            
            Health.Debuff fireDebuff = health.GetDebuff(DebuffType.Fire);
            if (fireDebuff != null)
            {
                fireDebuff.duration = tickDamageDuration;
                fireDebuff.value = damage;
            }
            else
                health.AddDebuff(new Health.Debuff(DebuffType.Fire, tickDamageDuration, tickDamage, doDamage));

            if (doDamage)
            {
                HitSound.active.HandleHitSound(health,health.TakeDamage(damage), health.transform.position + Vector3.up);
                SoundManager.active.PlayAtPos(health.transform.position + Vector3.up, igniteSound);
            }
            if(canSpawnFlames == false)
                timer = 0f;
        }
        //Debug.Log($"[Flame] trigger {other.gameObject.name}, {health}");
    }
}
