using UnityEngine;

public class DisableByKeyPress : MonoBehaviour
{

    [SerializeField]
    private UnityEngine.InputSystem.Key key;
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current[key].wasPressedThisFrame)
        {
            foreach (Transform child in GetComponentInChildren<Transform>())
            {
                if(child != transform)
                    child.gameObject.SetActive(!child.gameObject.activeSelf);
            }
        }
    }
}
