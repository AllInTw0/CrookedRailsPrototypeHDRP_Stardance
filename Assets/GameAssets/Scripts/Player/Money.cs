using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Money : NetworkBehaviour
{
    private const int startingMoney = 600;
    
    public static UnityEvent onMoneyChanged = new UnityEvent();
    private static NetworkVariable<int> money = new (writePerm: NetworkVariableWritePermission.Server);
    
    public static void SetStartingMoney()
    {
        Debug.Log("Is Server: " +  NetworkManager.Singleton.IsServer);
        if(NetworkManager.Singleton.IsServer)
            money.Value = startingMoney;
        
        money.OnValueChanged -= MoneyChanged;
        money.OnValueChanged += MoneyChanged;
    }
    
    public static bool CanAfford(int amount)
    {
        return money.Value >= amount;
    }
    public static void AddMoney(int amount)
    {
        if (NetworkManager.Singleton.IsServer == false)
        {
            Debug.LogWarning("Client called add money");
            return;
        }
        money.Value += amount;
    }

    private static void MoneyChanged(int previous, int current)
    {
        int change = current - previous;
        
        string str = change < 0 ? "<color=red>" + change + "$</color>" : change + "$";

        Override newOverride = new Override("Sum", OverrideType.Text);
        newOverride.stringOverride = str;
        MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Receipt", new List<Override>() { newOverride }));
        onMoneyChanged.Invoke();
    }
    public static int GetAmount()
    {
        return money.Value;
    }
}
