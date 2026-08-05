using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
public class EventInteractable : Interactable
{
    [Header("Event")]
    [SerializeField]
    public UnityEvent interactEventLocal;
    [SerializeField]
    public UnityEvent interactEventSynced;
    
    private bool interactionFailed;
    public override bool Interact()
    {
        interactEventLocal.Invoke();
        base.Interact();

        if (interactionFailed)
        {
            interactionFailed = false;
            return false;
        }

        if (interactEventSynced.GetPersistentEventCount() > 0)
        {
            InteractRpc();
        }
        
        return true;
    }

    [Rpc(SendTo.Everyone)]
    public void InteractRpc()
    {
        interactEventSynced.Invoke();
    }

    public void InteractionFailed()
    {
        interactionFailed = true;
    }
}
