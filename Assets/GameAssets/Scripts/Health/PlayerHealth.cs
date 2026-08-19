using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHealth : Health
{
    [Header("Player")] 
    [SerializeField] 
    private GameObject ragdollPrefab;
    [NonSerialized] 
    public Player player;
    [Header("Water")]
    [SerializeField]
    private float drowningDelay;
    [SerializeField]
    private float drowningDamageDelay;
    [SerializeField]
    private float drowningDamage;
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
        isAlive = true;
        AddHealthToGlobalList();
        onTakeDamage.AddListener((float damage, Vector3 force) => {
            if(damage > 0 && player == Player.localPlayer)
            {
                Fade.hitFade.FadeTo(targetAlpha, fadeInDuration, 0);
                Fade.hitFade.FadeTo(0f, fadeOutDuration, fadeInDuration);
            }
        });
    }

    private void Update()
    {
        UpdateCall();
        
        if(IsOwner == false)
            return;
        
        StatUI.active.UpdateHealth(GetHealth(), maxHealth);
        
        if (player.movement.IsSubmergedInWater())
        {
            timeUnderWater += Time.deltaTime;

            if(timeUnderWater >= drowningDelay)
            {
                timeSinceDammageTick += Time.deltaTime;
                if(timeSinceDammageTick >= drowningDamageDelay)
                {
                    TakeDamage(drowningDamage, Vector3.up);
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
        ragdoll = SpawnRagdoll(player.avatar.animator.gameObject, ragdollPrefab, force);
        if (IsOwner)
        {
            player.inventory.UnEquipAll();
            player.movement.EnableNoclip();
        }

        player.avatar.Hide();

        if (IsOwner)
        {
            InventoryUI.active.Hide();
            SpectateUI.active.Enable();
            StatUI.active.Hide();
        }

        isAlive = false;

        
        if(IsServer && Player.GetDeadPlayers().Count == Player.playerList.Count)
            GameStateManager.GameOver();
    }
    [Rpc(SendTo.Everyone)]
    public void ReviveRpc()
    {
        Destroy(ragdoll);
        isAlive = true;
        
        player.avatar.UnHide();
        
        if (IsOwner)
        {
            player.movement.DisableNoclip();
        }
        
        if (IsOwner)
        {
            InventoryUI.active.UnHide();
            SpectateUI.active.Disable();
            StatUI.active.UnHide();
        }

    }
}
