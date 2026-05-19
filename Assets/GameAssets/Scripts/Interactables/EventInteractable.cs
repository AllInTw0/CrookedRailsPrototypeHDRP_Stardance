using UnityEngine;
using UnityEngine.Events;
public class EventInteractable : Interactable
{
    [Header("Event")]
    [SerializeField]
    public UnityEvent interactEvent;

    private bool interactionFailed;
    public override bool Interact()
    {
        interactEvent.Invoke();
        base.Interact();

        if (interactionFailed) 
        {
            interactionFailed = false;
            return false;
        }
        return true;
    }

    public void InteractionFailed()
    {
        interactionFailed = true;
    }
}
