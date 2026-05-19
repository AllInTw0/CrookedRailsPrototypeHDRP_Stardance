using System;
using UnityEngine;

public class LocomotiveControls : MonoBehaviour
{
    [SerializeField] 
    private Train train;
    
    [Header("Controls")]
    [SerializeField] 
    private LeverInteractable throttle;
    [SerializeField] 
    private LeverInteractable reverser;
    [SerializeField] 
    private LeverInteractable locoBreaks;
    
    [Header("Supersonic")]
    [SerializeField]
    private float supersonicSpeed;
    [SerializeField]
    private float supersonicAcceleration;
    [SerializeField]
    private float supersonicDeceleration;
    //Run Time
    private int throttleNotch;
    private int reverserNotch;
    private int locoBreaksNotch;

    public enum State
    {
        normal,
        supersonic,
        locked,
        depletedFuel,
        derailed
    }
    [HideInInspector]
    public State currentState;
    [HideInInspector]
    public bool onSetOffTriggerGenerationReset;
    private void Start()
    {
        onSetOffTriggerGenerationReset = true;
    }
    private void LateUpdate()
    {
        if (currentState == State.locked)
        {
            throttle.currentNotch = throttleNotch;
            locoBreaks.currentNotch = locoBreaksNotch;
            reverser.currentNotch = reverserNotch;
            return;
        }
        else if (currentState == State.supersonic)
        {
            if (throttle.currentNotch > 0)
            {
                throttleNotch = throttle.notches;
            }
            if (throttleNotch > 0)
            {
                //if (onSetOffTriggerGenerationReset)
                //{
                //    onSetOffTriggerGenerationReset = false;
                //    GenerationManager.active.GenerateTillNextStation();
                //}

                if(onSetOffTriggerGenerationReset && (HaulingJobMonitorHandler.active != null && HaulingJobMonitorHandler.haulingJobPicked == false))
                {
                    //throttle.SetActionNameOverride("Pick a job first!");
                    throttle.currentNotch = throttleNotch;
                    throttleNotch = 0;

                    locoBreaks.currentNotch = 0;
                    locoBreaksNotch = 0;

                    reverser.currentNotch = 0;
                    reverserNotch = 0;
                    return;
                }

                if (throttle.actionNameOverride != "")
                {
                    //Freeze the players to the train
                    PlayerMovement.active.Freeze(Train.playerTrain.GetRailCarAtIndex(0));
                }
                throttle.SetActionNameOverride("");
                throttle.currentNotch = throttleNotch;

                locoBreaks.currentNotch = 0;
                locoBreaksNotch = 0;

                reverser.currentNotch = 0;
                reverserNotch = 0;

                train.SetMaxSpeed(supersonicSpeed);
                train.SetAcceleration(supersonicAcceleration);
                train.SetDeceleration(0f);
            }
        }
        else if (currentState == State.depletedFuel)
        {
            //Fuel
            if (train.HasEnoughFuel(out string depletedFuelSourceName))
            {
                throttle.SetActionNameOverride("");
                throttle.SetLocked(false);
                currentState = State.normal;
            }

            //Controlls
            UpdateThrottle();
            UpdateBreaks();
            UpdateReverser();
        }
        else if (currentState == State.normal) 
        {
            if(train.HasEnoughFuel(out string depletedFuelSourceName) == false)
            {
                throttle.SetActionNameOverride("No " + depletedFuelSourceName + "!");
                throttle.SetLocked(true);
                throttle.currentNotch = 0;
                currentState = State.depletedFuel;
            }

            //Controlls
            UpdateThrottle();
            UpdateBreaks();
            UpdateReverser();
        }

        if (throttleNotch > 0)
        {
            if (onSetOffTriggerGenerationReset)
            {
                onSetOffTriggerGenerationReset = false;
                GenerationManager.active.GenerateTillNextStation();
            }
        }
    }
    public void UpdateThrottle()
    {
        if (throttleNotch != throttle.currentNotch)
        {
            throttleNotch = throttle.currentNotch;

            locoBreaks.currentNotch = 0;
            locoBreaksNotch = 0;

            train.SetMaxSpeed((throttleNotch / (float)throttle.notches) * TrainUpgradeHandler.active.GetStatValue(TrainStatType.Speed));
            if (throttleNotch == 0)
                train.SetAcceleration(0f);
            else
                train.SetAcceleration(reverserNotch == 0 ? TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration) : -TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration));

            train.SetDeceleration(0f);
        }
    }
    public void UpdateBreaks()
    {
        if (locoBreaksNotch != locoBreaks.currentNotch)
        {
            locoBreaksNotch = locoBreaks.currentNotch;

            throttle.currentNotch = 0;
            throttleNotch = 0;

            train.SetMaxSpeed(0f);
            train.SetAcceleration(0f);
            train.SetDeceleration(TrainUpgradeHandler.active.GetStatValue(TrainStatType.Deceleration));
        }
    }
    public void UpdateReverser()
    {
        if (reverserNotch != reverser.currentNotch)
        {
            reverserNotch = reverser.currentNotch;

            if (throttleNotch == 0)
                train.SetAcceleration(0f);
            else
                train.SetAcceleration(reverserNotch == 0 ? TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration) : -TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration));
        }
    }
    public void Break()
    {
        throttle.currentNotch = 0;
        throttleNotch = 0;

        locoBreaks.currentNotch = 1;
        locoBreaksNotch = 1;

        train.SetMaxSpeed(0f);
        train.SetAcceleration(0f);
        train.SetDeceleration(GetDeceleration());

        //if (supersonic)
        //{
        //    //UnFreeze the players from the train
        //MOVED TO TRAIN.CS
        //}
        currentState = State.normal;//supersonic = false;
    }
    public void SetDerailled()
    {
        LockControlls();
        throttle.SetActionNameOverride("Derailed!");
        currentState = State.derailed;
    }
    public void LockControlls()
    {
        Break();

        currentState = State.locked; //locked = true;   
        reverser.currentNotch = 0;
        reverserNotch = 0;

        throttle.SetLocked(true);
        locoBreaks.SetLocked(true);
        reverser.SetLocked(true);
    }
    public void ActivateSupersonic()
    {
        currentState = State.supersonic;
        //supersonic = true;
        //locked = false;

        throttle.SetActionNameOverride("Set off");

        throttle.SetLocked(false);
        locoBreaks.SetLocked(true);
        reverser.SetLocked(true);
    }
    public void Unlock()
    {
        currentState = State.normal;//locked = false;
        throttle.SetActionNameOverride("");
        throttle.SetLocked(false);
        locoBreaks.SetLocked(false);
        reverser.SetLocked(false);
    }
    public void SetFullThrottle()
    {
        throttle.currentNotch = throttle.notches;
        UpdateThrottle();

        locoBreaks.currentNotch = 0;
        UpdateBreaks();
    }

    public float GetDeceleration()
    {
        if (currentState == State.supersonic)
            return supersonicDeceleration;
        else
            return TrainUpgradeHandler.active.GetStatValue(TrainStatType.Deceleration);
    }
}
