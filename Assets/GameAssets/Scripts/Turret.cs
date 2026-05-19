using System;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    
    //Variables
    [SerializeField] 
    private Transform rotateTransform;
    [SerializeField] 
    private Transform bulletSpawnTransform;
    [SerializeField] 
    private string shootSound;
    [Header("Stats")] 
    [SerializeField] 
    private float range;
    [SerializeField] 
    private float shootCoolDown;
    [SerializeField] 
    private float damage = 5f;
    [SerializeField] 
    private float rotateSpeed = 25f;
    [SerializeField] 
    private float maxAngle = 5f;
    [Header("Building")]
    [SerializeField] 
    public Material defaultMat;
    [SerializeField] 
    public Material hologramMatRed;
    [SerializeField] 
    public Material hologramMatGreen;
    [SerializeField] 
    private List<MeshRenderer> meshRendererList;
    
    //Run time
    private bool active;
    private float coolDown;
    private Enemy target;

    private void Update()
    {
        if(!active)
            return;

        target = GetClosestVisibleEnemy();
        
        //Rotate
        Quaternion targetRot;
        if(target != null)
            targetRot = Quaternion.LookRotation(target.centerTransform.position - transform.position);
        else
            targetRot = transform.rotation;
        
        rotateTransform.rotation = Quaternion.RotateTowards(rotateTransform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        
        //Shooting
        coolDown -= Time.deltaTime;
        if (target != null)
        {
            if (coolDown <= 0 && Vector3.Angle(rotateTransform.forward, (target.centerTransform.position - transform.position).normalized) <= maxAngle)
            {
                BulletManager.active.ShootBullets(bulletSpawnTransform.position, bulletSpawnTransform.forward, 1, damage, 0f);
                SoundManager.active.PlayAtPos(bulletSpawnTransform.position, shootSound);
                coolDown = shootCoolDown;
            }
        }
    }

    private Enemy GetClosestVisibleEnemy()
    {
        float currentMinDistance = float.PositiveInfinity;
        Enemy currentClosest = null;
        foreach (Enemy enemy in EnemyManager.active.GetEnemies())
        {
            float distance = Vector3.Distance(transform.position, enemy.centerTransform.position);
            if (distance < currentMinDistance)
            {
                //Check if visible
                if (Physics.Raycast(transform.position, enemy.centerTransform.position - transform.position, out RaycastHit hit, range))
                {
                    //Debug.Log(hit.transform);
                    if (hit.transform == enemy.transform)
                    {
                        currentMinDistance = distance;
                        currentClosest = enemy;
                    }
                }
            }
        }
        //Debug.Log(currentClosest);
        return currentClosest;
    }
    public void SetMaterialTo(Material material)
    {
        foreach (var renderer in meshRendererList)
        {
            renderer.sharedMaterial = material;
        }
    }
    public void Activate()
    {
        active = true;
        SetMaterialTo(defaultMat);
    }
}
