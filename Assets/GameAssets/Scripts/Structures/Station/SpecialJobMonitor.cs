using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SpecialJobMonitor : NetworkBehaviour
{
    [SerializeField]
    private List<SpecialJobSO> SpecialJobList  = new List<SpecialJobSO>();
    [SerializeField] 
    private MonitorArm jobMonitor;

    private SpecialJobSO selectedJob;
    private enum State
    {
        PreviewJob,
        Disabled
    }
    private NetworkVariable<State> networkState = new(writePerm: NetworkVariableWritePermission.Server);
    private void Start()
    {
        networkState.Value = State.PreviewJob;
        selectedJob = SpecialJobList[0];
        jobMonitor.onInteract.AddListener(() =>
        {
            Debug.Log("[SpecialJobMonitor] 1");
            if (networkState.Value == State.PreviewJob)
            {
                InteractServerRpc();
            }
        });

        networkState.OnValueChanged += UpdateMonitor;
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc()
    {
        if (networkState.Value == State.PreviewJob && selectedJob.OrderJob())
        {
            networkState.Value = State.Disabled;
        }
    }

    private void UpdateMonitor(State previousState = default, State newState = default)
    {
        if (networkState.Value == State.PreviewJob)
        {
            jobMonitor.EnableMonitor();
            jobMonitor.printer.ClearNotifications();

            Override override1 = new Override("Title", OverrideType.Text);
            override1.stringOverride = selectedJob.title;
            Override override2 = new Override("ItemIcon", OverrideType.RawImageTexture);
            override2.textureOverride = selectedJob.GetIcon();
            Override override3 = new Override("ItemDescription", OverrideType.Text);
            override3.stringOverride = selectedJob.GetDescription();
            Override override4 = new Override("ItemShopStats", OverrideType.Text);
            override4.stringOverride = selectedJob.GetUnderText();

            jobMonitor.printer.AddNotification(
                PaperRenderer.active.RenderPaper("ItemInfo",
                    new List<Override>() { override1, override2, override3, override4 }), float.MaxValue);
        }
        else if (networkState.Value == State.Disabled)
        {
            jobMonitor.DisableMonitor();
            //jobMonitor.printer.ClearNotifications();
        }
    }
    public void PlayerEntered()
    {
        if (HaulingJobMonitorHandler.active && HaulingJobMonitorHandler.haulingJobPicked.Value)
        {
            UpdateMonitor();
        }
    }

    public void PlayerLeft()
    {
        jobMonitor.DisableMonitor();
        jobMonitor.printer.ClearNotifications();
    }
}
