using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugHelper : MonoBehaviour
{
    [Header("Train")]
    public RailCarSO railCar;
    public CargoSO cargo;
    [Header("Enemy")] 
    public EnemySO enemy;
    [Header("Item")]
    public ItemSO item;
    [Header("Camera shake")]
    public float shakeStrength;
    public float shakeSpring;
    public float shakeDamp;
    [Header("Health")] 
    public bool debugHealth;
    public bool debugEnemies;
    public bool debugTrain;
    private void OnGUI()
    {
        Camera playerCam = PlayerCamera.active.GetComponent<Camera>();
        void Label(Vector3 worldPos, string text)
        {
            PlayerCamera.active.WorldPosToUI(worldPos, out Vector3 screenPos, out bool onScreen);
            if (onScreen)
            {
                //float distance = Vector3.Distance(playerCam.transform.position, worldPos);
                GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, Screen.height, Screen.height), text);
            } 
        }
        GUI.skin.label.clipping = TextClipping.Overflow;
        GUI.skin.label.fontSize = 20;
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        if (debugHealth)
        {
            foreach (List<Health> healthList in EnemyManager.active.healthTypeArray)
            {
                foreach (Health health in healthList)
                {
                    Label(health.transform.position, "h:" +health.GetHealth() + "/" + health.maxHealth +"\n cd:" + health.clientSideDamage);
                }
            }
        }

        if (debugEnemies)
        {
            foreach (List<Health> healthList in EnemyManager.active.healthTypeArray)
            {
                foreach (Health health in healthList)
                {
                    if (health is EnemyHealth)
                    {
                        Label(health.transform.position + Vector3.up, "Owner:" + (Player.localPlayer.OwnerClientId == health.OwnerClientId));
                    }
                }
            }
        }

        if (debugTrain)
        {
            foreach (RailCar railCar in Train.playerTrain.GetConsist())
            {
                Label(railCar.transform.position + Vector3.up, "i: " +railCar.networkData.Value.consistIndex +", t: " + railCar.networkData.Value.trainID);
            }
        }
    }
}
