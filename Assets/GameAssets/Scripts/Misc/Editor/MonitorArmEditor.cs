using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MonitorArm))]
public class MonitorArmEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI();

        MonitorArm monitorArm = (MonitorArm)target;

        if (GUILayout.Button("Play Animation"))
        {
            monitorArm.PlayAniamtion(monitorArm.animName, monitorArm.speed);
        }
        if (GUILayout.Button("Turn On light"))
        {
            monitorArm.TurnOnLight();
        }
        if (GUILayout.Button("Turn Off light"))
        {
            monitorArm.TurnOffLight();
        }
    }
}
