using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class Trigger : MonoBehaviour
{
    public UnityEvent onEnter;
    public UnityEvent onEnterAll;
    public UnityEvent onEnterOnce;
    public UnityEvent onExit;
    public UnityEvent onExitAll;
    
    [SerializeField]
    private LayerMask layerFiler;

    private bool triggered = false;
    private List<Collider> colliderList = new List<Collider>();
    
    private void OnTriggerEnter(Collider other)
    {
        //( mask & (1 << layer)) != 0 returns true if mask has the layer
        if ((layerFiler & (1 << other.gameObject.layer)) != 0)
        {
            if (triggered == false) onEnterOnce.Invoke();
            onEnter.Invoke();
            triggered = true;
        }

        if (IsInTrigger(other) == false)
        {
            if (colliderList.Count == 0)
            {
                onEnterAll.Invoke();
            }
            colliderList.Add(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        //( mask & (1 << layer)) != 0 returns true if mask has the layer
        if ((layerFiler & (1 << other.gameObject.layer)) != 0)
        {
            onExit.Invoke();
        }
        
        if (IsInTrigger(other))
        {
            colliderList.Remove(other);
            if (colliderList.Count == 0)
            {
                onExitAll.Invoke();
            }
            
        }
    }

    private bool IsInTrigger(Collider collider)
    {
        for (int i = 0; i < colliderList.Count; i++)
        {
            if (colliderList[i] == collider)
                return true;
        }

        return false;
    }
}
