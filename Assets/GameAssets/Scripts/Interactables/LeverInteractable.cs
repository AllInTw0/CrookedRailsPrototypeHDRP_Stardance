using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LeverInteractable : Interactable
{
    enum LeverType
    {
        Rotation,
        Position
    }
    [Header("Lever")]
    [SerializeField]
    private LeverType leverType;
    [SerializeField] 
    private Vector3 startAxis;
    [SerializeField]
    private Vector3 endAxis;
    [SerializeField] 
    public int notches;
    [SerializeField] 
    private float speed;
    [SerializeField] 
    private bool displayNotches = true;
    //RunTime
    private NetworkVariable<byte> serverCurrentNotch = new (writePerm: NetworkVariableWritePermission.Server);
    private List<byte> localCurrentNotchHistoryList = new List<byte>();
    private bool locked = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer == false)
        {
            serverCurrentNotch.OnValueChanged += ServerNotchChanged;
        }
    }

    private void ServerNotchChanged(byte previous, byte current)
    {
        if (localCurrentNotchHistoryList.Count > 0)
        {
            if (localCurrentNotchHistoryList[0] == current)
            {
                localCurrentNotchHistoryList.RemoveAt(0);
            }
            else
            {
                //Not expected remove all local inputs
                localCurrentNotchHistoryList.Clear();
            }
        }
    }

    public int GetCurrentNotch()
    {
        if (localCurrentNotchHistoryList.Count > 0)
            return localCurrentNotchHistoryList[^1];
        return serverCurrentNotch.Value;
    }
    private void Update()
    {
        Vector3 targetVector = Vector3.Lerp(startAxis,endAxis, GetCurrentNotch() / (float)notches);
        if (leverType == LeverType.Rotation)
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(targetVector), speed * Time.deltaTime);
        else
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetVector, speed * Time.deltaTime);
    }

    public override bool Interact()
    {
        if (locked)
        {
            base.Interact();//Play sound
            return false;
        }

        int notch = GetCurrentNotch() + 1;
        if (notch > notches)
            notch = 0;

        if (IsServer)
        {
            serverCurrentNotch.Value = (byte)notch;
        }
        else
        {
            localCurrentNotchHistoryList.Add((byte)notch);
            InteractRpc();
        }

        InteractIcon.active.Refresh();
        return base.Interact();
    }
    [Rpc(SendTo.Server)]
    private void InteractRpc()
    {
        int notch = GetCurrentNotch() + 1;
        if (notch > notches)
            notch = 0;
        serverCurrentNotch.Value = (byte)notch;
    }

    public void SetCurrentNotch(int value)
    {
        if (IsServer)
        {
            serverCurrentNotch.Value = (byte)value;
        }
        else
        {
            localCurrentNotchHistoryList.Clear();
        }
    }
    public override string GetName()
    {
        if (objectNameOverride != "")
            return objectNameOverride;
        else if (locked)
            return objectName;
        else
        {
            if (displayNotches)
                return objectName + " [" + GetCurrentNotch() + "/" + notches + "]";

            //else
            return base.GetName();
        }
    }
    public override string GetAction()
    {
        if (actionNameOverride != "")
            return actionNameOverride;
        else if (locked)
            return "Locked";
        else
            return base.GetAction();
    }
    public void SetLocked(bool locked)
    {
        this.locked = locked;
        InteractIcon.active.Refresh();
    }
}
