using UnityEngine;

public class EnableOnStart : MonoBehaviour
{
    public GameObject targetObject;

    private void Awake()
    {
        if(this.enabled)
            targetObject.SetActive(true);
    }
}
