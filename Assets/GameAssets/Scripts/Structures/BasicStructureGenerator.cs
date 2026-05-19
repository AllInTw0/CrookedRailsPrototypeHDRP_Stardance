using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicStructureGenerator : StructureGenerator
{
    public List<GenerationEntry> generationEntryList;

    public override IEnumerator Generate(StructureMaster structureMaster)
    {
        foreach (GenerationEntry generationEntry in generationEntryList)
        {
            if (generationEntry.countType == GenerationEntry.CountType.minMaxRandom)
            {
                int count = Random.Range(generationEntry.minMaxCount.x, generationEntry.minMaxCount.y + 1); //+1 because maxExclusive

                for (int i = 0; i < count; i++)
                {
                    //Spawn section
                    GameObject randomPrefab = generationEntry.sectionPrefabList[Random.Range(0, generationEntry.sectionPrefabList.Count)];

                    int sectionCountBefore = structureMaster.structureSectionList.Count;
                    yield return structureMaster.SpawnSection(randomPrefab);
                    if(generationEntry.obligatory && sectionCountBefore == structureMaster.structureSectionList.Count)
                    {
                        structureMaster.RestartGeneration();
                        yield break;
                    }
                }
            }
            else if (generationEntry.countType == GenerationEntry.CountType.fillLenght)
            {
                float length = structureMaster.GetLength(generationEntry.lengthType) + generationEntry.lengthAddition;
                structureMaster.onSectionAdded.AddListener((Section section) =>
                {
                    length -= section.GetLength();
                });

                int safety = 30;
                while (length > 0 && safety > 0)
                {
                    //Spawn section
                    GameObject randomPrefab = generationEntry.sectionPrefabList[Random.Range(0, generationEntry.sectionPrefabList.Count)];

                    int sectionCountBefore = structureMaster.structureSectionList.Count;
                    yield return structureMaster.SpawnSection(randomPrefab);
                    if (generationEntry.obligatory && sectionCountBefore == structureMaster.structureSectionList.Count)
                    {
                        structureMaster.RestartGeneration();
                        yield break;
                    }

                    safety--;
                }
                if (safety <= 0f) Debug.LogWarning("Safety == 0!");
            }
            yield return new WaitForSecondsRealtime(0.4f);
        }
        structureMaster.SpawnEndPrefabs();

        DistanceWaypoint[] distanceWaypoints = transform.GetComponentsInChildren<DistanceWaypoint>();
        foreach (DistanceWaypoint distWaypoint in distanceWaypoints)
        {
            distWaypoint.SpawnFoliage();
        }
    }
}
