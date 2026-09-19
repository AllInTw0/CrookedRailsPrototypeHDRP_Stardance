using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
public class Train : NetworkBehaviour
{
    public static List<Train> activeTrainList = new List<Train>();

    //Variables
    [SerializeField]
    private bool isPlayerTrain;
    [Header("Consist")]
    [SerializeField]
    public List<RailCar> consist = new List<RailCar>();
    [SerializeField] 
    public float couplerLength;
    [Header("Speed")]
    [SerializeField]
    private float drag = 0.25f;
    [SerializeField]
    private float weightAccelerationPenalty = 0.01f;
    [SerializeField]
    private float brokenDownWeightSpeedPenalty = 0.01f;
    [SerializeField]
    private float minSpeed = 1f;

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
    [HideInInspector]
    public float distanceTravelled;
    public virtual void Update()
    {
        if(IsInitialized() == false)
        {
            Debug.LogWarning("No track section assigned!");
            if (IsClient)
            {
                SyncTrainClient(trainNetworkData.Value, trainNetworkData.Value);
            }
            return;
        }
        
        bool derailed = IsDerailed();
        
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

        distanceTravelled = speed * Time.deltaTime;
        sectionProgress += distanceTravelled;
        
        UpdatePosition(distanceTravelled);
        
        
        
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

    public bool IsDerailed()
    {
        foreach (RailCar railCar in consist)
        {
            if(railCar.IsDerailed())
            {
                return true;
            }
        }
        return false;
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
    public virtual void Initialize(float sectionProgress, TrackSection trackSection, bool fullSpeed = false)
    {
        this.frontTrackSection = trackSection;
        this.sectionProgress = sectionProgress;
        UpdatePosition();
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
}
