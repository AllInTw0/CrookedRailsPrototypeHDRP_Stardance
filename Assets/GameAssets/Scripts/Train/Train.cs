using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Train : NetworkBehaviour
{
    public static Train playerTrain;
    public static List<Train> activeTrainList = new List<Train>();
    
    [System.Serializable]
    public class FuelSource
    {
        public TrainStatType consumptionRate;
        public Health targetHealth;
        public string fuelName;

        public float depletion;
        public void Deplete(float value)
        {
            depletion += value;
            int intDepletion = Mathf.FloorToInt(depletion);
            if (intDepletion > 0)
            {
                depletion -= intDepletion;
                targetHealth.TakeDamage(intDepletion);
            }

        }
    }

    //Variables
    [SerializeField]
    private bool isPlayerTrain;
    [Header("Consist")]
    [SerializeField]
    private List<RailCar> consist = new List<RailCar>();
    [SerializeField] 
    public float couplerLength;
    [Header("Fuel")]
    [SerializeField]
    private List<FuelSource> fuelSourceList;
    [Header("Speed")]
    [SerializeField]
    public LocomotiveControls controlls;
    [SerializeField]
    private float drag = 0.25f;
    [SerializeField]
    private float weightAccelerationPenalty = 0.01f;
    [SerializeField]
    private float brokenDownWeightSpeedPenalty = 0.01f;
    [SerializeField]
    private float minSpeed = 1f;
    [Header("Player")]
    [SerializeField]
    private float autoStopPlayerDist = 30f;
    [Header("Debri Detection")]
    [SerializeField]
    private BoxCollider debriBoxCollider;
    [SerializeField]
    private TrainStatType debriDerailChance;
    [SerializeField]
    private LayerMask debriLayerMask;

    //Run Time
    [HideInInspector]
    public float acceleration;
    [HideInInspector]
    public float deceleration;
    [HideInInspector]
    public float maxSpeed;

    [HideInInspector]
    public float speed;
    [HideInInspector]
    public TrackSection frontTrackSection;
    public bool IsInitialized() { return frontTrackSection != null;}
    [HideInInspector]
    public float sectionProgress;
    private float consistLength;

    private float playerDistFromTrain;
    
    private bool consumeFuel;
    
    //Multiplayer
    private NetworkVariable<TrainNetworkData> trainNetworkData = new(writePerm: NetworkVariableWritePermission.Server);
    private struct TrainNetworkData : INetworkSerializable
    {
        public int sectionID;
        public float sectionProgress;
        public float speed;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref sectionID);
            serializer.SerializeValue(ref sectionProgress);
            serializer.SerializeValue(ref speed);
        }
    }
    private void Awake()
    {
        if (isPlayerTrain)
            playerTrain = this;
        activeTrainList.Add(this);
    }

    public override void OnNetworkSpawn()
    {
        trainNetworkData.OnValueChanged += SyncTrainClient;
        AwaitConsist();
    }
    private async void AwaitConsist()
    {
        bool allSpawned = false;
        while (allSpawned == false)
        {
            allSpawned = true;
            for (int i = 0; i < consist.Count; i++)
            {
                if (consist[i].IsSpawned == false)
                    allSpawned = false;
            }
            await Task.Delay(10);
        }
        RecalculateConsistLength();
    }
    private void SyncTrainClient(TrainNetworkData previous, TrainNetworkData current)
    {
        frontTrackSection = TrackManager.active.FindTrackSectionByID(current.sectionID);
        sectionProgress = current.sectionProgress;
        speed = current.speed;
    }
    private void Update()
    {
        //Debug.Log(deceleration);
        if(IsInitialized() == false)
        {
            Debug.LogWarning("No track section assigned!");
            if (IsClient)
            {
                SyncTrainClient(trainNetworkData.Value, trainNetworkData.Value);
            }
            return;
        }
        
        //Player dist
        if (GameStateManager.gameStarted.Value)
        {
            playerDistFromTrain = GetClosestDistanceToPos(Player.localPlayer.transform.position);
            if (autoStopPlayerDist > 0 && playerDistFromTrain >= autoStopPlayerDist && deceleration == 0)
            {
                Debug.Log("Player Is To Far! Engaging breaks!");
                Override title = new Override("Title", OverrideType.Text, "WARNING");
                Override message = new Override("Message", OverrideType.Text, "You're to far from the train! Engaging breaks!");
                Override subText = new Override("SubText", OverrideType.Text, Mathf.Round(playerDistFromTrain) + "m");
                MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Message", new List<Override>() { title, message, subText }));

                controlls.Break();
            }
        }

        //Check if derailed
        bool derailed = false;
        foreach (RailCar railCar in consist)
        {
            if(railCar.IsDerailed())
            {
                derailed = true;
                break;
            }
        }
        if(controlls.currentState.Value == LocomotiveControls.State.derailed)
        {
            if (derailed == false)
                controlls.SetState(LocomotiveControls.State.normal);
        }
        else if(controlls.currentState.Value == LocomotiveControls.State.normal)
        {
            if (derailed)
                controlls.SetState(LocomotiveControls.State.derailed);
        }

        //Update Speed    
        float modifiedMaxSpeed = Mathf.Clamp(maxSpeed - GetBrokenCarWeight() * brokenDownWeightSpeedPenalty, minSpeed, float.MaxValue);
        if (Mathf.Abs(speed) < Mathf.Abs(modifiedMaxSpeed) || (speed > 0 && acceleration < 0) || (speed < 0 && acceleration > 0))
        {
            //Debug.Log((acceleration / (GetConsistWeight() * weightAccelerationPenalty + 1f)));
            speed += (acceleration / (GetConsistWeight() * weightAccelerationPenalty + 1f)) * Time.deltaTime;
            speed = Mathf.Clamp(speed, -modifiedMaxSpeed, modifiedMaxSpeed);
        }

        float decel = 0f;
        if(acceleration == 0f) decel += drag;
        if (deceleration > 0) decel = deceleration;
        if (derailed) decel += 2.5f;
        if (decel > 0f)
        {
            if (speed > 0)
            {
                speed -= decel * Time.deltaTime;
                if (speed < 0)
                    speed = 0;
            }
            if (speed < 0)
            {
                speed += decel * Time.deltaTime;
                if (speed > 0)
                    speed = 0;
            }
        }

        float distanceTravelled = speed * Time.deltaTime;
        sectionProgress += distanceTravelled;

        if(isPlayerTrain) 
            GameStateManager.distanceTravelled += distanceTravelled;

        UpdatePosition(distanceTravelled);
        
 
        //Fuel
        Debug.Log("Consume fuel: " + consumeFuel);
        if(IsServer && consumeFuel && acceleration > 0f && (controlls == null || controlls.currentState.Value == LocomotiveControls.State.normal))
        {
            foreach (FuelSource fuelSource in fuelSourceList)
            {
                fuelSource.Deplete(Mathf.Abs(distanceTravelled) * TrainUpgradeHandler.active.GetStatValue(fuelSource.consumptionRate));
                //Not having enough fuel handles LocomotiveControls.cs
            }
        }
        //Debri
        if(IsServer && debriBoxCollider != null && derailed == false && speed > 0.4f)
        {
            Collider[] debriColliders = Util.PhysicsBoxColliderOverlap(debriBoxCollider, debriLayerMask);
            for (int i = 0; i < debriColliders.Length; i++)
            {
                if (debriColliders[i].TryGetComponent(out Item item) && item.IsPhysicsEnabled() == false)
                {
                    if (Random.Range(0f, 1f) >= 0.5f)
                        item.EnablePhysicsRpc(consist[0].transform.forward * (speed * 1.5f) + consist[0].transform.right * Random.Range(2f, 4f) + consist[0].transform.up * Random.Range(5f, 6f));
                    else
                        item.EnablePhysicsRpc(consist[0].transform.forward * (speed * 1.5f) - consist[0].transform.right * Random.Range(2f, 4f) + consist[0].transform.up * Random.Range(5f, 6f));

                    if (Random.Range(0f, 1f) <= TrainUpgradeHandler.active.GetStatValue(debriDerailChance))
                    {
                        consist[0].SetDerailed(true);
                        break;
                    }
                }
            }
        }
        
        if (IsServer)
        {
            trainNetworkData.Value = new TrainNetworkData()
            {
                
                sectionID = frontTrackSection.sectionID,
                sectionProgress = sectionProgress,
                speed = speed
            };
        }
    }

    private void UpdatePosition(float distanceTravelled = 0f)
    {   
        //Check front
        if (TrackManager.active.GetTrackSectionFromProgress(sectionProgress, frontTrackSection, out TrackSection newSection, out float newSectionProgress))
        {
            //Update section
            sectionProgress = newSectionProgress;
            frontTrackSection = newSection;
        }
        else if(speed > 0)
        {
            //End of track
            Debug.Log("End of track front");
            speed *= -0.5f;
        }


        //Check back
        if (TrackManager.active.GetTrackSectionFromProgress(sectionProgress - consistLength, frontTrackSection, out newSection, out newSectionProgress) == false && speed < 0)
        {
            Debug.Log("End of track back");
            speed *= -0.5f;
        }
        
        float railCarProgress = sectionProgress;
        TrackSection railCarSection = frontTrackSection;
        foreach (var railCar in consist)
        {
            TrackManager.active.GetTrackSectionFromProgress(railCarProgress - railCar.frontLength, railCarSection, out newSection, out newSectionProgress);
            railCarProgress = newSectionProgress;
            railCarSection = newSection;

            railCar.UpdateRailCar(railCarProgress, railCarSection, distanceTravelled);

            railCarProgress -= railCar.backLength + couplerLength;
        }
    }
    public void SetFuelConsumption(bool value)
    {
        consumeFuel = value;
    }
    public void SetAcceleration(float value)
    {
        acceleration = value;
    }
    public void SetDeceleration(float value)
    {
        deceleration = value;
    }
    public void SetMaxSpeed(float value)
    {
        maxSpeed = value;
    }
    public RailCar GetRailCarAtIndex(int index)
    {
        return consist[index];
    }
    public void OffsetProgress(float value)
    {
        sectionProgress += value;
    }
    private void RecalculateConsistLength()
    {
        consistLength = CalculateConsistLength(false);

        //Reset couplers
        for (int i = 0; i < consist.Count; i++)
        {
            if(i + 1 < consist.Count)
            {
                consist[i].backCoupler.ConnectCoupler(consist[i + 1].frontCoupler);
            }

            if (i - 1 >= 0)
            {
                consist[i].frontCoupler.ConnectCoupler(consist[i - 1].backCoupler);
            }
            
            //Assign Train
            consist[i].SetLinkedTrain(this, i);
        }
        

    }
    public float CalculateConsistLength(bool onlyPlayerRailCars = false)
    {
        float length = 0f;
        foreach (RailCar railCar in consist)
        {
            if(onlyPlayerRailCars)
            {
                if(railCar.isPlayerRailCar)
                    length += railCar.frontLength + railCar.backLength + couplerLength;
            }
            else
                length += railCar.frontLength + railCar.backLength + couplerLength;
        }
        return length > 0 ? length - couplerLength : 0;
    }
    public float GetConsistLenght()
    {
        if (consistLength == 0)
            RecalculateConsistLength();

        return consistLength;
    }
    public void Initialize(float sectionProgress, TrackSection trackSection, bool fullSpeed)
    {
        this.frontTrackSection = trackSection;
        this.sectionProgress = sectionProgress;

        if (fullSpeed)
        {
            controlls.SetFullThrottle();
            speed = maxSpeed;
        }

        UpdatePosition();
        
        //GetComponentInChildren<TeleportOnStart>().Teleport();
    }
    public List<FuelSource> GetFuelSourceList()
    {
        return fuelSourceList;
    }
    public TrackSection GetFrontTrackSection()
    {
        return frontTrackSection;
    }
    public TrackSection GetBackTrackSection()
    {
        TrackManager.active.GetTrackSectionFromProgress(sectionProgress - consistLength, frontTrackSection, out TrackSection newSection, out float newSectionProgress);
        return newSection;
    }
    public float GetSpeed()
    {
        return speed;
    }
    public float GetBrokenCarWeight()
    {
        float sum = 0f;
        foreach (RailCar railCar in consist)
        {
            if(railCar.IsBroken())
                sum += railCar.GetWeight();
        }
        return sum;
    }
    public float GetConsistWeight()
    {
        float sum = 0f;
        foreach (RailCar railCar in consist)
        {
            sum += railCar.GetWeight();
        }
        return sum;
    }

    public List<CargoInfo> GetConsistCargoInfo()
    {
        List<CargoInfo> cargoInfoList = new List<CargoInfo>();

        foreach (RailCar railCar in consist)
        {
            cargoInfoList.Add(railCar.GetCargoInfo());
        }

        return cargoInfoList;
    }
    public List<RailCar> GetConsist()
    {
        return consist;
    }
    public void SetConsist(List<RailCar> consist)
    {
        this.consist = consist;
        RecalculateConsistLength();
        UpdatePosition();
    }
    public RailCar AddRailCar(RailCarSO railCarInfo, int index)
    {
        RailCar railCarCopy = Instantiate(railCarInfo.prefab).GetComponent<RailCar>();
        List<RailCar> listFront = new List<RailCar>();
        List<RailCar> listBack = new List<RailCar>();

        for (int i = 0; i < consist.Count; i++)
        {
            if (i < index)
                listFront.Add(consist[i]);
            else
                listBack.Add(consist[i]);
        }

        consist = listFront;
        consist.Add(railCarCopy);
        consist.AddRange(listBack);
        
        //Sync to client
        railCarCopy.GetComponent<NetworkObject>().Spawn(true);
        
        RecalculateConsistLength();
        
        return railCarCopy;
    }

    public bool HasEnoughFuel(out string depletedFuelSourceName)
    {
        depletedFuelSourceName = "";
        foreach (FuelSource fuelSource in fuelSourceList)
        {
            if (fuelSource.targetHealth.GetHealth() <= 0)
            {
                depletedFuelSourceName = fuelSource.fuelName;
                return false;
            }
        }

        return true;
    }

    public void RemoveNonPlayerRailCars()
    {
        for (int i = 0; i < consist.Count; i++)
        {
            if (consist[i].isPlayerRailCar == false)
            {
                consist[i].DestroyRailCar();
                consist.RemoveAt(i);
                i--;
            }
        }
        
        RecalculateConsistLength();
        
        Debug.Log("Removed NonPlayer RailCars");
    }
    public void HeallAllRailCars()
    {
        for (int i = 0; i < consist.Count; i++)
        {
            consist[i].health.ResetHealth();
        }
    }
    public float GetClosestDistanceToPos(Vector3 pos)
    {
        int minDistIndex = 0;
        float minDist = Vector3.Distance(consist[0].transform.position, pos);

        for (int i = 1; i < consist.Count; i++)
        {
            float d = Vector3.Distance(consist[i].transform.position, pos);
            if (d < minDist)
            {
                minDistIndex = i;
                minDist = d;
            }
        }

        return minDist;
    }

    public List<Player> GetPlayersOnTrain()
    {
        List<Player> playersOnTrain = new List<Player>();
        for (int i = 0; i < Player.playerList.Count; i++)
        {
            if (Player.playerList[i].movement.IsOnRailCar(out RailCar railCar) && railCar.GetLinkedTrain() == this)
            {
                playersOnTrain.Add(Player.playerList[i]);
            }
        }

        return playersOnTrain;
    }
    public float GetPlayerDist()
    {
        return playerDistFromTrain;
    }
}
