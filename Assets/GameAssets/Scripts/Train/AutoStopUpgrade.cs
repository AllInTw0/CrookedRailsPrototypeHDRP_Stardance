using System.Collections.Generic;
using UnityEngine;

public class AutoStopUpgrade : MonoBehaviour
{
    [Header("Train")]
    [SerializeField]
    private Train targetTrain;
    [Header("Controlls")]
    [SerializeField]
    private LeverInteractable leverInteractable;
    [System.Serializable]
    public class AutoStopFilter
    {
        public string name;
        public List<AutoStopType> autoStopFilterList;
    }
    [SerializeField]
    private List<AutoStopFilter> notchAutoStopFilterList = new List<AutoStopFilter>();

    private float maxAutoStopOffset;
    private AutoStop currentAutoBreakingStop;
    void Start()
    {
        foreach (AutoStopType type in System.Enum.GetValues(typeof(AutoStopType)))
        {
            float offset = GetAutoStopTypeOffset(type);
            if (offset > maxAutoStopOffset)
                maxAutoStopOffset = offset;
        }
    }

    // Update is called once per frame
    void Update()
    {
        AutoStopFilter currentFilter;
        if (leverInteractable != null)
        {
            currentFilter = notchAutoStopFilterList[leverInteractable.currentNotch];
            int nextNotch = leverInteractable.currentNotch + 1;
            if (nextNotch >= notchAutoStopFilterList.Count)
                nextNotch = 0;
            leverInteractable.SetActionNameOverride("Set To " + notchAutoStopFilterList[nextNotch].name);
        }
        else
        {
            currentFilter = notchAutoStopFilterList[0];
        }

        //Handle AutoStops
        TrackManager.active.GetTrackSectionFromProgress(targetTrain.sectionProgress - maxAutoStopOffset, targetTrain.frontTrackSection, out TrackSection newSection, out float newSectionProgress);
        if (targetTrain.deceleration == 0f && TrackManager.active.GetNearestAutoStop(newSectionProgress, newSection, 3, out AutoStop nearestAutoStop, out float distanceToAutoStop, currentFilter.autoStopFilterList))
        {
            TrackManager.active.GetTrackPositionFromProgress(newSectionProgress, newSection, out Vector3 pos1);
            TrackManager.active.GetTrackPositionFromProgress(newSectionProgress + distanceToAutoStop, newSection, out Vector3 pos2);
            Debug.DrawLine(pos1 + Vector3.up, pos2 + Vector3.up, Color.violetRed);

            float distanceToStop = (targetTrain.speed * targetTrain.speed) / (2 * targetTrain.controlls.GetDeceleration()); //v^2 - v0^2 = 2as

            //Handle diffrent types of autostop type distances
            float autoStopOffset = GetAutoStopTypeOffset(nearestAutoStop.stopType);
            distanceToAutoStop -= maxAutoStopOffset - autoStopOffset;

            //Debug.Log("Distance to stop: " + distanceToStop + ", distance: " + distanceToAutoStop);

            if (distanceToAutoStop <= distanceToStop)
            {
                Debug.Log("Engaging breaks! Detected autostop");
                if (nearestAutoStop.stopType == AutoStopType.Supersonic || nearestAutoStop.stopType == AutoStopType.Station || nearestAutoStop.stopType == AutoStopType.SlowDown)
                {
                    targetTrain.controlls.LockControlls();
                }
                else
                {
                    targetTrain.controlls.Break();
                }
                currentAutoBreakingStop = nearestAutoStop;
                nearestAutoStop.ignore = true;
            }
        }

        if (currentAutoBreakingStop != null)
        {
            if (Mathf.Abs(targetTrain.speed) < TrainUpgradeHandler.active.GetStatValue(TrainStatType.Speed) && currentAutoBreakingStop.stopType == AutoStopType.SlowDown)
            {
                targetTrain.controlls.Unlock();
                targetTrain.controlls.SetFullThrottle();
                currentAutoBreakingStop = null;
                PlayerMovement.active.UnFreeze();
            }
            else if (Mathf.Abs(targetTrain.speed) < 0.01f)
            {
                if (currentAutoBreakingStop.stopType == AutoStopType.Supersonic || currentAutoBreakingStop.stopType == AutoStopType.Station)
                {
                    targetTrain.controlls.ActivateSupersonic();
                    if (currentAutoBreakingStop.stopType == AutoStopType.Station) targetTrain.controlls.onSetOffTriggerGenerationReset = true;
                }
                currentAutoBreakingStop = null;
                //Unfreze players if the train was supersonic
                PlayerMovement.active.UnFreeze();
            }
        }
    }

    public float GetAutoStopTypeOffset(AutoStopType type)
    {
        if (type == AutoStopType.Front || type == AutoStopType.Supersonic)
            return 0f;
        else if (type == AutoStopType.TenderHatch)
        {
            List<RailCar> consist = targetTrain.GetConsist();
            float offset = 0f;
            for (int i = 0; i < consist.Count; i++)
            {
                offset += consist[i].frontLength;
                if (consist[i].TryGetComponent(out Tender tender))
                {
                    return offset += tender.waterHatchOffset;
                }
                offset += consist[i].backLength;
            }
        }
        return 0f;
    }
}
