using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DebugHelper))]
public class DebugHelperEditor : Editor
{
    private DebugHelper debugHelper;

    void OnEnable()
    {
        debugHelper = (DebugHelper)target;
    }
    
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI();

        if (GUILayout.Button("Add Railcar"))
        {
            RailCar railCar = Train.playerTrain.AddRailCar(debugHelper.railCar, 2); // 2 because 0-locomotive, 1-tender
            railCar.SetCargo(debugHelper.cargo, 1, 1);
        }
        if (GUILayout.Button("Spawn Enemy Near Player"))
        {
            EnemyManager.active.SpawnEnemy(debugHelper.enemy.prefab, Player.localPlayer.transform.position + new Vector3(3f,1f,0), Quaternion.identity);
        }

        if (GUILayout.Button("Spawn Item"))
        {
            Item.SpawnItem(debugHelper.item,Player.localPlayer.transform.position + Player.localPlayer.transform.forward + Vector3.up,Quaternion.identity);
        }
        if (GUILayout.Button("Shake Camera"))
        {
            PlayerCamera.active.Shake(debugHelper.shakeStrength, debugHelper.shakeSpring, debugHelper.shakeDamp);
        }
    }
}
