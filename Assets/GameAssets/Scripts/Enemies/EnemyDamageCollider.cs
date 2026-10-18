using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyDamageCollider : MonoBehaviour
{
    [SerializeField] 
    private Enemy enemyBehaviour;
    [SerializeField]
    private float xRayTime = 3f;
    [SerializeField] 
    private float coolDown = 0.2f;
    [SerializeField] 
    private float freezeTime = 0.1f;
    [SerializeField] 
    private float attackDelay = 0.1f;
    [SerializeField]
    private float attackDamage = 5f;
    [SerializeField]
    private float attackForce = 2.5f;
    [SerializeField] 
    private string attackSoundName;
    [SerializeField] 
    private string attackImpactSoundName;
    [SerializeField]
    private List<HealthType> healthTypeFilterList;
    //Run Time
    private float coolDownTime;

    private void Update()
    {
        coolDownTime -= Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if (enemyBehaviour.DoIWantToAttack() == false) return;

        if (coolDownTime < 0f)
        {
            Health healthScript = other.GetComponent<Health>();
            if (healthScript != null && healthScript.GetHealth() > 0f && healthTypeFilterList.Contains(healthScript.healthType))
            {
                SoundManager.active.PlayAtPos(transform.position,attackSoundName);

                enemyBehaviour.animator.SetTrigger("Attack");

                coolDownTime = coolDown;
                enemyBehaviour.Freeze();
                
                Invoke(nameof(UnFreeze),freezeTime);

                StartCoroutine(DamageDelayed(healthScript, attackDamage,(healthScript.GetOrigin() - transform.position + Vector3.up * 1.5f).normalized * attackForce, attackDelay));
            }
        }
    }

    private void UnFreeze()
    {
        enemyBehaviour.UnFreeze();
    }

    IEnumerator DamageDelayed(Health healthScript, float damage, Vector3 force, float delay)
    {
        enemyBehaviour.EnableXRay(xRayTime);
        yield return new WaitForSeconds(delay);
        
        if(enemyBehaviour.IsOwner) healthScript.TakeDamage(damage,force);
        
        SoundManager.active.PlayAtPos(transform.position,attackImpactSoundName);
        
    }
}
