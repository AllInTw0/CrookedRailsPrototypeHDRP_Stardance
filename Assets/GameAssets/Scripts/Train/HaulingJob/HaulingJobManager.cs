using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

public class HaulingJob
{
    public List<HaulingJobEntry> haulingJobEntryList;
    public float distance;
    public float GetConsistLength()
    {
        float sum = 0f;
        for (int i = 0; i < haulingJobEntryList.Count; i++)
        {
            RailCar prefabScript = haulingJobEntryList[i].railCar.prefab.GetComponent<RailCar>();
            sum += prefabScript.frontLength + prefabScript.backLength;
            if (i != haulingJobEntryList.Count - 1)
                sum += Train.playerTrain.couplerLength;
        }
        return sum;
    }

    public NodePath GetLinkedTrack()
    {
        for (int i = 0; i < HaulingJobManager.generatedHaulingJobList.Count; i++)
        {
            if (HaulingJobManager.generatedHaulingJobList[i] == this)
                return HaulingJobManager.currentNodePathList[i];
        }
        Debug.LogError("Did not find linked track");
        return null;
    }
}
public class HaulingJobEntry
{
    public CargoSO cargo;
    public RailCarSO railCar;

    public float weight;
    public float pay;
}
public class HaulingJobManager : NetworkBehaviour
{
    public static HaulingJobManager active;

    public static List<HaulingJob> generatedHaulingJobList = new List<HaulingJob>();
    public static int generatedHaulingJobLevel = -1;
    public static List<NodePath> currentNodePathList = new List<NodePath>();
    
    [Header("Cargo List")]
    [SerializeField]
    public List<CargoSO> haulingCargoInfoList = new List<CargoSO>();

    public static int GetCargoSOID(CargoSO cargoSO)
    {
        for (int i = 0; i < active.haulingCargoInfoList.Count; i++)
        {
            if (active.haulingCargoInfoList[i] == cargoSO) return i;
        }
        return -1;
    }
    public static CargoSO GetCargoSOByID(int ID)
    {
        return active.haulingCargoInfoList[ID];
    }
    [Header("Railcar List")]
    [SerializeField]
    public List<RailCarSO> railCarInfoList = new List<RailCarSO>();
    [Header("Pay params")]
    public Vector2Int payIncreaseFor100Meters;
    [Header("Icon rendering")]
    public LayerMask renderLayer;
    public bool renderIconsOnstart;


    private Camera renderCam;
    private RenderTexture renderTexture;
    private void Awake()
    {
        active = this;
    }
    private void Start()
    {
        if(renderIconsOnstart)
            GenerateIcons();
    }
    public void GenerateNewHaulingJobList(int count, List<NodePath> nodePathList = null)
    {
        //Server
        if (nodePathList != null)
        {
            bool sorted = false;
            while (sorted == false)
            {
                sorted = true;
                for (int i = 0; i < nodePathList.Count - 1; i++)
                {
                    if (nodePathList[i].length > nodePathList[i + 1].length)
                    {
                        sorted = false;
                        (nodePathList[i], nodePathList[i + 1]) = (nodePathList[i + 1], nodePathList[i]);
                    }
                }
            }
        }

        currentNodePathList = nodePathList;
        for (int i = 0; i < count; i++)
        {
            int seed = Random.Range(0, 99999999);
            GenerateHaulingJobRpc(seed, i * 0.5f, i * 1f, 0.4f, 2 + i * 2, GameStateManager.currentLevel.Value, nodePathList != null ? nodePathList[i].length : -1);
        }
    }

    private void AddHaulingJobToList(HaulingJob haulingJob, int level)
    {
        //Server & Client
        
        if (generatedHaulingJobLevel != level)
        {
            generatedHaulingJobList = new List<HaulingJob>();
            generatedHaulingJobLevel = level;
        }
        
        generatedHaulingJobList.Add(haulingJob);

        bool sorted = false;
        while (sorted == false)
        {
            sorted = true;
            for (int i = 0; i < generatedHaulingJobList.Count-1; i++)
            {
                if (generatedHaulingJobList[i].distance > generatedHaulingJobList[i + 1].distance)
                {
                    sorted = false;
                    (generatedHaulingJobList[i], generatedHaulingJobList[i + 1]) = (generatedHaulingJobList[i + 1], generatedHaulingJobList[i]);
                }
            }
        }
    }
    [Rpc(SendTo.Everyone)]
    public void GenerateHaulingJobRpc(int seed, float maxCargoDangerLevel, float targetDangerLevel, float mixedLevel, int maxCargoCount, int currentLevel, float trackLength = -1f)
    {
        System.Random rng = new System.Random(seed);
        
        //Pick first cargo
        CargoSO firstCargo;
        int safety = 0;
        do
        {
            firstCargo = haulingCargoInfoList[rng.Next(0, haulingCargoInfoList.Count-1)];
            safety++;
        }
        while (firstCargo.dangerLevel > maxCargoDangerLevel && GetEligibleRailCars(firstCargo,currentLevel).Count == 0 && safety < 20);

        if (safety >= 10)
            Debug.LogWarning("Safety : " + safety + "!");

        //Pick rest of cargo
        float dangerSum = firstCargo.dangerLevel;
        List<CargoSO> cargoList = new List<CargoSO>() { firstCargo };
        while (dangerSum <= targetDangerLevel && cargoList.Count < maxCargoCount)
        {
            bool chooseMixed = rng.Next(0, 1000) <= mixedLevel * 1000f;
            CargoSO chosenCargo;
            safety = 0;
            do 
            {
                if (chooseMixed)
                {
                    chosenCargo = haulingCargoInfoList[rng.Next(0, haulingCargoInfoList.Count-1)];
                }
                else
                {
                    chosenCargo = cargoList[^1].fittingCargo[rng.Next(0, cargoList[^1].fittingCargo.Count-1)];
                }
                safety++;
            } while (GetEligibleRailCars(chosenCargo, currentLevel).Count == 0 && safety < 20);
            if (safety >= 10)
                Debug.LogWarning("Safety : " + safety + "!");

            dangerSum += chosenCargo.dangerLevel;
            cargoList.Add(chosenCargo);

            mixedLevel *= mixedLevel;
        }

        //Setup Haulling Entries
        List<HaulingJobEntry> haulingJobEntryList = new List<HaulingJobEntry>();
        foreach (CargoSO cargo in cargoList)
        {
            HaulingJobEntry entry = new HaulingJobEntry();
            entry.cargo = cargo;

            List<RailCarSO> eligibleRailCarList = GetEligibleRailCars(cargo, currentLevel);
            entry.railCar = eligibleRailCarList[rng.Next(0, eligibleRailCarList.Count-1)];

            entry.weight = rng.Next(10, 50);
            entry.pay = 0f;

            haulingJobEntryList.Add(entry);
        }

        string debugString = "Hauling Job: ";
        foreach (HaulingJobEntry haulingJobEntry in haulingJobEntryList)
        {
            debugString += haulingJobEntry.cargo.GetName() + ":" + haulingJobEntry.railCar.GetName() + ", ";
        }
        Debug.Log(debugString);

        HaulingJob haulingJob = new HaulingJob();
        haulingJob.haulingJobEntryList = haulingJobEntryList;

        if (trackLength == -1f)
            haulingJob.distance = rng.Next(1250, 2500);
        else
        {
            haulingJob.distance = trackLength;
        }

        //Calculate pay
        float bonus = (Mathf.Floor(haulingJob.distance / 100f) * rng.Next(payIncreaseFor100Meters.x, payIncreaseFor100Meters.y)) / haulingJob.haulingJobEntryList.Count;
        foreach (HaulingJobEntry entry in haulingJob.haulingJobEntryList)
        {
            float cargo = rng.Next(entry.cargo.payRange.x, entry.cargo.payRange.y);
            float railCar = rng.Next(entry.railCar.payRange.x, entry.railCar.payRange.y);
            float pay = Mathf.Round((bonus + cargo + railCar) / 10f) * 10f;

            entry.pay = pay;
        }
        
        AddHaulingJobToList(haulingJob, currentLevel);
    }

    public List<RailCarSO> GetEligibleRailCars(CargoSO cargo, int level)
    {
        List<RailCarSO> railCarList = new List<RailCarSO>();
        foreach (RailCarSO railCar in cargo.fittingRailCars)
        {
            if(level >= railCar.minLevel && (level <= railCar.maxLevel || railCar.maxLevel == 0 || railCar.maxLevel == -1))
            {
                railCarList.Add(railCar);
            }
        }
        return railCarList;
    }





    public void GenerateIcons()
    {
        renderCam = Util.CreateCamera(renderLayer, CameraClearFlags.Color, true);
        renderCam.nearClipPlane = 0.1f;
        renderCam.farClipPlane = 10f;

        //Disable Post Processing
        HDAdditionalCameraData data = renderCam.gameObject.AddComponent<HDAdditionalCameraData>();
        data.customRenderingSettings = true;
        data.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Postprocess] = true;
        data.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
        data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;

        //Render texture
        renderTexture = new RenderTexture(256, 256, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        renderTexture.Create();

        renderCam.targetTexture = renderTexture;
        renderCam.forceIntoRenderTexture = true;

        foreach (CargoSO cargoInfo in haulingCargoInfoList)
        {
            GameObject cargoCopy = null;
            Debug.Log("1");
            if (cargoInfo.cargoPrefab)
            {
                cargoCopy = Instantiate(cargoInfo.cargoPrefab);
                Util.ChangeObjectsLayer(cargoCopy, renderLayer);
            }

            cargoInfo.iconList = new List<Texture2D>();

            foreach (RailCarSO railCarInfo in cargoInfo.fittingRailCars)
            {
                Texture2D texture = RenderIcon(railCarInfo, cargoCopy);
#if UNITY_EDITOR
                AssetDatabase.CreateAsset(texture, "Assets/GameAssets/SO/Icons/Cargo/" + railCarInfo.GetName() + "_" + cargoInfo.GetName() + "Icon.asset");
#endif
                cargoInfo.iconList.Add(texture);
            }
            DestroyImmediate(cargoCopy);
        }
        foreach (RailCarSO railCarInfo in railCarInfoList)
        {
            Texture2D texture = RenderIcon(railCarInfo);
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(texture, "Assets/GameAssets/SO/Icons/RailCar/" + railCarInfo.GetName() + "Icon.asset");
#endif
            railCarInfo.icon = texture;
        }
        DestroyImmediate(renderCam.gameObject);
    }

    private Texture2D RenderIcon(RailCarSO railCarInfo, GameObject cargoCopy = null)
    {
        Debug.Log("2");
        GameObject railCarCopy = Instantiate(railCarInfo.prefab, Vector3.zero, Quaternion.identity);
        Util.ChangeObjectsLayer(railCarCopy, renderLayer);
        railCarCopy.transform.position = Vector3.zero;

        if (cargoCopy)
        {
            RailCar railCarScript = railCarCopy.GetComponent<RailCar>();
            railCarScript.SetCargoPos(cargoCopy);
        }

        Bounds bounds = new Bounds();
        foreach (Renderer renderer in railCarCopy.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(renderer.bounds.min);
            bounds.Encapsulate(renderer.bounds.max);
        }

        //Position Camera
        renderCam.transform.position = bounds.center + new Vector3(bounds.extents.x + 0.2f, 0, 0);
        renderCam.transform.LookAt(bounds.center);
        renderCam.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.z);

        Texture2D texture = new Texture2D(256, 256);
        RenderTexture.active = renderTexture;
        renderCam.Render();
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        if (cargoCopy)
            cargoCopy.transform.SetParent(null);
        DestroyImmediate(railCarCopy);
        return texture;
    }
}
