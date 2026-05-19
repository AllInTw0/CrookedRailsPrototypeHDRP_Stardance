using System;
using UnityEngine;

public class SpectateUI : MonoBehaviour
{
    public static SpectateUI active;
    [SerializeField] 
    private RectTransform spectateUIParent;

    private void Start()
    {
        active = this;
    }

    public void Enable()
    {
        spectateUIParent.gameObject.SetActive(true);
    }
    public void Disable()
    {
        spectateUIParent.gameObject.SetActive(false);
    }
}
