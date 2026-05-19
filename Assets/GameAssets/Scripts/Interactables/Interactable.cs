using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string objectName;
    public string actionName;
    public float timeToInteract;

    public Transform iconPosition;

    [SerializeField] 
    public string interactSound = "Click";

    [HideInInspector]
    public string objectNameOverride;
    [HideInInspector]
    public string actionNameOverride;

    private float objectNameOverrideTime;
    private float actionNameOverrideTime;
    private void Update()
    {
        UpdateInetractable();
    }
    public void UpdateInetractable()
    {
        if (objectNameOverride != "")
        {
            objectNameOverrideTime -= Time.deltaTime;
            if (objectNameOverrideTime <= 0f) SetObjectNameOverride();
        }
        if (actionNameOverride != "")
        {
            actionNameOverrideTime -= Time.deltaTime;
            if (actionNameOverrideTime <= 0f) SetActionNameOverride();
        }
    }
    public virtual bool Interact()
    {
        SoundManager.active.PlayAtPos(iconPosition != null ? iconPosition.position : transform.position,interactSound);
        return true;
    }
    public virtual string GetName()
    {
        if (objectNameOverride != "")
            return objectNameOverride;
        else
            return objectName;
    }
    public virtual string GetAction()
    {
        if (actionNameOverride != "")
            return actionNameOverride;
        else
            return actionName;
    }

    public void SetObjectNameOverride(string name = "", float time = float.MaxValue)
    {
        objectNameOverride = name;
        InteractIcon.active.Refresh();
        objectNameOverrideTime = time;
    }
    public void SetActionNameOverride(string name = "", float time = float.MaxValue)
    {
        actionNameOverride = name;
        InteractIcon.active.Refresh();
        actionNameOverrideTime = time;
    }

    public void ClearOverrides()
    {
        objectNameOverride = "";
        actionNameOverride = "";
        InteractIcon.active.Refresh();
    }
}
