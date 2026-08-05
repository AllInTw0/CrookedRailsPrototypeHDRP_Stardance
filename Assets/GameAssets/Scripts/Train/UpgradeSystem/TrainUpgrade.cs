using System;
using Unity.Netcode;
using UnityEngine;

public class TrainUpgrade : NetworkBehaviour
{
    private NetworkVariable<bool> unlocked = new NetworkVariable<bool>();

    public bool IsUnlocked()
    {
        return unlocked.Value;
    }

    public void SetUnlocked(bool value)
    {
        unlocked.Value = value;
    }

    public UpgradeSO linkedUpgradeSO;
    public bool unlockedAtStart;
    
    private void Awake()
    {
        unlocked.OnValueChanged += UnlockedStateChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if(unlockedAtStart)
                SetUnlocked(true);
            else
                UnlockedStateChanged(unlocked.Value, unlocked.Value);
        }
        else
        {
            UnlockedStateChanged(unlocked.Value, unlocked.Value);
        }
    }

    private void UnlockedStateChanged(bool previous, bool current)
    {
        if (current)
        {
            foreach (Upgrade upgrade in linkedUpgradeSO.upgradeList)
            {
                TrainUpgradeHandler.active.currentUpgradeDict[upgrade.trainStat].Add(upgrade);
            }
        }
        else
        {
            foreach (Upgrade upgrade in linkedUpgradeSO.upgradeList)
            {
                TrainUpgradeHandler.active.currentUpgradeDict[upgrade.trainStat].Remove(upgrade);
            }
        }
        SetVisualsState(current);
    }

    private void SetVisualsState(bool enabled)
    {
        foreach (Transform childTransform in GetComponentInChildren<Transform>())
        {
            if (childTransform != transform)
            {
                childTransform.gameObject.SetActive(enabled);
            }
        }
    }
}
