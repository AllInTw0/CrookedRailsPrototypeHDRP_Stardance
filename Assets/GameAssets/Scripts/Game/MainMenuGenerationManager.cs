using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Util;

public class MainMenuGenerationManager : MonoBehaviour
{
    public Train train;
    [SerializeField]
    private int generatedSectionsAheadOfTrain = 2;
    [SerializeField]
    private int generatedSectionsBehindTrain = 2;
    [Header("Foliage")]
    [SerializeField]
    private List<ProbabilityListElement<ObjectPool>> foliageProbabilityElementList = new List<ProbabilityListElement<ObjectPool>>();
    [SerializeField]
    private int chunkSize;
    [SerializeField]
    private int foliageDensity;
    [SerializeField]
    private AnimationCurve foliageSpawnProbability_DistanceFromTrack;
    [SerializeField]
    private LayerMask foliageRaycastLayerMask;
    [SerializeField]
    private LayerMask foliageSpawnLayerMask;
    private List<Vector2> foliageFilledChunkList = new List<Vector2>();

    private ProbabilityList<ObjectPool> foliageProbabilityList;

    private Point lastPoint;
    private TrackSection lastSection;

    private void Start()
    {
        Point pointA = Spline.CreatePoint(Vector3.zero, Vector3.forward);
        Point pointB = Spline.CreatePoint(Vector3.forward * (train.GetConsistLenght() + 5f), Vector3.forward * ((train.GetConsistLenght() + 5f) * 1.5f));

        TrackSection section = TrackManager.active.CreateTrackSection(pointA, pointB);

        train.Initialize(train.GetConsistLenght() + 2.5f, section);

        lastSection = section;
        lastPoint = pointB;


        foreach (ProbabilityListElement<ObjectPool> entry in foliageProbabilityElementList)
        {
            entry.element.Init(entry.element.maxCapacity, entry.element.defaultCapacity);
        }

        foliageProbabilityList = new ProbabilityList<ObjectPool>(foliageProbabilityElementList);
    }
    private void Update()
    {
        int sectionsAhead = 0;
        TrackSection trackSection = train.GetFrontTrackSection();
        while (trackSection.nextSection != null)
        {
            sectionsAhead++;
            trackSection = trackSection.nextSection;
        }

        int sectionsBehind = 0;
        trackSection = train.GetBackTrackSection();
        while (trackSection.previousSection != null)
        {
            sectionsBehind++;
            trackSection = trackSection.previousSection;
        }

        if (sectionsAhead < generatedSectionsAheadOfTrain)
        {
            GenerateNextSection();
        }

        if (sectionsBehind > generatedSectionsBehindTrain)
        {
            Debug.Log("Destroy Section");
            TrackManager.active.DestroyTrackSection(trackSection);
        }
    }

    private void GenerateNextSection()
    {
        //Pick random dir
        Point pointB = null;
        switch (Random.Range(0,2+1))
        {
            case 0:
                pointB = Spline.CreatePoint(lastPoint.position + new Vector3(0f, 0f, 40f), lastPoint.position + new Vector3(0f, 0f, 50f));
                break;
            case 1:
                pointB = Spline.CreatePoint(lastPoint.position + new Vector3(-10f, 0f, 40f), lastPoint.position + new Vector3(-10f, 0f, 50f));
                break;
            case 2:
                pointB = Spline.CreatePoint(lastPoint.position + new Vector3(10f, 0f, 40f), lastPoint.position + new Vector3(10f, 0f, 50f));
                break;
            default:
                break;
        }

        TrackSection section = TrackManager.active.CreateTrackSection(lastPoint, pointB);

        lastSection.SetNextSection(section);
        lastSection = section;
        lastPoint = pointB;

        StartCoroutine(GenerateFoliageAroundPoint(section.pointA.position));

    }

    public IEnumerator GenerateFoliageAroundPoint(Vector3 position, int radius = 3)
    {
        if(TrackManager.active.trackSectionList.Count > 2)
            yield return new WaitForSeconds(0.1f);

        Vector2 pos = new Vector2(position.x, position.z);

        Vector2Int chunkPos = new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize), Mathf.FloorToInt(pos.y / chunkSize));
        List<Vector2Int> chunkList = new List<Vector2Int>();

        void AddChunk(Vector2Int chunk)
        {
            if (foliageFilledChunkList.Contains(chunk) == false)
            {
                chunkList.Add(chunk);
                foliageFilledChunkList.Add(chunk);
            }
        }
        for (int x = chunkPos.x - radius; x < chunkPos.x + radius; x++)
        {
            for (int y = chunkPos.y - radius; y < chunkPos.y + radius; y++)
            {
                AddChunk(new Vector2Int(x, y));
            }
        }


        foreach (Vector2Int chunk in chunkList)
        {
            Vector3 cornerWorldPos = new Vector3(chunk.x * chunkSize, 0, chunk.y * chunkSize);
            float increment = chunkSize / foliageDensity;
            for (int x = 0; x < foliageDensity; x++)
            {
                for (int y = 0; y < foliageDensity; y++)
                {
                    if (TrackManager.active.trackSectionList.Count > 2)
                        yield return new WaitForSeconds(0.015f);
                    SpawnFoliage(cornerWorldPos + new Vector3(x * increment, 0, y * increment));
                }
            }
        }
    }
    private void SpawnFoliage(Vector3 pos)
    {
        ObjectPool prefabPool = foliageProbabilityList.PickNext();
        if (prefabPool == null) return;

        TrackManager.active.GetClosestTrackSection(pos, out TrackSection trackSection, out float distance);
        float dist = TrackManager.active.GetDistanceFromPath(trackSection.path, pos);
        if (Random.Range(0f, 1f) > foliageSpawnProbability_DistanceFromTrack.Evaluate(TrackManager.active.IsLeftOfPath(trackSection.path, pos) ? -dist : dist))
            return;

        
        Debug.DrawLine(pos, pos + Vector3.up * 20f, Color.coral, 60f);
        Vector2 randomDir = Random.insideUnitCircle;
        pos += new Vector3(randomDir.x * foliageDensity, 0, randomDir.y * foliageDensity);

        if (Physics.SphereCast(new Vector3(pos.x, 50f, pos.z), 2.5f, Vector3.down, out RaycastHit hit, 75f, foliageRaycastLayerMask))
        {
            if ((foliageSpawnLayerMask & (1 << hit.transform.gameObject.layer)) != 0)
            {
                Transform copy = prefabPool.Get().transform;
                copy.position = hit.point - new Vector3(0f, 0.1f, 0f);
                copy.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));

                trackSection.AddObject(prefabPool, copy.gameObject);
            }
            else
            {
                Debug.DrawLine(pos, pos + Vector3.up * 20f, Color.red, 60f);
            }
        }
    }
}
