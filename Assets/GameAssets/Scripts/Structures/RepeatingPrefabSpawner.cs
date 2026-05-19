using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RepeatingPrefabSpawner : StructureGenerator
{
    //Variables
    [System.Serializable]
    public struct RepeatingPrefabEntry
    {
        public GameObject prefab;
        public float prefabLength;
        public float offset;
        public bool randomlyFlipRotation;
    }
    [SerializeField]
    private List<RepeatingPrefabEntry> repeatingPrefabList;
    [SerializeField]
    private List<RepeatingPrefabEntry> endPrefabList;
    [Header("Length")]
    [SerializeField]
    private LengthType lengthType;
    [SerializeField]
    private float lengthIfNotProvided = 20f;
    [SerializeField]
    private float providedLengthOffset = 10f;
    //RunTime
    private bool spawned = false;

    private void Start()
    {
        if (spawned == false)
            Debug.LogWarning("Spawning without StructureMaster");
        StartCoroutine((IEnumerator)Generate(null));
    }

    public override IEnumerator Generate(StructureMaster structureMaster)
    {
        if (spawned)
            yield break;
        spawned = true;

        //Get Length
        float length = lengthIfNotProvided;
        if (structureMaster != null && lengthType != LengthType.None)
            length = structureMaster.GetLength(lengthType) + providedLengthOffset;

        Vector3 dir = -transform.forward;
        Vector3 pos = transform.position;

        void SpawnPrefab(RepeatingPrefabEntry prefabEntry, float distance, bool rotationReversed = false)
        {
            Transform copy = Instantiate(prefabEntry.prefab,transform).transform;

            copy.transform.position = pos + dir * distance;

            if (rotationReversed)
            {
                copy.transform.position += dir * prefabEntry.offset;
                copy.transform.LookAt(copy.transform.position - dir);
            }
            else
            {
                copy.transform.position -= dir * prefabEntry.offset;
                if(prefabEntry.randomlyFlipRotation && Random.Range(0,2) == 1)
                    copy.transform.LookAt(copy.transform.position - dir);
                else
                    copy.transform.LookAt(copy.transform.position + dir);
            }
        }

        //Start prefab
        if(endPrefabList.Count > 0)
            SpawnPrefab(endPrefabList[Random.Range(0, endPrefabList.Count)], 0f, true);

        //Repeating Prefabs
        float lengthLeft = length;
        while(lengthLeft > 0f)
        {
            RepeatingPrefabEntry chosenPrefab = repeatingPrefabList[Random.Range(0, repeatingPrefabList.Count)];
            SpawnPrefab(chosenPrefab, length - lengthLeft);
            if (lengthLeft - chosenPrefab.prefabLength <= 0f)
                break;
            lengthLeft -= chosenPrefab.prefabLength;
        }

        //End Prefab
        if (endPrefabList.Count > 0)
            SpawnPrefab(endPrefabList[Random.Range(0, endPrefabList.Count)], length - lengthLeft, false);

        yield break;
    }
}
