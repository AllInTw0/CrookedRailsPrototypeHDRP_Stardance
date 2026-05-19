using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class SliderSetting : Setting
{
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private TMP_InputField inputField;

    private void Start()
    {
        slider.onValueChanged.AddListener((float value) =>
        {
            UpdateInputField();
            Settings.PlayClick();
            onValueChanged.Invoke(value);
        });

        inputField.onValueChanged.AddListener((string str) =>
        {
            if (str == "0" || str == "0," || str == "0.") return;

            if(float.TryParse(str,out float value))
            {
                slider.value = value;
            }

            UpdateInputField();
        });
    }
    public void SetValues(float minValue, float maxValue, float defaultValue)
    {
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = defaultValue;
        UpdateInputField();
    }
    private void UpdateInputField()
    {
        inputField.text = (Mathf.Round(slider.value * 10f) / 10f).ToString();
    }
    public override float GetValue()
    {
        return slider.value;
    }
}
