using UnityEngine;

public class UnparentOnStart : MonoBehaviour
{
    public float delay = 0.25f;
    void Start()
    {
        Invoke(nameof(Unparent),delay);
    }
    
    void Unparent()
    {
        transform.SetParent(null);
    }
    
}
