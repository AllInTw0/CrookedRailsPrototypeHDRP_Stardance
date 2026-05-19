using System.Collections.Generic;
using UnityEngine;

public class RailCarHealth : Health
{
    [Header("Railcar Health")]
    [SerializeField]
    private RailCar linkedRailCar;
    [SerializeField]
    private List<EventInteractable> rerailInteractableList;
    [SerializeField]
    private List<GameObject> brokenDownParticles;

    private bool brokenDown;
    private void Start()
    {
        AddHealthToGlobalList();

        foreach (EventInteractable interactable in rerailInteractableList)
        {
            interactable.interactEvent.AddListener(() =>
            {
                Rerail();
            });

            interactable.gameObject.SetActive(false);

            interactable.objectName = linkedRailCar.railCarSO.GetName();
            interactable.actionName = "Rerail";

            interactable.timeToInteract = 4.5f;
        }
        foreach (GameObject obj in brokenDownParticles)
        {
            obj.SetActive(false);
        }
    }
    private void Update()
    {
        UpdateCall();
    }
    public override void HealthReachedZero(Vector3 force = default)
    {
        if (brokenDown) return;
        
        brokenDown = true;
        Derail();

        base.HealthReachedZero();
    }

    public void Rerail()
    {
        foreach (EventInteractable interactable in rerailInteractableList)
        {
            interactable.gameObject.SetActive(false);
        }
        linkedRailCar.derailed = false;
    }
    public void Derail()
    {
        foreach (EventInteractable interactable in rerailInteractableList)
        {
            interactable.gameObject.SetActive(true);
        }

        linkedRailCar.derailed = true;

        if (brokenDown)
        {
            foreach (GameObject obj in brokenDownParticles)
            {
                obj.SetActive(true);
            }
        }
    }

    public void ResetHealth()
    {
        Rerail();
        brokenDown = false;
        health = maxHealth;
        foreach (GameObject obj in brokenDownParticles)
        {
            obj.SetActive(false);
        }
    }
    public bool IsBroken()
    {
        return brokenDown;
    }
}
