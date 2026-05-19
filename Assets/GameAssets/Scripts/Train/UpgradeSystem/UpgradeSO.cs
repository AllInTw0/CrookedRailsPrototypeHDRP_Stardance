using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeSO", menuName = "ScriptableObjects/UpgradeSO", order = 1)]
public class UpgradeSO : ShopItemSO
{
    [Header("UpgradeSO")]
    public List<Upgrade> upgradeList = new List<Upgrade>();
}
