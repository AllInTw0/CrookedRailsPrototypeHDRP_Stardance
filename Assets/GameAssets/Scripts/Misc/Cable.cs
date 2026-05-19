using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Cable : MonoBehaviour
{
    [Serializable]
    public class CableEntry
    {
        public Transform start;
        public Transform end;

        public Vector3 startOffset;
        public Vector3 endOffset;

        public float width = 0.01f;
        public float length = 10f;

        public int wireResolution = 5;

        public LineRenderer lineRenderer;
    }

    public Material cableMaterial;
    public List<CableEntry> cableList = new List<CableEntry>();

    private Vector3 lastStartPos;
    private Vector3 lastEndPos;
    private void Start()
    {
        lastEndPos = Vector3.zero;
        lastEndPos = Vector3.zero;
    }
    private void LateUpdate()
    {
        foreach (CableEntry cable in cableList)
        {
            if (cable.lineRenderer == null) 
            {
                GameObject newObject = new GameObject("cable");
                newObject.transform.parent = transform;
                cable.lineRenderer = newObject.AddComponent<LineRenderer>();
            }

            if(cable.start != null && cable.end != null)
                UpdateWire(cable);
        }
    }
    public void UpdateWire(CableEntry cableEntry)
    {
        Vector3 start = cableEntry.start.TransformPoint(cableEntry.startOffset);
        Vector3 end = cableEntry.end.TransformPoint(cableEntry.endOffset);

        float distance = Vector3.Distance(start, end);
        int posCount = cableEntry.wireResolution;
        float wireSagMult = Mathf.Clamp((cableEntry.length - distance) * 0.5f, 0f, cableEntry.length * 0.5f);

        Vector3[] positions = new Vector3[posCount];
        for (int i = 0; i < posCount; i++)
        {
            float time = i / (posCount - 1f);
            Vector3 pos = Vector3.Lerp(start, end, time);

            float modifiedTime = 2f * time - 1f;
            float sag = (1f - modifiedTime * modifiedTime) * wireSagMult;

            pos -= new Vector3(0, sag, 0);

            positions[i] = pos;
        }

        cableEntry.lineRenderer.sharedMaterial = cableMaterial;
        cableEntry.lineRenderer.widthMultiplier = cableEntry.width;

        cableEntry.lineRenderer.positionCount = posCount;
        cableEntry.lineRenderer.SetPositions(positions);
    }
}
