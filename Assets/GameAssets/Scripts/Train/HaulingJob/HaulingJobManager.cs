using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

public class HaulingJob
{
    public List<HaulingJobEntry> haulingJobEntryList;
    public float distance;
    public NodePath linkedTrainTrackPath;
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
}
public class HaulingJobEntry
{
    public CargoSO cargo;
    public RailCarSO railCar;

    public float weight;
    public float pay;
}
public class HaulingJobManager : MonoBehaviour
{
    public static HaulingJobManager active;

    public static List<HaulingJob> generatedHaulingJobList = new List<HaulingJob>();

    [Header("Cargo List")]
    [SerializeField]
    public List<CargoSO> haulingCargoInfoList = new List<CargoSO>();
    [Header("Railcar List")]
    [SerializeField]
    public List<RailCarSO> railCarInfoList = new List<RailCarSO>();
    [Header("Pay params")]
    public Vector2 payIncreaseFor100Meters;
    [Header("Icon rendering")]
    public LayerMask renderLayer;
    public bool renderIconsOnstart;


    private Camera renderCam;
    private RenderTexture renderTexture;
    private void Awake()
    {
        active = this;
        GenerateNewHaulingJobList(3);
    }
    private void Start()
    {
        if(renderIconsOnstart)
            GenerateIcons();
    }
    public void GenerateNewHaulingJobList(int count, List<NodePath> nodePathList = null)
    {
        List<HaulingJob> haulingJobList = new List<HaulingJob>();

        for (int i = 0; i < count; i++)
        {
            haulingJobList.Add(GenerateHaulingJob(i * 0.5f, i * 1f, 0.4f, 2 + i * 2, GameStateManager.currentLevel, nodePathList != null ? nodePathList[i] : null));
        }

        generatedHaulingJobList = haulingJobList;

    }
    public HaulingJob GenerateHaulingJob(float maxCargoDangerLevel, float targetDangerLevel, float mixedLevel, int maxCargoCount, int currentLevel, NodePath trackPath = null)
    {
        //Pick first cargo
        CargoSO firstCargo;
        int safety = 0;
        do
        {
            firstCargo = haulingCargoInfoList[Random.Range(0, haulingCargoInfoList.Count)];
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
            bool chooseMixed = Random.Range(0f, 1f) <= mixedLevel;
            CargoSO chosenCargo;
            safety = 0;
            do 
            {
                if (chooseMixed)
                {
                    chosenCargo = haulingCargoInfoList[Random.Range(0, haulingCargoInfoList.Count)];
                }
                else
                {
                    chosenCargo = cargoList[^1].fittingCargo[Random.Range(0, cargoList[^1].fittingCargo.Count)];
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
            entry.railCar = eligibleRailCarList[Random.Range(0, eligibleRailCarList.Count)];

            entry.weight = Random.Range(10, 50);
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

        if (trackPath == null)
            haulingJob.distance = Random.Range(1250, 2500);
        else
        {
            haulingJob.distance = trackPath.length;
            haulingJob.linkedTrainTrackPath = trackPath;
        }

        //Calculate pay
        float bonus = (Mathf.Floor(haulingJob.distance / 100f) * Random.Range(payIncreaseFor100Meters.x, payIncreaseFor100Meters.y)) / haulingJob.haulingJobEntryList.Count;
        foreach (HaulingJobEntry entry in haulingJob.haulingJobEntryList)
        {
            float cargo = Random.Range(entry.cargo.payRange.x, entry.cargo.payRange.y);
            float railCar = Random.Range(entry.railCar.payRange.x, entry.railCar.payRange.y);
            float pay = Mathf.Round((bonus + cargo + railCar) / 10f) * 10f;

            entry.pay = pay;
        }
        return haulingJob;
        
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
            railCarScript.ParentCargo(cargoCopy);
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
