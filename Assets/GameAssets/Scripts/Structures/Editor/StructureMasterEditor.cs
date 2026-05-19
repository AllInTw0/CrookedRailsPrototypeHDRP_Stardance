using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StructureMaster))]
public class StructureMasterEditor : Editor
{
    private StructureMaster structureMaster;

    void OnEnable()
    {
        structureMaster = (StructureMaster)target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Destroy"))
        {
            structureMaster.DestroyStructure();
        }
        if (GUILayout.Button("Generate"))
        {
            structureMaster.Generate();
        }

    }
}
