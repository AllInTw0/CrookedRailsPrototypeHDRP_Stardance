using System.Collections.Generic;
using UnityEngine;

public class SpecialJobSO : ScriptableObject
{
    public string title;
    public string description;
    public string underText;

    public List<StructureSO> jobStructureList = new List<StructureSO>();
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
        foreach (StructureSO structureSO in jobStructureList)
        {
            GenerationManager.active.AddOneTimeStructure(structureSO);
        }
        return true;
    }
}
