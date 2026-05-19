using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Section))]
public class SectionEditor : Editor
{
    private Section section;

    void OnEnable()
    {
        section = (Section)target;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Auto Add Connections"))
        {
            foreach (Transform childTransform in section.transform.Find("Connections").GetComponentsInChildren<Transform>())
            {
                if (childTransform == section.transform.Find("Connections")) continue;

                if(childTransform.childCount == 0)
                {
                    section.AddTranfromToConnections(childTransform);
                }
            }
        }

    }
}
