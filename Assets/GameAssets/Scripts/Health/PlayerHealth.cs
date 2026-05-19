using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHealth : Health
{
    public static PlayerHealth active;
    [Header("Player")] 
    [SerializeField] 
    private GameObject ragdollPrefab;
    [SerializeField] 
    private PlayerAvatar playerAvatar;
    [SerializeField] 
    private PlayerMovement playerMovement;
    [SerializeField] 
    private PlayerCamera playerCamera;
    [SerializeField] 
    private PlayerInventory playerInventory;
    [Header("Water")]
    [SerializeField]
    private float drowningDelay;
    [SerializeField]
    private float drowningDammageDelay;
    [SerializeField]
    private float drowningDammage;
    [Header("HitEffect")]
    [SerializeField]
    private float targetAlpha;
    [SerializeField]
    private float fadeInDuration;
    [SerializeField]
    private float fadeOutDuration;
    //RunTime
    [NonSerialized]
    public GameObject ragdoll;
    [NonSerialized]
    public bool isAlive = true;

    private float timeUnderWater;
    private float timeSinceDammageTick;
    private void Start()
    {
        active = this;
        isAlive = true;
        AddHealthToGlobalList();
        onTakeDamage.AddListener((float dammage, Vector3 force) => {
            if(dammage > 0)
            {
                Fade.hitFade.FadeTo(targetAlpha, fadeInDuration, 0);
                Fade.hitFade.FadeTo(0f, fadeOutDuration, fadeInDuration);
            }
        });
    }

    private void Update()
    {
        StatUI.active.UpdateHealth(health,maxHealth);
        UpdateCall();
        if (playerMovement.IsSubmergedInWater())
        {
            timeUnderWater += Time.deltaTime;

            if(timeUnderWater >= drowningDelay)
            {
                timeSinceDammageTick += Time.deltaTime;
                if(timeSinceDammageTick >= drowningDammageDelay)
                {
                    TakeDamage(drowningDammage, Vector3.up);
                    SoundManager.active.PlayAtPos(transform.position, "Water - Bubbles");
                    timeSinceDammageTick = 0f;
                }
            }
        }
        else
        {
            timeUnderWater = 0f;
            timeSinceDammageTick = 0f;
        }
    }

    public override void HealthReachedZero(Vector3 force = default)
    {
        ragdoll = SpawnRagdoll(playerAvatar.animator.gameObject, ragdollPrefab, force);
        playerInventory.UnEquipAll();
        playerMovement.EnableNoclip();
        playerAvatar.Hide();
        
        InventoryUI.active.Hide();
        SpectateUI.active.Enable();
        StatUI.active.Hide();
        
        isAlive = false;

        GameOverScreen.active.StartGameOver();
    }
}
