using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGeneration))]
public class TerrainGenerationEditor : Editor
{
    private TerrainGeneration terrainGeneration;

    void OnEnable()
    {
        terrainGeneration = (TerrainGeneration)target;
    }
    public override void OnInspectorGUI()
    {
        if (DrawDefaultInspector() && terrainGeneration.autoUpdate)
        {
            terrainGeneration.GeneratePreviewEditor();
        }

        if (GUILayout.Button("Generate Preview"))
        {
            terrainGeneration.GeneratePreviewEditor();
        }
    }
}
