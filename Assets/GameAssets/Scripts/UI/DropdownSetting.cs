using UnityEngine;
using TMPro;
using System.Collections.Generic;
public class DropdownSetting : Setting
{
    [SerializeField]
    private TMP_Dropdown dropdown;

    private void Start()
    {
        dropdown.onValueChanged.AddListener((int index) =>
        {
            Settings.PlayClick();
            onValueChanged.Invoke(index);
        });
    }

    public void SetValues(List<string> valueList, int index = 0)
    {
        List<TMP_Dropdown.OptionData> optionDataList = new List<TMP_Dropdown.OptionData>();
        foreach (string value in valueList)
        {
            optionDataList.Add(new TMP_Dropdown.OptionData(value));
        }
        dropdown.options = optionDataList;
        dropdown.value = index;
    }

    public override float GetValue()
    {
        return dropdown.value;
    }
}
