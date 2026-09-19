using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class HaulingJobMonitorHandler : NetworkBehaviour
{
    public static HaulingJobMonitorHandler active;
    public static NetworkVariable<bool> haulingJobPicked = new(writePerm:NetworkVariableWritePermission.Server);
    enum State
    {
        Welcome,
        Stats,
        ChooseHaulingJob,
        HaulingJobChosen
    }

    [SerializeField]
    private MonitorArm mainMonitorArm;
    [SerializeField]
    private List<MonitorArm> monitorArmList;

    private NetworkVariable<MonitorNetworkData> networkData = new(writePerm: NetworkVariableWritePermission.Server);
    private List<State> clientCurrentStateList = new List<State>();
    private int clientTargetMonitor = -1;
    
    private struct MonitorNetworkData : INetworkSerializable
    {
        public State state;
        public int targetMonitor;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref state);
            serializer.SerializeValue(ref targetMonitor);
        }
    }
    
    private bool haulingJobsGenerated = false;
    private void Start()
    {
        active = this;
        if(IsServer) haulingJobPicked.Value = false;
        
        networkData.OnValueChanged += ServerStateChanged;
    }
    private void ServerStateChanged(MonitorNetworkData previous, MonitorNetworkData current)
    {
        if (IsServer)
        {
            if(previous.state == State.Welcome && current.state == State.Stats)
                Player.ReviveDeadPlayers();
            
            if (previous.state == State.Stats && current.state == State.ChooseHaulingJob)
            {
                Debug.Log("Reciept");
                float sum = 0f;
                List<CargoInfo> cargoList = PlayerTrain.active.GetConsistCargoInfo();
                for (int i = 0; i < cargoList.Count; i++)
                {
                    if(cargoList[i].GetValueSum() != 0)
                        sum += cargoList[i].GetPaySum();
                }

                Money.AddMoney(Mathf.RoundToInt(sum));

                PlayerTrain.active.RemoveNonPlayerRailCars();
                PlayerTrain.active.HeallAllRailCars();
            }

            if (current.state == State.HaulingJobChosen)
            {
                LoadHaulingJob(HaulingJobManager.generatedHaulingJobList[current.targetMonitor]);
            }
            SetupMonitors();
        }
        else
        {
            if (clientCurrentStateList.Count > 0)
            {
                if (clientCurrentStateList[0] == current.state && current.targetMonitor == -1)
                {
                    clientCurrentStateList.RemoveAt(0);
                }
                else
                {
                    //Not expected remove all local inputs
                    clientCurrentStateList.Clear();
                    clientTargetMonitor = -1;
                    SetupMonitors();
                }
            }
            else
            {
                SetupMonitors();
            }
        }
    }

    private int GetTargetMonitor()
    {
        if (IsServer)
        {
            return networkData.Value.targetMonitor;
        }
        else
        {
            if(clientTargetMonitor == -1)
                return networkData.Value.targetMonitor;
            return clientTargetMonitor;
        }
    }

    private State GetCurrentState()
    {
        if (IsServer)
        {
            return networkData.Value.state;
        }
        else
        {
            if(clientCurrentStateList.Count > 0)
                return clientCurrentStateList[^1];
            return networkData.Value.state;
        }
    }

    private void SetState(State state, int targetMonitor = -1)
    {
        if (IsServer == false)
        {
            clientCurrentStateList.Add(state);
            clientTargetMonitor = targetMonitor;
            SetupMonitors();
        }
        
        SetStateServerRpc(state, targetMonitor);
    }
    [Rpc(SendTo.Server)]
    private void SetStateServerRpc(State state, int targetMonitor)
    {
        if ((int)state - 1 == (int)networkData.Value.state)
        {
            networkData.Value = new MonitorNetworkData()
            {
                state = state,
                targetMonitor = targetMonitor
            };
        }
        else
        {
            Debug.LogWarning("Server does not agree!!!" + networkData.Value.state +" -> " + state);
        }
    }
    public void PlayerEntered()
    {
        //Player has steped inside of the trigger
        if(IsServer == false)
            return;
        
        if(haulingJobsGenerated == false)
        {
            FindPathsClientRpc();
            
            LoadingScreen.active.Enable("Finding");
            //Thread for finding train track paths
            GenerationManager.active.FindPathsRequest(24, 3, 1f, OnPathsFound);
        }
        else
        {
            EnableMonitorsRpc();
        }
    }
    [Rpc(SendTo.NotServer)]
    private void FindPathsClientRpc()
    {
        LoadingScreen.active.Enable("Finding");
        LoadingScreen.active.SetProgress(0, "Waiting For Host");
    }
    public void OnPathsFound(object _pathList)
    {
        List<NodePath> pathList = (List<NodePath>)_pathList;
        HaulingJobManager.active.GenerateNewHaulingJobList(monitorArmList.Count, pathList);//Syncs with clients
        haulingJobsGenerated = true;
        LoadingScreen.active.Disable();
        PlayerEntered();
        
        PathsFoundClientRpc();
    }
    [Rpc(SendTo.NotServer)]
    private void PathsFoundClientRpc()
    {
        LoadingScreen.active.Disable();
    }
    public void PlayerExited()
    {
        //Player has steped outside of the trigger
        DisableMonitorsRpc();
    }
    [Rpc(SendTo.Everyone)]
    private void DisableMonitorsRpc()
    {
        if (GetCurrentState() != State.HaulingJobChosen)
        {
            ResetMonitors();
            foreach (MonitorArm monitor in monitorArmList)
            {
                monitor.DisableMonitor();
            }
        }
    }
    [Rpc(SendTo.Everyone)]
    private void EnableMonitorsRpc()
    {
        if (GetCurrentState() != State.HaulingJobChosen)
            SetupMonitors();
    }
    private void SetupMonitors()
    {
        ResetMonitors();
        switch (GetCurrentState())
        {
            case State.Welcome:
                mainMonitorArm.EnableMonitor();
                mainMonitorArm.printer.AddNotification(PaperRenderer.active.RenderPaper("StationWelcome", new List<Override>()), float.MaxValue);
                mainMonitorArm.onInteract.AddListener(() => {
                    SetState(State.Stats);
                });
                break;
            case State.Stats:
                mainMonitorArm.EnableMonitor();

                Override newOverride = new Override("HaulReceipt", OverrideType.HaulReceipt);
                newOverride.cargoListOverride = PlayerTrain.active.GetConsistCargoInfo();

                mainMonitorArm.printer.AddNotification(PaperRenderer.active.RenderPaper("HaulReceipt", new List<Override>() { newOverride }), float.MaxValue);
                mainMonitorArm.onInteract.AddListener(() => {
                    SetState(State.ChooseHaulingJob);
                });
                break;
            case State.ChooseHaulingJob:
                for (int i = 0; i < monitorArmList.Count; i++)
                {
                    monitorArmList[i].EnableMonitor();

                    Override newOverride1 = new Override("HaulingJob", OverrideType.HaulingJobEntry);
                    newOverride1.haulingJobOverride = HaulingJobManager.generatedHaulingJobList[i];

                    monitorArmList[i].printer.AddNotification(PaperRenderer.active.RenderPaper("HaulingJob", new List<Override>() { newOverride1 }), float.MaxValue);

                    int index = i;
                    monitorArmList[i].onInteract.AddListener(() =>
                    {
                        SetState(State.HaulingJobChosen,index);
                    });
                }
                break;
            case State.HaulingJobChosen:
                Debug.Log("Target monitor: " + GetTargetMonitor() +" Server: " +networkData.Value.targetMonitor +" Client: " + clientTargetMonitor);
                if (monitorArmList[GetTargetMonitor()].IsMonitorEnabled() == false)
                {
                    monitorArmList[GetTargetMonitor()].EnableMonitor();

                    Override newOverride2 = new Override("HaulingJob", OverrideType.HaulingJobEntry);
                    newOverride2.haulingJobOverride = HaulingJobManager.generatedHaulingJobList[GetTargetMonitor()];

                    monitorArmList[GetTargetMonitor()].printer.AddNotification(PaperRenderer.active.RenderPaper("HaulingJob", new List<Override>() { newOverride2 }), float.MaxValue);
                }

                for (int j = 0; j < monitorArmList.Count; j++)
                {
                    if (GetTargetMonitor() != j)
                    {
                        monitorArmList[j].DisableMonitor();
                    }
                }
                break;
            default:
                break;
        }
    }
    public void ResetMonitors()
    {
        foreach (MonitorArm monitor in monitorArmList)
        {
            monitor.onInteract.RemoveAllListeners();

            if (GetCurrentState() == State.HaulingJobChosen)
                monitor.DisableButton();
        }
    }
    public void LoadHaulingJob(HaulingJob haulingJob)
    {
        GameStateManager.currentHaulingJob = haulingJob;
        haulingJobPicked.Value = true;
        foreach (HaulingJobEntry haulingJobEntry in haulingJob.haulingJobEntryList)
        {
            RailCar railCar = PlayerTrain.active.AddRailCar(haulingJobEntry.railCar, 2); // 2 because 0-locomotive, 1-tender

            railCar.SetCargo(haulingJobEntry.cargo, Mathf.RoundToInt(haulingJobEntry.pay * 0.6f), Mathf.RoundToInt(haulingJobEntry.pay * 0.4f));
        }
    }

    private void OnDestroy()
    {
        if (active == this) {
            active = null;
            haulingJobPicked.Value = false;
        }
    }
}
