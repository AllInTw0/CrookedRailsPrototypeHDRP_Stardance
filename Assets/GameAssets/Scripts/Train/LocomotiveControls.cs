using System;
using Unity.Netcode;
using UnityEngine;

public class LocomotiveControls : NetworkBehaviour
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

    public enum State
    {
        normal,
        supersonic,
        locked,
        depletedFuel,
        derailed
    }
    [HideInInspector]
    public NetworkVariable<State> currentState = new(writePerm:NetworkVariableWritePermission.Server);

    public void SetState(State newState)
    {
        if(IsServer)
            currentState.Value = newState;
    }
    [HideInInspector]
    public NetworkVariable<bool> onSetOffTriggerGenerationReset = new(writePerm:NetworkVariableWritePermission.Server);
    private void Start()
    {
        throttle.interactEvent.AddListener(() =>
        {
            if (currentState.Value == State.normal)
            {
                locoBreaks.SetCurrentNotch(0);

                train.SetMaxSpeed((throttle.GetCurrentNotch() / (float)throttle.notches) * TrainUpgradeHandler.active.GetStatValue(TrainStatType.Speed));
                if (throttle.GetCurrentNotch() == 0)
                    train.SetAcceleration(0f);
                else
                    train.SetAcceleration(reverser.GetCurrentNotch() == 0 ? TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration) : -TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration));

                train.SetDeceleration(0f);
            }
            else if (currentState.Value == State.supersonic)
            {
                if (throttle.GetCurrentNotch() > 0)
                {
                    if (throttle.actionNameOverride == "Set off")
                    {
                        if(IsServer)
                            FreezePlayersToTrainRpc();
                    }

                    throttle.SetActionNameOverride("");

                    locoBreaks.SetCurrentNotch(0);
                    reverser.SetCurrentNotch(0);

                    train.SetMaxSpeed(supersonicSpeed);
                    train.SetAcceleration(supersonicAcceleration);
                    train.SetDeceleration(0f);
                }
            }
        });
        reverser.interactEvent.AddListener(() =>
        {
            if (currentState.Value == State.normal)
            {
                if (throttle.GetCurrentNotch() == 0)
                    train.SetAcceleration(0f);
                else
                    train.SetAcceleration(reverser.GetCurrentNotch() == 0 ? TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration) : -TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration));
            }
        });
        locoBreaks.interactEvent.AddListener(()=>
        {
            if (currentState.Value == State.normal)
            {
                if (locoBreaks.GetCurrentNotch() == 1)
                {
                    throttle.SetCurrentNotch(0);

                    train.SetMaxSpeed(0f);
                    train.SetAcceleration(0f);
                    train.SetDeceleration(TrainUpgradeHandler.active.GetStatValue(TrainStatType.Deceleration));
                }
            }
        });

        currentState.OnValueChanged += StateChanged;
    }

    private void StateChanged(State previousState, State currentState)
    {
        //Unfreze players if the train was supersonic
        Player.localPlayer.movement.UnFreeze();
        if (currentState == State.normal)
        {
            throttle.SetActionNameOverride("");
            throttle.SetLocked(false);
            locoBreaks.SetLocked(false);
            reverser.SetLocked(false);
        }
        else if (currentState == State.supersonic)
        {
            Break();
            reverser.SetCurrentNotch(0);
            
            throttle.SetLocked(true);
            locoBreaks.SetLocked(true);
            reverser.SetLocked(true);
        }
        else if (currentState == State.locked)
        {
            throttle.SetLocked(true);
            locoBreaks.SetLocked(true);
            reverser.SetLocked(true);
        }
        else if (currentState == State.depletedFuel)
        {
            Break();
            reverser.SetCurrentNotch(0);

            train.HasEnoughFuel(out string depletedFuelSourceName);
            throttle.SetActionNameOverride("No " + depletedFuelSourceName + "!");
            
            throttle.SetLocked(false);
            locoBreaks.SetLocked(true);
            reverser.SetLocked(true);
        }
        else if (currentState == State.derailed)
        {
            Break();
            
            throttle.SetActionNameOverride("Derailed!");
            
            reverser.SetCurrentNotch(0);
            
            throttle.SetLocked(true);
            locoBreaks.SetLocked(true);
            reverser.SetLocked(true);
        }
    }
    private void LateUpdate()
    {
        if (currentState.Value == State.supersonic)
        {
            if (onSetOffTriggerGenerationReset.Value)
            {
                if (HaulingJobMonitorHandler.active != null && HaulingJobMonitorHandler.haulingJobPicked.Value == false)
                {
                    throttle.SetLocked(true);
                    throttle.SetActionNameOverride("Pick a job first!");
                }
                else
                {
                    int playersOnTrainCount = train.GetPlayersOnTrain().Count;
                    if (playersOnTrainCount < Player.playerList.Count)
                    {
                        throttle.SetLocked(true);
                        throttle.SetActionNameOverride("Not all players are on the train! [" + playersOnTrainCount + "/" + Player.playerList.Count + "]");
                    }
                    else
                    {
                        throttle.SetActionNameOverride("Set off");
                        throttle.SetLocked(false);
                    }
                }
            }
            else
            {
                throttle.SetActionNameOverride("Set off");
                throttle.SetLocked(false);
            }
        }
        else if (currentState.Value == State.depletedFuel)
        {
            //Fuel
            if (train.HasEnoughFuel(out string depletedFuelSourceName))
            {
                throttle.SetActionNameOverride("");
                throttle.SetLocked(false);
                if(IsServer) currentState.Value = State.normal;
            }
        }
        else if (currentState.Value == State.normal) 
        {
            if(train.HasEnoughFuel(out string depletedFuelSourceName) == false)
            {
                throttle.SetActionNameOverride("No " + depletedFuelSourceName + "!");
                throttle.SetLocked(true);
                throttle.SetCurrentNotch(0);
                if(IsServer) currentState.Value = State.depletedFuel;
            }
        }

        if (throttle.GetCurrentNotch() > 0)
        {
            if (onSetOffTriggerGenerationReset.Value && IsServer)
            {
                onSetOffTriggerGenerationReset.Value = false;
                GenerationManager.active.GenerateTillNextStation();
            }
        }
    }
    public void Break()
    {
        throttle.SetCurrentNotch(0);
        locoBreaks.SetCurrentNotch(1);

        train.SetMaxSpeed(0f);
        train.SetAcceleration(0f);
        train.SetDeceleration(GetDeceleration());
    }
    public void SetFullThrottle()
    {
        throttle.SetCurrentNotch(throttle.notches);
        locoBreaks.SetCurrentNotch(0);
        locoBreaks.SetCurrentNotch(0);
        
        train.SetMaxSpeed(TrainUpgradeHandler.active.GetStatValue(TrainStatType.Speed));
        train.SetAcceleration(TrainUpgradeHandler.active.GetStatValue(TrainStatType.Acceleration));
    }

    public float GetDeceleration()
    {
        if (currentState.Value == State.supersonic)
            return supersonicDeceleration;
        else
            return TrainUpgradeHandler.active.GetStatValue(TrainStatType.Deceleration);
    }
    
    [Rpc(SendTo.Everyone)]
    private void FreezePlayersToTrainRpc()
    {
        if (Player.localPlayer.movement.IsOnRailCar(out RailCar railCar) && railCar.GetLinkedTrain() == train)
        {
            Player.localPlayer.movement.Freeze(railCar);
        }
        else
        {
            Player.localPlayer.movement.Freeze(Train.playerTrain.GetRailCarAtIndex(0));
        }
    }
    [Rpc(SendTo.Everyone)]
    public void UnFreezePlayersToTrainRpc()
    {
        Player.localPlayer.movement.UnFreeze();
    }
}
