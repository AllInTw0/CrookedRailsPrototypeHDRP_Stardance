using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnimationPlayer))]
public class AnimationPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI();

        AnimationPlayer animPlayer = (AnimationPlayer)target;

        if (GUILayout.Button("Play Animation"))
        {
            animPlayer.PlayAniamtion(animPlayer.animName, animPlayer.speed);
        }
    }
}
