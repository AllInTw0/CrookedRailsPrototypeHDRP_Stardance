using System;
using UnityEngine;

public class LeverInteractable : Interactable
{
    enum LeverType
    {
        Rotation,
        Position
    }
    [Header("Lever")]
    [SerializeField]
    private LeverType leverType;
    [SerializeField] 
    private Vector3 startAxis;
    [SerializeField]
    private Vector3 endAxis;
    [SerializeField] 
    public int notches;
    [SerializeField] 
    private float speed;
    [SerializeField] 
    private bool displayNotches = true;
    //RunTime
    [Header("Start Notch")]
    public int currentNotch;
    private bool locked = false;
    private void Update()
    {
        Vector3 targetVector = Vector3.Lerp(startAxis,endAxis, currentNotch / (float)notches);
        if (leverType == LeverType.Rotation)
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(targetVector), speed * Time.deltaTime);
        else
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetVector, speed * Time.deltaTime);
    }

    public override bool Interact()
    {
        if (locked)
        {
            base.Interact();//Play sound
            return false;
        }

        currentNotch++;
        if (currentNotch > notches)
            currentNotch = 0;
        
        InteractIcon.active.Refresh();
        return base.Interact();
    }
    
    public override string GetName()
    {
        if (objectNameOverride != "")
            return objectNameOverride;
        else if (locked)
            return objectName;
        else
        {
            if (displayNotches)
                return objectName + " [" + currentNotch + "/" + notches + "]";

            //else
            return base.GetName();
        }
    }
    public override string GetAction()
    {
        if (actionNameOverride != "")
            return actionNameOverride;
        else if (locked)
            return "Locked";
        else
            return base.GetAction();
    }
    public void SetLocked(bool locked)
    {
        this.locked = locked;
        InteractIcon.active.Refresh();
    }
}
