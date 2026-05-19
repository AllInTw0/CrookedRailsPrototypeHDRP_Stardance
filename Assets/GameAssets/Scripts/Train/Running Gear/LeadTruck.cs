using UnityEngine;

public class LeadTruck : RunningGear
{
    [Header("LeadTruck")]
    public Transform leadTruckTransform;

    public override void UpdateRunningGearPosition(float sectionProgress, TrackSection section)
    {
        TrackManager.active.GetTrackPositionFromProgress(sectionProgress + wheelOffset, section, out Vector3 pos);

        leadTruckTransform.LookAt(pos);
    }
}
