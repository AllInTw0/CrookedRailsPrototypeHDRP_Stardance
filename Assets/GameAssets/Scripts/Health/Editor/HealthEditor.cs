using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Health), editorForChildClasses: true)]
public class HealthEditor : Editor
{
    private Health health;

    void OnEnable()
    {
        health = (Health)target;
    }
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI();

        if (GUILayout.Button("Take 10% Damage"))
        {
            health.TakeDamage(health.maxHealth * 0.1f);
        }
        if (GUILayout.Button("Take 100% Damage"))
        {
            health.TakeDamage(health.maxHealth);
        }
    }
}
