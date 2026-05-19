using UnityEngine;


public class TwoBogie : RunningGear
{
    [System.Serializable]
    public class Bogie
    {
        public Transform bogieTransform;
        public float wheelOffset;
    }

    [Header("TwoBogie")]
    public Bogie frontBogie;
    public Bogie backBogie;
    public override void UpdateRunningGearPosition(float sectionProgress, TrackSection section)
    {
        //TwoBogie
        Vector3 UpdateBogie(Bogie bogie, float bogieProgress)
        {
            TrackManager.active.GetTrackPositionFromProgress(bogieProgress + bogie.wheelOffset, section, out Vector3 posFront);
            TrackManager.active.GetTrackPositionFromProgress(bogieProgress - bogie.wheelOffset, section, out Vector3 posBack);

            bogie.bogieTransform.LookAt(bogie.bogieTransform.position + posFront - posBack);

            return (posFront + posBack) * 0.5f;
        }

        Vector3 posFront = UpdateBogie(frontBogie, sectionProgress + wheelOffset);
        Vector3 posBack = UpdateBogie(backBogie, sectionProgress - wheelOffset);

        railCarTransform.position = (posFront + posBack) * 0.5f;
        railCarTransform.LookAt(posFront);

    }
}
