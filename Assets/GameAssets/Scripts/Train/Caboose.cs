using System;
using System.Collections.Generic;
using UnityEngine;

public class Caboose : MonoBehaviour
{
    private RailCar railCar;

    [SerializeField] 
    private List<Interactable> doorInteractableList;
    private void Start()
    {
        railCar = GetComponent<RailCar>();
        foreach (Interactable door in doorInteractableList)
        {
            door.enabled = false;
        }
    }

    private void Update()
    {
        if (doorInteractableList[0].enabled == false && railCar.GetLinkedTrain().IsInitialized() && railCar.GetLinkedTrain().speed == 0)
        {
            foreach (Interactable door in doorInteractableList)
            {
                door.enabled = true;
            }
        }
    }
    
    
}
