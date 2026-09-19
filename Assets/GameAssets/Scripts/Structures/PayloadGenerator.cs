using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PayloadGenerator : StructureGenerator
{
    [Header("Payload Track")]
    [SerializeField] 
    private string mainPathString;
    [SerializeField] 
    private string preplacedPathString = "payloadTrack*";
    [SerializeField] 
    private float pathLengthForSection = 4f;
    [SerializeField] 
    private Vector2 minMaxOffset;
    [SerializeField] 
    private float handleLength = 4f;
    [SerializeField] 
    private int payloadTrackMeshIndex = 4;
    public override IEnumerator Generate(StructureMaster structureMaster)
    {
        Debug.Log("Generating Payload Structure");
        List<Section> mainPathSectionList = new List<Section>();

        Section previousSection = null;
        //Find first section
        for (int i = structureMaster.structureSectionList.Count-1; i >= 0; i--)
        {
            if (structureMaster.DoStringsMatch(mainPathString,structureMaster.structureSectionList[i].GetAllConnectionList()[0].connectionTransform.name) ||
                structureMaster.DoStringsMatch(preplacedPathString,structureMaster.structureSectionList[i].GetAllConnectionList()[0].connectionTransform.name))
            {
                mainPathSectionList.Add(structureMaster.structureSectionList[i]);
                previousSection = structureMaster.structureSectionList[i];
                break;
            }
        }

        //Find following sections
        bool loop = true;
        while (loop)
        {
            loop = false;
            foreach (Connection connection in previousSection.GetAllConnectionList())
            {
                if(connection.connectedConnection == null || connection.connectedConnection.section == null)
                    continue;
                
                if (structureMaster.DoStringsMatch(mainPathString,connection.connectedConnection.section.GetAllConnectionList()[0].connectionTransform.name) ||
                    structureMaster.DoStringsMatch(preplacedPathString,connection.connectedConnection.section.GetAllConnectionList()[0].connectionTransform.name))
                {
                    if (mainPathSectionList.Contains(connection.connectedConnection.section) == false)
                    {
                        loop = true;
                        mainPathSectionList.Add(connection.connectedConnection.section);
                        previousSection = connection.connectedConnection.section;
                        break;
                    }
                }
            }
        }
        Debug.Log("[Payload] sections: " + mainPathSectionList.Count);
        for (int i = 0; i < mainPathSectionList.Count-1; i++)
        {
            SplineGenerator splineGen = mainPathSectionList[i].GetComponent<SplineGenerator>();
            
            Debug.DrawLine(mainPathSectionList[i].GetAllConnectionList()[0].connectionTransform.position + Vector3.up,
                mainPathSectionList[i + 1].GetAllConnectionList()[0].connectionTransform.position + Vector3.up, Color.cyan, 60f);
        }

        Point lastPoint = null;
        bool modifyTerrain = true;
        List<TrackSection> trackList = new List<TrackSection>();
        List<List<PathPoint>> terrainModifyPathListList = new List<List<PathPoint>>();
        List<PathPoint> terrainModifyPathList = new List<PathPoint>();
        for (int i = 0; i < mainPathSectionList.Count; i++)
        {
            Section section = mainPathSectionList[i];
            SplineGenerator splineGen = mainPathSectionList[i].GetComponent<SplineGenerator>();
            if (splineGen != null)
            {
                List<PathPoint> path = splineGen.GetPath();
                float pathLength = path[^1].distance;

                if (lastPoint == null)
                {
                    Vector3 pos = section.GetAllConnectionList()[1].connectionTransform.position;
                    Vector3 dir = section.GetAllConnectionList()[1].connectionTransform.forward;
                    lastPoint = Spline.CreatePoint(pos, pos - dir * handleLength);
                }

                int sectionCount = Mathf.Clamp(Mathf.RoundToInt(path[^1].distance / pathLengthForSection), 1, 99);
                float interval = path[^1].distance / (float)sectionCount;
                for (int j = 1; j <= sectionCount; j++)
                {
                    Vector3 pos = TrackManager.GetPathPosition(path, pathLength - interval * j);
                    Vector3 dir = -TrackManager.GetPathDirectionVector(path, pathLength - interval * j);
                    Vector3 perpendicularDir = new Vector3(-dir.z, 0, dir.x).normalized;
                    pos += perpendicularDir * Random.Range(minMaxOffset.x, minMaxOffset.y);
                    Point newPoint = Spline.CreatePoint(pos, pos - dir * handleLength, false);

                    TrackSection track = TrackManager.active.CreateTrackSection(lastPoint, newPoint,
                        Spline.active.splineMeshList[payloadTrackMeshIndex]);
                    if(modifyTerrain)
                        terrainModifyPathList.AddRange(track.path);
                    else
                    {
                        terrainModifyPathListList.Add(terrainModifyPathList);
                        terrainModifyPathList = new List<PathPoint>();
                    }

                    trackList.Add(track);
                    lastPoint = newPoint;
                    modifyTerrain = true;
                }
            }
            else
            {
                foreach (Connection connection in section.GetAllConnectionList())
                {
                    if (structureMaster.DoStringsMatch(preplacedPathString,connection.connectionTransform.name))
                    {
                        Vector3 pos = connection.connectionTransform.position;
                        Vector3 dir = connection.connectionTransform.forward;
                        Point newPoint = Spline.CreatePoint(pos, pos - dir * handleLength);
                        
                        if (lastPoint != null)
                        {
                            TrackSection track = TrackManager.active.CreateTrackSection(lastPoint, newPoint,
                                Spline.active.splineMeshList[payloadTrackMeshIndex]);
                            modifyTerrain = false;
                        }
                        lastPoint = newPoint;
                    }
                }
            }
        }
        
        terrainModifyPathListList.Add(terrainModifyPathList);
        yield return new WaitForSecondsRealtime(3f);
        for (int i = 0; i < terrainModifyPathListList.Count; i++)
        {
            if(terrainModifyPathListList[i].Count > 0)
                TrackGeneration.active.ModifyTerrainToFollowPath(terrainModifyPathListList[i], true);
        }
        
    }
}
