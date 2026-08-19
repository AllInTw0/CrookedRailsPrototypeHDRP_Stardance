using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
public class EventInteractable : Interactable
{
    [Header("Event")]
    [SerializeField]
    public UnityEvent interactEventLocal;
    [SerializeField]
    public UnityEvent interactEventEveryone;
    [SerializeField]
    public UnityEvent<ulong> interactEventServer;
    
    private bool interactionFailed;
    public override bool Interact()
    {
        interactEventLocal.Invoke();
        
        if(IsSpawned)
            InteractEveryoneRpc();

        if(IsServer)
            interactEventServer.Invoke(NetworkManager.LocalClientId);
        else if(IsSpawned)
            InteractServerRpc();

        base.Interact();

        if (interactionFailed)
        {
            interactionFailed = false;
            return false;
        }
        
        return true;
    }

    [Rpc(SendTo.Everyone)]
    public void InteractEveryoneRpc()
    {
        interactEventEveryone.Invoke();
    }
    [Rpc(SendTo.Server)]
    public void InteractServerRpc(RpcParams receiveParams = default)
    {
        interactEventServer.Invoke(receiveParams.Receive.SenderClientId);
    }

    public void InteractionFailed()
    {
        interactionFailed = true;
    }
}
