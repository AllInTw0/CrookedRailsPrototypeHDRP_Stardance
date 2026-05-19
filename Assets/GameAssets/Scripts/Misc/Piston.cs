using UnityEngine;

[ExecuteInEditMode]
public class Piston : MonoBehaviour
{
    public Transform targetTransform;
    [Header("Model")]
    public Transform pistonStartModel;
    public Transform pistonStretchModel;
    public Transform pistonEndModel;

    void Start()
    {
        
    }

    void Update()
    {
        if (targetTransform == null)
            return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (pistonStartModel != null) pistonStartModel.LookAt(targetTransform);
        if (pistonStretchModel != null) { pistonStretchModel.LookAt(targetTransform); pistonStretchModel.localScale = new Vector3(pistonStretchModel.localScale.x, pistonStretchModel.localScale.y, distance); }
        if (pistonEndModel != null) pistonEndModel.LookAt(transform.position);

    }
}
