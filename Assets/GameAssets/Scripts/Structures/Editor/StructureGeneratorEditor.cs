using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StructureGenerator), editorForChildClasses: true)]
public class StructureGeneratorEditor : Editor
{
    private StructureGenerator structureGenerator;

    void OnEnable()
    {
        structureGenerator = (StructureGenerator)target;
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Generate"))
        {
            structureGenerator.GenerateTest();
        }
    }
}
