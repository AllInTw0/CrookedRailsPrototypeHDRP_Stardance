using UnityEngine;
using UnityEngine.Events;
public class Trigger : MonoBehaviour
{
    public UnityEvent onEnter;
    public UnityEvent onEnterOnce;
    public UnityEvent onExit;

    [SerializeField]
    private LayerMask layerFiler;

    private bool triggered = false;
    
    private void OnTriggerEnter(Collider other)
    {
        //( mask & (1 << layer)) != 0 returns true if mask has the layer
        if ((layerFiler & (1 << other.gameObject.layer)) != 0)
        {
            if (triggered == false) onEnterOnce.Invoke();
            onEnter.Invoke();
            triggered = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        //( mask & (1 << layer)) != 0 returns true if mask has the layer
        if ((layerFiler & (1 << other.gameObject.layer)) != 0)
        {
            onExit.Invoke();
        }
    }
}
