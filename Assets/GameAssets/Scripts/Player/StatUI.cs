using System;
using UnityEngine;
using TMPro;
public class StatUI : MonoBehaviour
{
    public static StatUI active;
    [SerializeField] 
    private RectTransform parent;
    [SerializeField] 
    private TMP_Text healthText;
    [SerializeField] 
    private TMP_Text staminaText;

    private void Start()
    {
        active = this;
    }

    public void UpdateHealth(float value, float max)
    {
        healthText.text = "Health: " + value + "/" + max;
    }
    public void UpdateStamina(float value, float max)
    {
        staminaText.text = "Stamina: " + value + "/" + max;
    }
    public void Hide()
    {
        parent.gameObject.SetActive(false);
    }
    public void UnHide()
    {
        parent.gameObject.SetActive(true);
    }
}
