using UnityEngine;

public class FollowObject : MonoBehaviour
{
    [SerializeField] 
    public Transform target;
    [SerializeField] 
    private Vector3 affectedAxis = Vector3.one;
    void LateUpdate()
    {
        if(target != null)
            transform.position = new Vector3(target.position.x * affectedAxis.x, target.position.y * affectedAxis.y, target.position.z * affectedAxis.z);
    }
}
