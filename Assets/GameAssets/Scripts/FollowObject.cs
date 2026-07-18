using UnityEngine;

public class FollowObject : MonoBehaviour
{
    [SerializeField] 
    public Transform target;
    [SerializeField] 
    private Vector3 affectedAxis = Vector3.one;
    [Header("Overrides")]
    [SerializeField]
    private bool targetIsPlayer;
    void LateUpdate()
    {
        if (targetIsPlayer && Player.localPlayer)
            target = Player.localPlayer.transform;
        if(target != null)
            transform.position = new Vector3(target.position.x * affectedAxis.x, target.position.y * affectedAxis.y, target.position.z * affectedAxis.z);
    }
}
