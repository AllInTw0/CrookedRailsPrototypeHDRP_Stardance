using Codice.CM.Common;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PaperRenderer))]
public class PaperRendererEditor : Editor
{
    private PaperRenderer paperRenderer;

    void OnEnable()
    {
        paperRenderer = (PaperRenderer)target;
    }
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI();

        if (GUILayout.Button("Render Active"))
        {
            foreach (var item in paperRenderer.paperList)
            {
                if(item.paperObject.activeSelf)
                {
                    Texture2D texture = paperRenderer.RenderPaper(item.name, new List<Override>());
                    AssetDatabase.CreateAsset(texture, "Assets/GameAssets/PaperTest.asset");
                    return;
                }    
            }
            Debug.LogWarning("No Active Paper");
        }

    }
}
