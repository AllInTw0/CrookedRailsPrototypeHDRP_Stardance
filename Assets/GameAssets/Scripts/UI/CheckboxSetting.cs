using UnityEngine;
using UnityEngine.UI;

public class CheckboxSetting : Setting
{
    [SerializeField]
    private Graphic checkMarkGraphic;
    [SerializeField]
    private Button button;

    private bool ticked;
    private void Start()
    {
        button.onClick.AddListener(() =>
        {
            SetTicked(!ticked);
        });
    }

    public void SetTicked(bool boolValue)
    {
        ticked = boolValue;

        checkMarkGraphic.color = new Color(1f, 1f, 1f, GetValue());

        Settings.PlayClick();
        onValueChanged.Invoke(GetValue());
    }
    private void LateUpdate()
    {
        //Override fade script
        if(ticked == false)
            checkMarkGraphic.color = new Color(1f, 1f, 1f, 0f);
    }
    public override float GetValue()
    {
        return ticked ? 1 : 0;
    }
}
