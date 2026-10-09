using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PayloadCraneRailCar : RailCar
{
    [Header("Crane")] 
    [SerializeField]
    private float maxPickUpDistance;
    [SerializeField] 
    private CargoSO payloadCargoSO;
    
    [Header("Parts")]
    [SerializeField] 
    private Transform partBody;
    [SerializeField] 
    private float bodyRotSpeed;
    [SerializeField] 
    private Transform partCrane;
    [SerializeField] 
    private float craneRotSpeed;
    [SerializeField] 
    private float craneLength;
    [SerializeField] 
    private Transform partHook;
    [SerializeField] 
    private float hookSpeed;
    
    private enum PartType
    {
        lookAt,
        rotX
    }
    [Serializable]
    private class PartEntry
    {
        public PartType type;
        public Transform[] transformArray;
        public int targetPartIndex;
        public float speedValue;
    }
    [Header("OtherParts")]
    [SerializeField]
    private List<PartEntry> partEntryList = new List<PartEntry>();
    
    private Payload targetPayload;
    private Transform helperTransform;

    private Quaternion payloadStartRot;
    private Vector3 payloadStartPos;
    private void Start()
    {
        helperTransform = new GameObject("HelperTransform").transform;
    }

    public override void Update()
    {
        base.Update();
        RailCar loadRailCar = null;
        for (int i = 0; i < GetLinkedTrain().GetConsist().Count; i++)
        {
            if (GetLinkedTrain().GetConsist()[i] == this)
            {
                loadRailCar = GetLinkedTrain().GetConsist()[i - 1];
            }
        }
        
        if (targetPayload == null)
        {
            if (IsMoving() == false)
            {
                for (int i = 0; i < Payload.activePayloadList.Count; i++)
                {
                    float distance = Vector3.Distance(Payload.activePayloadList[i].transform.position, transform.position);
                    if (Payload.activePayloadList[i].CanBePickedUp() && distance < maxPickUpDistance)
                    {
                        if (UpdateCraneMovement(Payload.activePayloadList[i].GetHookPos().position))
                        {
                            targetPayload = Payload.activePayloadList[i];
                            targetPayload.LinkCrane(this);
                            payloadStartPos = targetPayload.GetHookPos().position;
                            payloadStartRot = targetPayload.transform.rotation;
                        }
                        return; ///////////////////////////////////////////
                    }
                }
            }
            UpdateCraneMovement(loadRailCar.GetCargoPos() + Vector3.up * 3.5f);
        }
        else
        {
            if (loadRailCar.GetCargoInfo().GetCargoSO() == null)
            {
                Vector3 endPos = loadRailCar.GetCargoPos() + Vector3.up;
                if (UpdateCraneMovement(endPos) && IsServer)
                {
                    loadRailCar.SetCargo(payloadCargoSO, Random.Range(payloadCargoSO.payRange.x, payloadCargoSO.payRange.y), loadRailCar.GetCargoInfo().railCarValue);
                    Health railCarHealth = targetPayload.GetConsist()[0].GetCargoInfo().GetRailCarHealth();
                    Health cargoHealth = loadRailCar.GetCargoInfo().GetCargoHealth();
                    cargoHealth.SetMaxHealth(railCarHealth.GetMaxHealth());
                    cargoHealth.SetHealth(railCarHealth.GetHealth());
                    targetPayload.GetComponent<NetworkObject>().Despawn(true);
                }
                else
                {
                    float dist = Vector3.Distance(payloadStartPos, endPos);
                    float payloadDist = Vector3.Distance(targetPayload.GetHookPos().position, endPos);
                    targetPayload.transform.rotation = Quaternion.Lerp(loadRailCar.transform.rotation, payloadStartRot, payloadDist / dist);
                    targetPayload.transform.position = partHook.position - targetPayload.transform.TransformDirection(targetPayload.GetHookPos().localPosition);
                }
            }
            else
            {
                UpdateCraneMovement(loadRailCar.GetCargoPos() + Vector3.up * 3.5f);
            }
        }
    }

    public bool UpdateCraneMovement(Vector3 targetPos)
    {
        partHook.parent.rotation = Quaternion.Euler(0f, partCrane.parent.rotation.eulerAngles.y, 0f);
        float UpdatePart(int partIndex)
        {
            switch (partIndex)
            {
                case 0:
                    helperTransform.position = partBody.position;
                    helperTransform.LookAt(targetPos);
                    float targetRotY = helperTransform.rotation.eulerAngles.y;
                    float currentRotY = partBody.rotation.eulerAngles.y;
                    float movedRotY = Mathf.MoveTowardsAngle(currentRotY, targetRotY, bodyRotSpeed * Time.deltaTime);
                    float deltaY = movedRotY - currentRotY;
                    partBody.rotation = Quaternion.Euler(0f, movedRotY, 0f);
                    return deltaY;
                case 1:
                    float distance = Vector2.Distance(new Vector2(targetPos.x, targetPos.z), new Vector2(partCrane.position.x, partCrane.position.z));
                    float targetRotX = -Mathf.Acos(distance / craneLength) * Mathf.Rad2Deg;
                    float currentRotX = partCrane.rotation.eulerAngles.x;
                    float movedRotX = Mathf.MoveTowardsAngle(currentRotX, targetRotX, craneRotSpeed * Time.deltaTime);
                    float deltaX = movedRotX - currentRotX;
                    partCrane.rotation = Quaternion.Euler(movedRotX, partCrane.rotation.eulerAngles.y, partCrane.rotation.eulerAngles.z);
                    return deltaX;
                case 2:
                    float currentPosY = partHook.position.y;
                    float targetPosY = targetPos.y;
                    if(Vector2.Distance(new Vector2(targetPos.x, targetPos.z), new Vector2(partHook.parent.position.x, partHook.parent.position.z)) > 1f)
                        targetPosY = partHook.parent.position.y - 1f;
                    float movedPosY = Mathf.MoveTowards(partHook.position.y, targetPosY, hookSpeed * Time.deltaTime);
                    partHook.position = new Vector3(partHook.position.x, movedPosY, partHook.position.z);
                    float deltaPosY = movedPosY - currentPosY;
                    return deltaPosY;
            }

            return 0f;
        }
        
        float[] deltaArray = new float[3];
        deltaArray[2] = UpdatePart(2);
        if (Mathf.Abs(deltaArray[2]) < 0.1f)
        {
            deltaArray[0] = UpdatePart(0);
            deltaArray[1] = UpdatePart(1);
        }

        for (int i = 0; i < partEntryList.Count; i++)
        {
            switch (partEntryList[i].type)
            {
                case PartType.lookAt:
                    partEntryList[i].transformArray[0].LookAt(partEntryList[i].transformArray[1]);
                    break;
                case PartType.rotX:
                    partEntryList[i].transformArray[0].localRotation *= Quaternion.Euler(new Vector3(deltaArray[partEntryList[i].targetPartIndex] * partEntryList[i].speedValue, 0f, 0f));
                    break;
            }
        }
        
        return Vector3.Distance(targetPos,partHook.transform.position) < 0.1f;
    }
}
