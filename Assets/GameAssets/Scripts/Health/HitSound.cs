using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HitSound : MonoBehaviour
{
    public static HitSound active;
    
    //Variables
    [SerializeField] 
    private string hitSoundName;
    [SerializeField] 
    private RectTransform damageTextParent;
    [SerializeField] 
    private RectTransform damageTextPrefab;
    [SerializeField] 
    private float hitUpdateLifeLenght;
    [SerializeField] 
    private float hitLifeLenght;
    //Run time
    private List<HealthHitInfo> hitInfoList = new List<HealthHitInfo>();

    private void Start()
    {
        active = this;
    }

    private void LateUpdate()
    {
        for (int i = 0; i < hitInfoList.Count; i++)
        {
            HealthHitInfo hitInfo = hitInfoList[i];
            
            //Update Pos
            PlayerCamera.active.WorldPosToUI(hitInfo.position, out Vector3 screenPos, out bool onScreen);
            if (onScreen == false)
            {
                if(hitInfo.damageText.gameObject.activeSelf == true)
                    hitInfo.damageText.gameObject.SetActive(false);
            }
            else
            {
                if(hitInfo.damageText.gameObject.activeSelf == false)
                    hitInfo.damageText.gameObject.SetActive(true);
                hitInfo.damageText.transform.position = screenPos;
            }
            
            if (hitInfo.updateNextFrame)
            {
                //Update Other
                hitInfo.damageText.text = "-"+hitInfo.totalDamage;
                SoundManager.active.PlayAtPos(PlayerMovement.active.transform.position,hitSoundName);
                hitInfo.updateNextFrame = false;
                hitInfo.timeSinceUpdate = 0f;
            }
            else
            {
                hitInfo.timeSinceUpdate += Time.deltaTime;
                if (hitInfo.timeSinceUpdate > hitLifeLenght)
                {
                    Destroy(hitInfo.damageText.gameObject);
                    hitInfoList.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public void HandleHitSound(Health targetHealth, float damage, Vector3 position)
    {
        foreach (var hitInfo in hitInfoList)
        {
            if (hitInfo.healthScript == targetHealth && hitInfo.timeSinceUpdate <= hitUpdateLifeLenght)
            {
                hitInfo.totalDamage += damage;
                hitInfo.updateNextFrame = true;
                hitInfo.position = position;
                return;
            }
        }
        //Didn't find hit info. Create new one
        HealthHitInfo newHitInfo = new HealthHitInfo();
        newHitInfo.healthScript = targetHealth;
        newHitInfo.position = position;
        newHitInfo.totalDamage = damage;
        newHitInfo.updateNextFrame = true;
        newHitInfo.damageText = Instantiate(damageTextPrefab, damageTextParent).GetComponent<TMP_Text>();
        
        hitInfoList.Add(newHitInfo);
    }
}

public class HealthHitInfo
{
    public Health healthScript;
    public Vector3 position;

    public float totalDamage;
    public TMP_Text damageText;
    
    public float timeSinceUpdate;
    public bool updateNextFrame;
}