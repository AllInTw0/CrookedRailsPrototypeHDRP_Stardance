using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CargoInfo
{
    public CargoSO cargoInfo;
    public GameObject cargoObject;

    public float cargoValue;
    public Health cargoHealth;

    public float railCarValue;
    public Health railCarHealth;

    public RailCar railCarRefrence;

    public float GetValueSum()
    {
        return cargoValue + railCarValue;
    }
    public string GetCargoHealthPrecentString()
    {
        if (cargoInfo != null)
            if (cargoHealth != null)
                return Mathf.Round((cargoHealth.GetHealth() / cargoHealth.maxHealth) * 100f) + "%";
            else
                return Mathf.Round((railCarHealth.GetHealth() / railCarHealth.maxHealth) * 100f) + "%*";
        else
            return "-";
    }
    public string GetRailCarHealthPrecentString()
    {
        return Mathf.Round((railCarHealth.GetHealth() / railCarHealth.maxHealth) * 100f) + "%";
    }
    public float GetExpensesSum()
    {
        if (cargoHealth != null)
        {
            return GetValueSum() * (2f - (cargoHealth.GetHealth() / cargoHealth.maxHealth) - (railCarHealth.GetHealth() / railCarHealth.maxHealth));
        }
        else
        {
            return GetValueSum() * (2f - 2f * (railCarHealth.GetHealth() / railCarHealth.maxHealth));
        }
    }
    public float GetPaySum()
    {
        return GetValueSum() - GetExpensesSum();
    }
}
public class RailCar : NetworkBehaviour
{
    private Train linkedTrain;
    public NetworkVariable<RailCarNetworkData> networkData = new(writePerm: NetworkVariableWritePermission.Server);
    public struct RailCarNetworkData : INetworkSerializable
    {
        public ushort trainID;
        public int consistIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref trainID);
            serializer.SerializeValue(ref consistIndex);
        }
    }
    //Variables
    [Header("Parameters")]
    public RailCarSO railCarSO;
    public float frontLength
    {
        get
        {
            return frontCoupler.transform.localPosition.z;
        }
    }
    public float backLength
    {
        get
        {
            return -backCoupler.transform.localPosition.z;
        }
    }
    public bool isPlayerRailCar;
    public float derailVisualAngle = 5f;
    public Vector3 derailVisualOffset = new Vector3(0.18f,-0.09f,0f);
    //public float wheelLength = 2f;
    public List<RunningGear> railCarRunningGearList;
    [Header("Health")]
    [SerializeField]
    public RailCarHealth health;
    [Header("Cargo")]
    [SerializeField]
    private Transform cargoOrigin;
    [Header("Couplers")]
    [SerializeField]
    public Coupler frontCoupler;
    [SerializeField]
    public Coupler backCoupler;
    [Header("Sway")]
    [SerializeField]
    private Transform swayParent;
    public float swayApplyMult = 5f;
    public float swaySpring = 0.2f;
    public float swayDrag = 0.025f;
    public float swayMaxAngle = 5f;
    //Run Time
    //[NonSerialized]
    //public TrackSection currentFrontSection;
    [Header("Run Time")] public bool derailed;
    private float derailTime;
    private CargoInfo currentCargo;

    private float speed;
    //sway
    private float swayVelocity;
    private float swayRot;
    private float railCarLastRotY;

    private void ServerValuesChanged(RailCarNetworkData previous, RailCarNetworkData current)
    {
        if(IsServer) 
            return;
        Debug.Log("ServerValuesChanged: " + current.consistIndex+", " + current.trainID);
        //Sync linked train and consist index
        //Will be called when the consist changes (Server spawns in railcars)
        for (int i = 0; i < Train.activeTrainList.Count; i++)
        {
            if (Train.activeTrainList[i].NetworkBehaviourId == current.trainID)
            {
                Debug.Log("Found Train");
                linkedTrain = Train.activeTrainList[i];
                
                List<RailCar> consist = linkedTrain.GetConsist();
                for (int j = 0; j < consist.Count; j++)
                {
                    if (consist[j] == null)
                    {
                        consist.RemoveAt(j);
                        j--;
                    }
                }
                if(consist.Contains(this) == false)
                    consist.Add(this);

                bool sorted = false;
                while (sorted == false)
                {
                    sorted = true;
                    for (int j = 0; j < consist.Count-1; j++)
                    {
                        if(consist[j].networkData.Value.consistIndex > consist[j+1].networkData.Value.consistIndex)
                        {
                            (consist[j], consist[j + 1]) = (consist[j + 1], consist[j]);
                            sorted = false;
                        }
                    }
                }
                
                linkedTrain.SetConsist(consist);
                return;
            }
        }
        Debug.Log("Did not find train");
    }
    private void Awake()
    {
        currentCargo = new CargoInfo();
        currentCargo.railCarRefrence = this;
        currentCargo.railCarHealth = health;

        networkData.OnValueChanged += ServerValuesChanged;
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer == false)
            ServerValuesChanged(default, networkData.Value);
    }

    private void Update()
    {
        derailTime += derailed ? Time.deltaTime : -Time.deltaTime;
        derailTime = Mathf.Clamp01(derailTime);

        UpdateSway();
    }

    private void LateUpdate()
    {
        if (currentCargo != null && currentCargo.cargoObject != null)
            SetCargoPos(currentCargo.cargoObject);
    }

    public void SetCargoPos(GameObject cargoObject)
    {
        cargoObject.transform.position = cargoOrigin.position;
        cargoObject.transform.rotation = cargoOrigin.rotation;
    }
    private void FixedUpdate()
    {
        UpdateSwaySpring();
    }
    public void UpdateRailCar(float sectionProgress, TrackSection trackSection, float distanceTravelled)
    {
        speed = distanceTravelled / Time.deltaTime;
        foreach (RunningGear runningGear in railCarRunningGearList)
        {
            runningGear.UpdateRunningGearPosition(sectionProgress, trackSection);
            runningGear.UpdateRunningGearRotation(distanceTravelled);
        }

        if (derailTime != 0)
        {
            transform.RotateAround(transform.position, transform.forward, derailTime * -derailVisualAngle);
            transform.position += (transform.forward * derailVisualOffset.z + transform.right * derailVisualOffset.x + transform.up * derailVisualOffset.y) * derailTime;
        }
    }
    private void UpdateSway()
    {
        if(swayParent != null)
        {
            float rotDiff = railCarLastRotY - swayParent.transform.eulerAngles.y;
            swayRot += swayVelocity * Time.deltaTime;
            //if (swayVelocity > 0)
            //    swayVelocity -= rotDiff * swayApplyMult; 
            //else
            //    swayVelocity += rotDiff * swayApplyMult;
            swayVelocity -= rotDiff * swayApplyMult;

            swayVelocity = Mathf.Clamp(swayVelocity, -10f, 10f);
            swayRot = Mathf.Clamp(swayRot, -swayMaxAngle, swayMaxAngle);

            swayParent.localRotation = Quaternion.Euler(0f, 0f, swayRot);
            railCarLastRotY = swayParent.transform.eulerAngles.y;
        }
    }
    private void UpdateSwaySpring()
    {
        swayVelocity += -swayRot * swaySpring;
        swayVelocity -= swayVelocity * swayDrag;
    }
    public CargoInfo GetCargoInfo()
    {
        return currentCargo;
    }
    public void SetCargo(CargoSO cargoSO, int cargoValue, int railCarValue)
    {
        NetworkObject cargoPrefabReference = null;
        if (cargoSO.cargoPrefab != null)
        {
            cargoPrefabReference = Instantiate(cargoSO.cargoPrefab).GetComponent<NetworkObject>();
            if (cargoPrefabReference != null)
                cargoPrefabReference.Spawn(true);
            else
                Debug.LogWarning("CARGO PREFAB HAS NO NETWORK OBJECT: " + cargoSO);
        }

        SetCargoRpc(HaulingJobManager.GetCargoSOID(cargoSO), cargoPrefabReference, cargoValue, railCarValue);
    }

    [Rpc(SendTo.Everyone)]
    public void SetCargoRpc(int cargoSOID, NetworkObjectReference cargoPrefab, int cargoValue, int railCarValue)
    {
        Debug.Log("SetCargoRpc: " + gameObject.name);
        CargoSO cargoSO = HaulingJobManager.GetCargoSOByID(cargoSOID);
        
        CargoInfo cargoInfo = new CargoInfo();
        cargoInfo.cargoInfo = cargoSO;
        
        cargoInfo.cargoValue = cargoValue;

        if (cargoPrefab.TryGet(out NetworkObject cargoObject))
        {
            cargoInfo.cargoObject = cargoObject.gameObject;
            cargoInfo.cargoHealth = cargoInfo.cargoObject.GetComponent<Health>();
        }

        cargoInfo.railCarValue = railCarValue;
        cargoInfo.railCarHealth = health;

        cargoInfo.railCarRefrence = this;

        currentCargo = cargoInfo;

    }

    public void DestroyRailCar()
    {
        if (currentCargo != null && currentCargo.cargoObject != null)
        {
            currentCargo.cargoObject.GetComponent<NetworkObject>().Despawn(true);
        }
        
        GetComponent<NetworkObject>().Despawn(true);
    }

    public override void OnDestroy()
    {
        if (IsServer == false && linkedTrain != null)
        {
            List<RailCar> newConsist = linkedTrain.GetConsist();
            newConsist.Remove(this);
            linkedTrain.SetConsist(newConsist);
            Debug.Log("Removed railCar from consist");
        }

        base.OnDestroy();
    }

    public float GetWeight()
    {
        if (currentCargo != null && currentCargo.cargoInfo != null)
            return railCarSO.weight + currentCargo.cargoInfo.weight;

        return railCarSO.weight;
    }
    public bool IsBroken()
    {
        return health.IsBroken();
    }
    public void Derail()
    {
        health.Derail();
    }
    public void Rerail()
    {
        health.Rerail();
    }

    public void SetLinkedTrain(Train train, int consistIndex)
    {
        linkedTrain = train;

        if (IsServer)
        {
            networkData.Value = new RailCarNetworkData()
            {
                trainID = train.NetworkBehaviourId,
                consistIndex = consistIndex
            };
        }
    }

    public Train GetLinkedTrain()
    {
        return linkedTrain;
    }
    public bool IsMoving()
    {
        return Mathf.Abs(speed) > 0.1f;
    }

    public float GetSpeed()
    {
        if (linkedTrain)
            return linkedTrain.speed;
        return speed;
    }
}
