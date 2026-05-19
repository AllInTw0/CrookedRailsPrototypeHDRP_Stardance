using UnityEngine;

public class Ladder : MonoBehaviour
{
    [HideInInspector]
    public CapsuleCollider ladderCollider;
    private void Start()
    {
        ladderCollider = transform.GetComponent<CapsuleCollider>();
        ladderCollider.isTrigger = true;
    }
    public Vector3 GetLadderDir()
    {
        return transform.forward;
    }
}
