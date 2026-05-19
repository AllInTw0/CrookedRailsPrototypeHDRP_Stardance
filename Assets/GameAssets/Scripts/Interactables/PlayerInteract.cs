using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public static PlayerInteract active;
    //Variables
    [SerializeField] 
    private LayerMask raycastLayer;
    [SerializeField] 
    private LayerMask interactableLayer;
    [SerializeField] 
    private float range;
    [SerializeField] 
    private float raycastRadius;
    
    //Run time
    private Transform currentTarget;
    private Interactable currentInteractable;

    private bool holdingInteractLastFrame;
    private bool finishedInteracting;
    private float timeHoldingInteract;
    void Start()
    {
        active = this;
    }


    void Update()
    {
        if (PlayerHealth.active.isAlive == false)
        {
            if(currentTarget != null)
                StopInteracting();
            
            return;
        }
        
        Raycast();

        //Interact Handling
        if (holdingInteractLastFrame == false && InputManager.interactAction.IsPressed() && currentInteractable != null)
        {
            //Began To Hold Interact
            Debug.Log("Began Interact");
            InteractIcon.active.animator.SetBool("Interacting",true);
            if (currentInteractable.timeToInteract == 0)
            {
                FinishedInteracting();
            }
        }
        
        //For Interactables With Timers
        if (holdingInteractLastFrame && finishedInteracting == false && currentInteractable != null)
        {
            //Is Interacting
            Debug.Log("Is Interacting");
            timeHoldingInteract += Time.deltaTime;
            InteractIcon.active.SetProgress(Mathf.Clamp01(timeHoldingInteract / currentInteractable.timeToInteract));
            
            if (timeHoldingInteract > currentInteractable.timeToInteract)
            {
                //Finished Interacting
                FinishedInteracting();
            }
        }
        
        if (currentInteractable != null)
            holdingInteractLastFrame = InputManager.interactAction.IsPressed();
        else
            holdingInteractLastFrame = false;
        
        if (holdingInteractLastFrame == false)
        {
            finishedInteracting = false;
            timeHoldingInteract = 0;
            InteractIcon.active.SetProgress(0f);
            if(InteractIcon.active.animator.isActiveAndEnabled) InteractIcon.active.animator.SetBool("Interacting",false);
        }
    }

    private void Raycast()
    {
        if (Physics.SphereCast(transform.position,raycastRadius, transform.forward, out RaycastHit hit, range, raycastLayer))
        {
            //( mask & (1 << layer)) != 0 returns true if mask has the layer
            if ((interactableLayer & (1 << hit.collider.gameObject.layer)) != 0)
            {
                if (currentTarget != hit.collider.transform)
                {
                    //hit.collider - collider componenet that was hit
                    //hit.transform - transform of the rigidbody component which way not be the same as the colider's
                    //Different or a new object
                    if(hit.collider.transform.TryGetComponent(out Interactable interactable))
                    {
                        //Debug.Log(hit.transform + " : " + interactable.transform + " : " + hit.collider.transform);

                        currentTarget = hit.collider.transform;
                        currentInteractable = interactable;
                        
                        InteractIcon.active.Enable(currentInteractable);
                    }
                    else
                    {
                        Debug.LogWarning("Interactable doesn't have a script: " + hit.transform);
                        StopInteracting();
                    }
                }
            }
            else
            {
                StopInteracting();
            }
        }
        else
        {
            StopInteracting();
        }
    }

    public void StopInteracting()
    {
        if (currentInteractable == null && currentTarget == null && InteractIcon.active.IsEnabled() == false) return;

        currentTarget = null;
        currentInteractable = null;
        if (InteractIcon.active.animator.isActiveAndEnabled) InteractIcon.active.animator.SetBool("Interacting", false);
        InteractIcon.active.Disable();
        
        holdingInteractLastFrame = false;
    }

    private void FinishedInteracting()
    {
        Debug.Log("Finished Interacting");
        finishedInteracting = true;
        InteractIcon.active.SetProgress(0f);
        InteractIcon.active.animator.SetBool("Interacting",false);
        
        
        bool success = currentInteractable.Interact();
        if(success)
            InteractIcon.active.animator.SetTrigger("Finished");
        else
            InteractIcon.active.animator.SetTrigger("Failed");
    }
    public bool IsLookingAtInteractable()
    {
        return currentInteractable != null;
    }
}
