using UnityEngine;

public class Coupler : MonoBehaviour
{

    [Header("Refrences")]
    [SerializeField]
    private Transform couplerTransform;
    [SerializeField]
    private MeshFilter couplerMeshFilter;
    [Header("Meshes")]
    [SerializeField]
    private Mesh couplerOpenMesh;
    [SerializeField]
    private Mesh couplerClosedMesh;

    private Coupler connectedCoupler;
    private Vector3 defaultLocalPos;

    public void ConnectCoupler(Coupler coupler)
    {
        if (couplerTransform == null) return;

        if (coupler != null)
        {
            connectedCoupler = coupler;
            if (couplerMeshFilter != null)
                couplerMeshFilter.sharedMesh = couplerClosedMesh;
        }
        else
        {
            connectedCoupler = null;
            if (couplerMeshFilter != null)
            {
                couplerMeshFilter.sharedMesh = couplerClosedMesh;
            }
            couplerTransform.localPosition = defaultLocalPos;
            couplerTransform.localRotation = Quaternion.identity;
        }
    }

    private void LateUpdate()
    {
        if (connectedCoupler == null) return;

        Vector3 targetPosLocal = transform.InverseTransformPoint(connectedCoupler.GetWorldPos());
        targetPosLocal = new Vector3(targetPosLocal.x, 0, targetPosLocal.z);
        //float distance = Mathf.Sqrt(targetPosLocal.x * targetPosLocal.x + targetPosLocal.z + targetPosLocal.z);

        couplerTransform.localPosition = targetPosLocal * 0.5f;
        couplerTransform.LookAt(transform.TransformPoint(targetPosLocal));
    }

    public Vector3 GetWorldPos()
    {
        return transform.position;
    }
}
