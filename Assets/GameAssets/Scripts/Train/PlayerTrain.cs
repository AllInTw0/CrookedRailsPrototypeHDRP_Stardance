using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerTrain : Train
{
    public static PlayerTrain active;
    
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
    [Header("Fuel")]
    [SerializeField]
    private List<FuelSource> fuelSourceList;
    [Header("Speed")]
    [SerializeField]
    public LocomotiveControls controlls;
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
    private float playerDistFromTrain;

    public float GetPlayerDist()
    {
        return playerDistFromTrain;
    }
    
    private bool consumeFuel;
    private void Awake()
    {
        activeTrainList.Add(this);
        active = this;
    }
    public override void Update()
    {
        base.Update();
        
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
        bool derailed = IsDerailed();
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
        
        GameStateManager.distanceTravelled += distanceTravelled;
        
        
        //Fuel
        //Debug.Log("Consume fuel: " + consumeFuel);
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
    }

    public void SetFuelConsumption(bool value)
    {
        consumeFuel = value;
    }
    
    public List<FuelSource> GetFuelSourceList()
    {
        return fuelSourceList;
    }

    public override void Initialize(float sectionProgress, TrackSection trackSection, bool fullSpeed)
    {
        base.Initialize(sectionProgress, trackSection, fullSpeed);
        if (fullSpeed)
        {
            controlls.SetFullThrottle();
            speed = maxSpeed;
        }
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
}
