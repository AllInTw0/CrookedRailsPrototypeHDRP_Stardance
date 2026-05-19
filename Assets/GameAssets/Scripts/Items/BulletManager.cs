using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public enum ShooterType
{
    Player,
    Sentry
}
public class BulletManager : MonoBehaviour
{
    public static BulletManager active;

    [SerializeField] 
    private GameObject burstParticle;
    [SerializeField] 
    private GameObject bulletParticle;
    [SerializeField] 
    private GameObject impactParticle;
    [SerializeField] 
    private GameObject shortImpactParticle;
    [Header("Explosions")]
    [SerializeField]
    private string explosionSound;
    [SerializeField]
    private GameObject explosionParticle;
    [SerializeField]
    private LayerMask explosionLayerMask;
    [Header("force")]
    [SerializeField]
    private float forcePerDamage;
    private void Start()
    {
        active = this;
    }
    
    public void ShootBullets(Vector3 start, Vector3 dir, int count,float bulletDamage, float spread, List<HealthType> healthTypeFilterList = null, ShooterType shooterType = ShooterType.Player)
    {
        
        SpawnBurstEffect(start,dir);
        for (int i = 0; i < count; i++)
        {
            Vector3 spreadDir = dir + new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread), Random.Range(-spread, spread));
            if (Physics.Raycast(start,spreadDir, out RaycastHit hit, 100f))
            {
                SpawnBulletEffect(start,hit.point,hit.distance);
                

                if (hit.transform.TryGetComponent(out Health healthScript) && (healthTypeFilterList == null || healthTypeFilterList.Contains(healthScript.healthType)))
                {
                    float damageDone = healthScript.TakeDamage(bulletDamage, spreadDir.normalized * (bulletDamage * forcePerDamage));
                    HitSound.active.HandleHitSound(healthScript, damageDone, hit.point);
                    SpawnImpactEffect(hit.point,hit.normal,true);
                    if(damageDone > 0 && healthScript.health == 0)
                    {
                        Debug.Log("Killed! " + shooterType);
                        if (shooterType == ShooterType.Player)
                            GameStateManager.AddToStatistic("Kills (Player)", 1);
                        else if (shooterType == ShooterType.Sentry)
                            GameStateManager.AddToStatistic("Kills (Sentry)", 1);
                    }
                }
                else
                {
                    SpawnImpactEffect(hit.point,hit.normal,false);
                }
            }
            else
            {
                SpawnBulletEffect(start,spreadDir.normalized * 100f,100f); 
            }
        }
        
    }
    public void ShootPrefab(Vector3 start, Vector3 dir, GameObject prefab, int count, float spread, ShooterType shooterType = ShooterType.Player)
    {

        SpawnBurstEffect(start, dir);
        for (int i = 0; i < count; i++)
        {
            Vector3 spreadDir = dir + new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread), Random.Range(-spread, spread));
            GameObject copy = Instantiate(prefab);
            copy.transform.position = start;
            copy.transform.LookAt(start + spreadDir);
            if (copy.TryGetComponent(out Bullet bulletScript)) bulletScript.Initialize(shooterType);
        }

    }
    public RaycastHit ShootInvisibleBullet(Vector3 start, Vector3 dir, float damage, float range)
    {
        if (Physics.Raycast(start,dir, out RaycastHit hit, range))
        {
            Health healthScript = hit.transform.GetComponent<Health>();
            if (healthScript != null)
            {
                HitSound.active.HandleHitSound(healthScript, healthScript.TakeDamage(damage, dir.normalized * (damage * forcePerDamage)), hit.point);
                SpawnImpactEffect(hit.point,hit.normal,true);
            }
            else
            {
                SpawnImpactEffect(hit.point,hit.normal,false);
            }

            return hit;
        }

        return new RaycastHit();
    }

    public void SpawnExplosion(Vector3 position, float damage, float range, float force, Transform hitTransform = null, List<HealthType> healthTypeFilterList = null, ShooterType shooterType = ShooterType.Player)
    {
        GameObject explosion = Instantiate(explosionParticle);
        explosion.transform.position = position;
        Destroy(explosion, 10f);

        SoundManager.active.PlayAtPos(position, explosionSound);

        Collider[] colliderArray = Physics.OverlapSphere(position, range, explosionLayerMask);

        foreach (Collider collider in colliderArray)
        {
            Vector3 dir = collider.transform.position - position;
            if (collider.TryGetComponent(out Health health) && (healthTypeFilterList == null || healthTypeFilterList.Contains(health.healthType)))
            {
                float dist = 0f;
                if (hitTransform != collider.transform) {
                    if (collider.TryGetComponent(out Enemy enemy))
                        dist = Vector3.Distance(position, enemy.centerTransform.position);
                    else
                        dist = Vector3.Distance(position, collider.transform.position) - 2f;
                }
                dist = Mathf.Clamp(dist, 0f, range * 0.9f);
                float distDamage = Mathf.Round(damage - (dist / range) * damage);
                float damageDone = health.TakeDamage(distDamage, dir.normalized * (distDamage * forcePerDamage) + Vector3.up * ((distDamage * forcePerDamage)));
                HitSound.active.HandleHitSound(health, damageDone, collider.transform.position);
                if (damageDone > 0 && health.health == 0)
                {
                    Debug.Log("Killed! " + shooterType);
                    if (shooterType == ShooterType.Player)
                        GameStateManager.AddToStatistic("Kills (Player)", 1);
                    else if (shooterType == ShooterType.Sentry)
                        GameStateManager.AddToStatistic("Kills (Sentry)", 1);
                }
            }
            if (collider.TryGetComponent(out Rigidbody rb))
            {
                if (rb.isKinematic)
                {
                    if (collider.TryGetComponent(out Item item))
                    {
                        item.AddExplosionForce(position - Vector3.down * 0.5f, force, range + 0.5f);
                    }
                }
                else
                {
                    rb.AddExplosionForce(force, position - Vector3.down * 0.5f, range + 0.5f);
                }
            }
        }
    }

    private void SpawnBurstEffect(Vector3 position, Vector3 dir)
    {
        GameObject burst = Instantiate(burstParticle);
        burst.transform.position = position;
        burst.transform.LookAt(position + dir);

        Destroy(burst,3f);
    }
    private void SpawnBulletEffect(Vector3 start, Vector3 end, float distance)
    {
        GameObject bullet = Instantiate(bulletParticle);
        bullet.transform.position = start;
        bullet.transform.LookAt(end);

        ParticleSystem.ShapeModule shape = bullet.GetComponent<ParticleSystem>().shape;
        shape.position = new Vector3(0f, 0f, distance*0.5f);
        shape.scale = new Vector3(distance*0.5f, 1f, 1f);
        
        Destroy(bullet,3f);
    }
    private void SpawnImpactEffect(Vector3 position, Vector3 normal, bool isShort)
    {
        GameObject impact = Instantiate(isShort? shortImpactParticle : impactParticle);
        impact.transform.position = position;
        impact.transform.LookAt(position + normal);

        Destroy(impact,3f);
    }
}
