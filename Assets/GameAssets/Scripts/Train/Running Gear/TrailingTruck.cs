using UnityEngine;

public class TrailingTruck : RunningGear
{
    [Header("LeadTruck")]
    public Transform traillingTruckTransform;

    public override void UpdateRunningGearPosition(float sectionProgress, TrackSection section)
    {
        TrackManager.active.GetTrackPositionFromProgress(sectionProgress + wheelOffset, section, out Vector3 pos);

        Vector3 targetPos = NearestPointOnLine(traillingTruckTransform.position, traillingTruckTransform.position + traillingTruckTransform.right, pos);
        Vector3 localPos = traillingTruckTransform.parent.InverseTransformPoint(targetPos);
        traillingTruckTransform.localPosition = new Vector3(localPos.x, traillingTruckTransform.localPosition.y, traillingTruckTransform.localPosition.z);
    }

    public Vector3 NearestPointOnLine(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 line = (end - start);
        float length = line.magnitude;
        line = line.normalized;

        Vector3 v = point - start;
        float d = Vector3.Dot(v, line);
        return start + line * d;
    }
}
