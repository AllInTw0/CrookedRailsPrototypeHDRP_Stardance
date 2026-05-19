using UnityEngine;
using UnityEngine.Events;

public class Setting : MonoBehaviour
{
    public UnityEvent<float> onValueChanged;

    public virtual float GetValue()
    {
        return 0f;
    }
}
