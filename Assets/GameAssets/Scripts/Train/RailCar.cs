using System.Collections.Generic;
using TMPro;
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
                return Mathf.Round((cargoHealth.health / cargoHealth.maxHealth) * 100f) + "%";
            else
                return Mathf.Round((railCarHealth.health / railCarHealth.maxHealth) * 100f) + "%*";
        else
            return "-";
    }
    public string GetRailCarHealthPrecentString()
    {
        return Mathf.Round((railCarHealth.health / railCarHealth.maxHealth) * 100f) + "%";
    }
    public float GetExpensesSum()
    {
        if (cargoHealth != null)
        {
            return GetValueSum() * (2f - (cargoHealth.health / cargoHealth.maxHealth) - (railCarHealth.health / railCarHealth.maxHealth));
        }
        else
        {
            return GetValueSum() * (2f - 2f * (railCarHealth.health / railCarHealth.maxHealth));
        }
    }
    public float GetPaySum()
    {
        return GetValueSum() - GetExpensesSum();
    }
}
public class RailCar : MonoBehaviour
{
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

    private void Awake()
    {
        currentCargo = new CargoInfo();
        currentCargo.railCarRefrence = this;
        currentCargo.railCarHealth = health;
    }
    private void Update()
    {
        derailTime += derailed ? Time.deltaTime : -Time.deltaTime;
        derailTime = Mathf.Clamp01(derailTime);

        UpdateSway();
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
    public void SetCargo(CargoSO cargoSO, float cargoValue, float railCarValue)
    {
        CargoInfo cargoInfo = new CargoInfo();
        cargoInfo.cargoInfo = cargoSO;

        if (cargoSO.cargoPrefab != null)
        {
            cargoInfo.cargoObject = Instantiate(cargoSO.cargoPrefab);
            ParentCargo(cargoInfo.cargoObject);
        }

        cargoInfo.cargoValue = cargoValue;
        if(cargoInfo.cargoObject)
            cargoInfo.cargoHealth = cargoInfo.cargoObject.GetComponent<Health>();

        cargoInfo.railCarValue = railCarValue;
        cargoInfo.railCarHealth = health;

        cargoInfo.railCarRefrence = this;

        currentCargo = cargoInfo;
    }
    public void ParentCargo(GameObject cargoObject)
    {
        cargoObject.transform.SetParent(cargoOrigin);
        cargoObject.transform.localPosition = Vector3.zero;
        cargoObject.transform.localRotation = Quaternion.identity;
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
}
