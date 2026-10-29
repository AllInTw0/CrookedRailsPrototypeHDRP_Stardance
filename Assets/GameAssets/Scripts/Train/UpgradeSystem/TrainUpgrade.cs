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
    
    public virtual void Start()
    {
        unlocked.OnValueChanged += UnlockedStateChanged;
        
        TrainUpgradeHandler.onUpgradeAdded.AddListener((UpgradeSO upgrade) =>
        {
            if(upgrade == linkedUpgradeSO)
                SetUnlocked(true);
        });
        TrainUpgradeHandler.onUpgradeRemoved.AddListener((UpgradeSO upgrade) =>
        {
            if(upgrade == linkedUpgradeSO)
                SetUnlocked(false);
        });
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if(unlockedAtStart)
                TrainUpgradeHandler.active.AddUpgrade(linkedUpgradeSO);
            else if (TrainUpgradeHandler.IsUpgradeUnlocked(linkedUpgradeSO))
                TrainUpgradeHandler.active.AddUpgrade(linkedUpgradeSO);
            else
                UnlockedStateChanged(IsUnlocked(), IsUnlocked());
        }
        else
        {
            UnlockedStateChanged(IsUnlocked(), IsUnlocked());
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
            if (childTransform.parent == transform)
            {
                childTransform.gameObject.SetActive(enabled);
            }
        }
    }
}
