using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Item), editorForChildClasses:true)]
public class ItemEditor : Editor
{
    private Item item;

    void OnEnable()
    {
        item = (Item)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Test Physics"))
        {
            item.EnablePhysics(new Vector3(Random.Range(-3f, 3f), Random.Range(8f, 10f), Random.Range(-3f, 3f)));
        }

    }
}
