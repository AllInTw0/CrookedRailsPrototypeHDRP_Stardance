using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public struct CargoInfo : INetworkSerializable
{
    public int cargoSOID;
    public NetworkObjectReference cargoObject;

    public int cargoValue;
    public NetworkBehaviourReference cargoHealth;

    public int railCarValue;
    public NetworkBehaviourReference railCarHealth;

    public NetworkBehaviourReference railCarRefrence;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cargoSOID);
        serializer.SerializeValue(ref cargoObject);
        serializer.SerializeValue(ref cargoValue);
        serializer.SerializeValue(ref railCarValue);
        serializer.SerializeValue(ref cargoHealth);
        serializer.SerializeValue(ref railCarHealth);
        serializer.SerializeValue(ref railCarRefrence);
    }
    
    public float GetValueSum()
    {
        return cargoValue + railCarValue;
    }

    public NetworkObject GetCargo()
    {
        return cargoObject.TryGet(out NetworkObject cargo) ? cargo : null;
    }
    public CargoSO GetCargoSO()
    {
        return HaulingJobManager.GetCargoSOByID(cargoSOID);
    }
    public Health GetCargoHealth()
    {
        return cargoHealth.TryGet(out Health health) ? health : null;
    }
    public Health GetRailCarHealth()
    {
        return railCarHealth.TryGet(out Health health) ? health : null;
    }
    public RailCar GetRailCar()
    {
        return railCarRefrence.TryGet(out RailCar railCar) ? railCar : null;
    }
    public string GetCargoHealthPrecentString()
    {
        CargoSO cargoInfo = GetCargoSO();
        Health cargoHealth = GetCargoHealth();
        Health railCarHealth = GetRailCarHealth();
        
        if (cargoInfo != null)
            if (cargoHealth != null)
                return cargoHealth.GetHealthPercent() + "%";
            else
                return railCarHealth.GetHealthPercent() + "%*";
        else
            return "-";
    }
    public string GetRailCarHealthPrecentString()
    {
        Health railCarHealth = GetRailCarHealth();
        return railCarHealth.GetHealthPercent()+ "%";
    }
    public float GetExpensesSum()
    {
        Health cargoHealth = GetCargoHealth();
        Health railCarHealth = GetRailCarHealth();
        if (cargoHealth != null)
        {
            return GetValueSum() * (2f - cargoHealth.GetHealthTime() - railCarHealth.GetHealthTime());
        }
        else
        {
            return GetValueSum() * (2f - 2f * railCarHealth.GetHealthTime());
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
        public NetworkBehaviourReference train;
        public int consistIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref train);
            serializer.SerializeValue(ref consistIndex);
        }
    }

    private NetworkVariable<CargoInfo> networkCargo = new(writePerm: NetworkVariableWritePermission.Server);
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

    [Header("Display Board")] 
    [SerializeField]
    private GameObject displayBoardPrefab;
    [SerializeField]
    private List<Transform> displayBoardPositionList = new List<Transform>();
    private List<DisplayBoard> displayBoardList = new List<DisplayBoard>();
    public class DisplayBoard
    {
        public GameObject displayBoardObject;
        public TMP_Text healthText;
        public TMP_Text cargoText;
    }
    
    public void AddDisplayBoards()
    {
        displayBoardList = new List<DisplayBoard>();
        foreach (Transform displayPosTransform in displayBoardPositionList)
        {
            GameObject clone = Instantiate(displayBoardPrefab);
            clone.transform.SetParent(displayPosTransform);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;

            TMP_Text[] textArray = clone.GetComponentsInChildren<TMP_Text>();
            DisplayBoard displayBoard = new DisplayBoard();
            displayBoard.displayBoardObject = clone;
            
            foreach (TMP_Text text in textArray)
            {
                if (text.name == "HealthText") displayBoard.healthText = text;
                //if (text.name == "CargoText") displayBoard.cargoText = text;
            }
            
            displayBoardList.Add(displayBoard);
        }
    }
    
    //Run Time
    //[NonSerialized]
    //public TrackSection currentFrontSection;
    public bool IsDerailed()
    {
        return health.IsDerailed();
    }
    private float derailTime;

    private float speed;
    //sway
    private float swayVelocity;
    private float swayRot;
    private float railCarLastRotY;

    private void ServerValuesChanged(RailCarNetworkData previous, RailCarNetworkData current)
    {
        if(IsServer) 
            return;
        Debug.Log("ServerValuesChanged: " + current.consistIndex+", " + current.train);
        //Sync linked train and consist index
        //Will be called when the consist changes (Server spawns in railcars)

        if (current.train.TryGet(out PlayerTrain train))
        {
            Debug.Log("Found Train");
            linkedTrain = train;
            
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
        Debug.Log("Did not find train");
    }
    private void Awake()
    {
        networkData.OnValueChanged += ServerValuesChanged;
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer)
            networkCargo.Value = new CargoInfo
            {
                cargoSOID = -1,
                railCarRefrence = this,
                railCarHealth = health
            };
        if(IsServer == false)
            ServerValuesChanged(default, networkData.Value);

        AddDisplayBoards();
    }
    
    public virtual void Update()
    {
        derailTime += IsDerailed() ? Time.deltaTime : -Time.deltaTime;
        derailTime = Mathf.Clamp01(derailTime);

        UpdateSway();

        foreach (DisplayBoard displayBoard in displayBoardList)
        {
            if (displayBoard.healthText)
            {
                displayBoard.healthText.text = railCarSO.GetName() + ": " + networkCargo.Value.GetRailCarHealth().GetHealthPercent() + "%";
            
                //Debug.Log(railCarSO.GetName() + ":" +networkCargo.Value.GetCargoSO());
                if (networkCargo.Value.GetCargoSO() && networkCargo.Value.GetCargoHealth())
                {
                    displayBoard.healthText.text += "\n" + networkCargo.Value.GetCargoSO().GetName() + ": " +
                                                   networkCargo.Value.GetCargoHealth().GetHealthPercent() + "%";
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (networkCargo.Value.GetCargo() != null)
            SetCargoPos(networkCargo.Value.GetCargo().gameObject);
    }

    public void SetCargoPos(GameObject cargoObject)
    {
        cargoObject.transform.position = cargoOrigin.position;
        cargoObject.transform.rotation = cargoOrigin.rotation;
    }

    public Vector3 GetCargoPos()
    {
        return cargoOrigin.position;
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
        return networkCargo.Value;
    }
    public void SetCargo(CargoSO cargoSO, int cargoValue, int railCarValue)
    {
        NetworkObject cargoPrefabReference = null;
        if (cargoSO && cargoSO.cargoPrefab != null)
        {
            cargoPrefabReference = Instantiate(cargoSO.cargoPrefab).GetComponent<NetworkObject>();
            if (cargoPrefabReference != null)
                cargoPrefabReference.Spawn(true);
            else
                Debug.LogWarning("CARGO PREFAB HAS NO NETWORK OBJECT: " + cargoSO);
        }


        networkCargo.Value = new CargoInfo
        {
            cargoSOID = HaulingJobManager.GetCargoSOID(cargoSO),
            cargoValue = cargoValue,
            railCarValue = railCarValue,
            
            cargoObject = cargoPrefabReference,
            cargoHealth = cargoPrefabReference != null ? cargoPrefabReference.GetComponent<Health>() : null,
            
            railCarHealth = health,
            railCarRefrence = this
        };
    }
    public void DestroyRailCar()
    {
        if (networkCargo.Value.GetCargo() != null)
        {
            networkCargo.Value.GetCargo().Despawn(true);
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
        if (networkCargo.Value.cargoSOID != -1)
            return railCarSO.weight + networkCargo.Value.GetCargoSO().weight;

        return railCarSO.weight;
    }
    public bool IsBroken()
    {
        return health.IsBroken();
    }
    public void SetDerailed(bool derailed)
    {
        health.SetDerailed(derailed);
    }

    public void SetLinkedTrain(Train _train, int _consistIndex)
    {
        linkedTrain = _train;

        if (IsServer)
        {
            networkData.Value = new RailCarNetworkData()
            {
                train = _train,
                consistIndex = _consistIndex
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
