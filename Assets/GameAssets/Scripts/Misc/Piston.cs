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
        if (!targetTransform)
            return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (pistonStartModel) pistonStartModel.LookAt(targetTransform);
        if (pistonStretchModel) { pistonStretchModel.LookAt(targetTransform); pistonStretchModel.localScale = new Vector3(pistonStretchModel.localScale.x, pistonStretchModel.localScale.y, distance); }
        if (pistonEndModel) pistonEndModel.LookAt(transform.position);

    }
}
