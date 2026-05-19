using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour
{
    public static Train playerTrain;

    [System.Serializable]
    public class FuelSource
    {
        public TrainStatType consumptionRate;
        public Health targetHealth;
        public string fuelName;
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
    [HideInInspector]
    public float sectionProgress;
    private float consistLength;

    private float playerDistFromTrain;
    private void Awake()
    {
        if (isPlayerTrain)
            playerTrain = this;

        RecalculateConsistLength();
    }

    private void Update()
    {
        //Debug.Log(deceleration);
        if(frontTrackSection == null)
        {
            Debug.LogWarning("No track section assigned!");
            return;
        }

        //Player dist
        if (PlayerMovement.active != null && GameStateManager.gameStarted)
        {
            playerDistFromTrain = GetClosestDistanceToPos(PlayerMovement.active.transform.position);
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
            if(railCar.derailed)
            {
                derailed = true;
                break;
            }
        }
        if(controlls.currentState == LocomotiveControls.State.derailed)
        {
            if (derailed == false)
                controlls.Unlock();
        }
        else if(controlls.currentState == LocomotiveControls.State.normal)
        {
            if (derailed)
                controlls.SetDerailled();
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

        //Fuel
        if(acceleration > 0f && (controlls == null || controlls.currentState == LocomotiveControls.State.normal))
        {
            foreach (FuelSource fuelSource in fuelSourceList)
            {
                fuelSource.targetHealth.TakeDamage(distanceTravelled * TrainUpgradeHandler.active.GetStatValue(fuelSource.consumptionRate));
                //Not having enough fuel handles LocomotiveControls.cs
            }
        }
        //Debri
        if(debriBoxCollider != null && derailed == false && speed > 0.4f)
        {
            Collider[] debriColliders = Util.PhysicsBoxColliderOverlap(debriBoxCollider, debriLayerMask);
            for (int i = 0; i < debriColliders.Length; i++)
            {
                if (debriColliders[i].TryGetComponent(out Item item) && item.IsPhysicsEnabled() == false)
                {
                    if (Random.Range(0f, 1f) >= 0.5f)
                        item.EnablePhysics(consist[0].transform.forward * (speed * 1.5f) + consist[0].transform.right * Random.Range(2f, 4f) + consist[0].transform.up * Random.Range(5f, 6f));
                    else
                        item.EnablePhysics(consist[0].transform.forward * (speed * 1.5f) + consist[0].transform.right * Random.Range(-4f, -2f) + consist[0].transform.up * Random.Range(5f, 6f));

                    if (Random.Range(0f, 1f) <= TrainUpgradeHandler.active.GetStatValue(debriDerailChance))
                    {
                        consist[0].Derail();

                        Override title = new Override("Title", OverrideType.Text, "WARNING");
                        Override message = new Override("Message", OverrideType.Text, "Train DERAILED! Due to object on track!");
                        Override subText = new Override("SubText", OverrideType.Text, "Locomotive");
                        MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Message", new List<Override>() { title, message, subText }));

                        break;
                    }
                }
            }
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
    public void Initialize(float sectionProgress, TrackSection trackSection)
    {
        this.frontTrackSection = trackSection;
        this.sectionProgress = sectionProgress;
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
    public RailCar AddRailCar(RailCarSO railCarInfo, int index)
    {
        RailCar railCarCopy = Instantiate(railCarInfo.prefab,transform).GetComponent<RailCar>();
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

        RecalculateConsistLength();

        return railCarCopy;
    }

    public bool HasEnoughFuel(out string depletedFuelSourceName)
    {
        depletedFuelSourceName = "";
        foreach (FuelSource fuelSource in fuelSourceList)
        {
            if (fuelSource.targetHealth.health <= 0)
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
                Destroy(consist[i].gameObject);
                consist.RemoveAt(i);
                i--;
            }
        }

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

    public float GetPlayerDist()
    {
        return playerDistFromTrain;
    }
}
