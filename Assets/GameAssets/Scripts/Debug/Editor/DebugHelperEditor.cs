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
            railCar.SetCargo(debugHelper.cargo, 1f, 1f);
        }
        if (GUILayout.Button("Spawn Enemy Near Player"))
        {
            EnemyManager.active.SpawnEnemy(debugHelper.enemy.prefab, PlayerMovement.active.transform.position + new Vector3(3f,1f,0), Quaternion.identity);
        }
    }
}
