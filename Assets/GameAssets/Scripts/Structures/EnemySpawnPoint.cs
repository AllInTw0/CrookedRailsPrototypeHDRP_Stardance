using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : StructureGenerator
{
    public static Dictionary<StructureMaster, List<EnemySpawnPoint>> activeSpawnPointDict = new Dictionary<StructureMaster, List<EnemySpawnPoint>>();

    [Header("Spawn Point")] 
    public float radius;

    private StructureMaster linkedStructure;
    public override IEnumerator Generate(StructureMaster structureMaster)
    {
        linkedStructure = structureMaster;
        if(activeSpawnPointDict.TryGetValue(structureMaster, out List<EnemySpawnPoint> list))
            list.Add(this);
        else
            activeSpawnPointDict.Add(structureMaster, new List<EnemySpawnPoint>(){this});
        yield break;
    }

    public void SpawnDanger(float danger)
    {
        EnemySpawner.active.SpawnDanger(danger, Vector2.zero, Vector2.zero, this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    public override void OnDestroy()
    {
        if(activeSpawnPointDict.TryGetValue(linkedStructure, out List<EnemySpawnPoint> list))
            list.Remove(this);
        base.OnDestroy();
    }
}
