using UnityEngine;

public class SpecialJobSO : ScriptableObject
{
    public string title;
    public string description;
    public string underText;
    public virtual string GetDescription()
    {
        return description;
    }
    public virtual string GetUnderText()
    {
        return underText;
    }
    public virtual Texture2D GetIcon()
    {
        return null;
    }

    public virtual bool OrderJob()
    {
        return true;
    }
}
