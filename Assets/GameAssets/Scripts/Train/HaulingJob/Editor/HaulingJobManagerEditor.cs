using Codice.CM.Common;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[CustomEditor(typeof(HaulingJobManager))]
public class HaulingJobManagerEditor : Editor
{
    private HaulingJobManager haulingJobManager;
    void OnEnable()
    {
        haulingJobManager = (HaulingJobManager)target;
    }
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate Icons"))
        {
            haulingJobManager.GenerateIcons();
        }

    }
}
