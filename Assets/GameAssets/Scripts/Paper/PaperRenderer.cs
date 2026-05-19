using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public enum OverrideType
{
    Text,
    HaulingJobEntry,
    HaulReceipt,
    RawImageTexture,
    SellReceipt,
    UnlockList,
    StatisticList
}
public class Override
{
    public string targetName;
    public OverrideType overrideType;
    public string stringOverride;
    public HaulingJob haulingJobOverride;
    public List<CargoInfo> cargoListOverride;
    public Texture2D textureOverride;
    public List<Sell.SellEntry> sellReceiptOverride;
    public List<UnlockEntry> unlockListOverride;
    public Override(string targetName, OverrideType overrideType, string stringOverride = "")
    {
        this.targetName = targetName;
        this.overrideType = overrideType;
        this.stringOverride = stringOverride;
    }
}
public class PaperRenderer : MonoBehaviour
{
    public static PaperRenderer active;

    [Serializable]
    public class OverrideEntry
    {
        public string name;
        public Transform objectRefrence;
        public Transform objectRefrence2;
    }
    [Serializable]
    public class Paper
    {
        public string name;
        public GameObject paperObject;
        public Vector2Int renderResolution;
        public Vector2Int paperResolution;
        public List<OverrideEntry> overrideEntryList;   
        public OverrideEntry FindOverrideEntry(string name)
        {
            foreach (OverrideEntry entry in overrideEntryList)
            {
                if (entry.name.ToLower() == name.ToLower())
                {
                    return entry;
                }
            }

            Debug.LogWarning("Did not find override entry with name: " + name);
            return null;
        }
    }

    //Variables
    [SerializeField]
    private Camera renderCamera;
    [SerializeField]
    private RectTransform rawImagePrefab;
    [SerializeField]
    public List<Paper> paperList;

    private void Start()
    {
        active = this;
    }
    public Texture2D RenderPaper(string paperName,List<Override> overrideList)
    {
        Paper paper = GetPaper(paperName);
        if (paper == null)
        {
            Debug.LogWarning("No Paper");
            return null;
        }

        List<GameObject> destroyList = new List<GameObject>();
        //Handle overrides
        foreach (Override overrideInfo in overrideList)
        {
            OverrideEntry overrideEntry = paper.FindOverrideEntry(overrideInfo.targetName);
            if (overrideEntry != null)
            {
                if (overrideInfo.overrideType == OverrideType.Text)
                    overrideEntry.objectRefrence.GetComponent<TMP_Text>().text = overrideInfo.stringOverride;
                else if (overrideInfo.overrideType == OverrideType.RawImageTexture)
                    overrideEntry.objectRefrence.GetComponent<RawImage>().texture = overrideInfo.textureOverride;
                else if (overrideInfo.overrideType == OverrideType.HaulingJobEntry)
                {
                    float listHeight = 6.5f; //Hard coded is bad but whatever
                    float heigth = Mathf.Clamp(listHeight / overrideInfo.haulingJobOverride.haulingJobEntryList.Count, 0f, 1f);

                    float sum = 0f;
                    foreach (HaulingJobEntry entry in overrideInfo.haulingJobOverride.haulingJobEntryList)
                    {
                        Transform copy = Instantiate(overrideEntry.objectRefrence2);
                        copy.SetParent(overrideEntry.objectRefrence);
                        ((RectTransform)copy).localPosition = Vector3.zero;
                        ((RectTransform)copy).sizeDelta = new Vector2(((RectTransform)copy).sizeDelta.x, heigth);

                        copy.Find("Cargo").GetComponent<TMP_Text>().text = entry.cargo.cargoName;
                        copy.Find("Weight").GetComponent<TMP_Text>().text = entry.weight + "t";
                        copy.Find("Pay").GetComponent<TMP_Text>().text = entry.pay + "$";
                        sum += entry.pay;

                        if (entry.railCar.icon != null)
                            copy.Find("Icon").GetComponent<RawImage>().texture = entry.cargo.GetIcon(entry.railCar);
                    }

                    OverrideEntry infoOverride = paper.FindOverrideEntry("Info");
                    infoOverride.objectRefrence.GetComponent<TMP_Text>().text = "Dist.: " + Mathf.Round(overrideInfo.haulingJobOverride.distance / 10f) * 10f + "m, Sum: " + sum + "$";

                    for (int i = 0; i < overrideEntry.objectRefrence.childCount; i++)
                    {
                        destroyList.Add(overrideEntry.objectRefrence.GetChild(i).gameObject);
                    }
                }
                else if (overrideInfo.overrideType == OverrideType.HaulReceipt)
                {
                    float listHeight = 6.5f; //Hard coded is bad but whatever
                    float heigth = Mathf.Clamp(listHeight / overrideInfo.cargoListOverride.Count, 0f, 1f);

                    float sum = 0f;
                    foreach (CargoInfo cargoInfo in overrideInfo.cargoListOverride)
                    {
                        Transform copy = Instantiate(overrideEntry.objectRefrence2);
                        copy.SetParent(overrideEntry.objectRefrence);
                        ((RectTransform)copy).localPosition = Vector3.zero;
                        ((RectTransform)copy).sizeDelta = new Vector2(((RectTransform)copy).sizeDelta.x, heigth);

                        copy.Find("CargoHealth").GetComponent<TMP_Text>().text = cargoInfo.GetCargoHealthPrecentString();

                        copy.Find("RailCarHealth").GetComponent<TMP_Text>().text = cargoInfo.GetRailCarHealthPrecentString();

                        if (cargoInfo.GetValueSum() != 0)
                        {
                            copy.Find("Pay").GetComponent<TMP_Text>().text = cargoInfo.GetValueSum() + "$(<color=red>-" + cargoInfo.GetExpensesSum() + "$</color>)";
                            sum += cargoInfo.GetPaySum();
                        }
                        else
                            copy.Find("Pay").GetComponent<TMP_Text>().text = "-";

                        if (cargoInfo.cargoInfo != null)
                            copy.Find("Icon").GetComponent<RawImage>().texture = cargoInfo.cargoInfo.GetIcon(cargoInfo.railCarRefrence.railCarSO);
                        else
                            copy.Find("Icon").GetComponent<RawImage>().texture = cargoInfo.railCarRefrence.railCarSO.icon;


                    }
                    OverrideEntry sumOverride = paper.FindOverrideEntry("Sum");
                    sumOverride.objectRefrence.GetComponent<TMP_Text>().text = "Sum: " + sum + "$";

                    for (int i = 0; i < overrideEntry.objectRefrence.childCount; i++)
                    {
                        destroyList.Add(overrideEntry.objectRefrence.GetChild(i).gameObject);
                    }
                }
                else if (overrideInfo.overrideType == OverrideType.SellReceipt)
                {
                    float listHeight = 6.5f; //Hard coded is bad but whatever
                    float heigth = Mathf.Clamp(listHeight / overrideInfo.sellReceiptOverride.Count, 0f, 1f);

                    foreach (Sell.SellEntry entry in overrideInfo.sellReceiptOverride)
                    {
                        Transform copy = Instantiate(overrideEntry.objectRefrence2);
                        copy.SetParent(overrideEntry.objectRefrence);
                        ((RectTransform)copy).localPosition = Vector3.zero;
                        ((RectTransform)copy).sizeDelta = new Vector2(((RectTransform)copy).sizeDelta.x, heigth);

                        copy.Find("Item").GetComponent<TMP_Text>().text = "(" + entry.count + "x" + entry.GetSingleItemValue() +"$) " + entry.itemSO.GetName();
                        copy.Find("Pay").GetComponent<TMP_Text>().text = entry.GetAllItemValue() + "$";
                        copy.Find("Icon").GetComponent<RawImage>().texture = entry.itemSO.icon;
                        ((RectTransform)copy.Find("Icon").transform).sizeDelta = Vector2.one * heigth;
                    }

                    for (int i = 0; i < overrideEntry.objectRefrence.childCount; i++)
                    {
                        destroyList.Add(overrideEntry.objectRefrence.GetChild(i).gameObject);
                    }
                }
                else if (overrideInfo.overrideType == OverrideType.UnlockList)
                {
                    float listHeight = 6.5f; //Hard coded is bad but whatever
                    float heigth = Mathf.Clamp(listHeight / overrideInfo.unlockListOverride.Count, 0f, 1f);

                    foreach (UnlockEntry entry in overrideInfo.unlockListOverride)
                    {
                        Transform copy = Instantiate(overrideEntry.objectRefrence2);
                        copy.SetParent(overrideEntry.objectRefrence);
                        ((RectTransform)copy).localPosition = Vector3.zero;
                        ((RectTransform)copy).sizeDelta = new Vector2(((RectTransform)copy).sizeDelta.x, heigth);

                        copy.Find("UnlockedText").gameObject.SetActive(entry.isUnlocked);

                        void AddIcon(Transform parent,Texture2D icon)
                        {
                            RectTransform copyImage = Instantiate(rawImagePrefab,parent);
                            copyImage.sizeDelta = Vector2.one * heigth;
                            RawImage rawImage = copyImage.GetComponent<RawImage>();
                            rawImage.texture = icon;
                            rawImage.color = new Color(0.2f, 0.2f, 0.2f);
                        }

                        Transform parent = copy.Find("NeededItemList");
                        foreach (ShopItemSO shopItem in entry.neededShopItemList)
                        {
                            AddIcon(parent, shopItem.icon);
                        }
                        AddIcon(copy.Find("UnlockItemList"), entry.targetShopItem.icon);
                    }

                    for (int i = 0; i < overrideEntry.objectRefrence.childCount; i++)
                    {
                        destroyList.Add(overrideEntry.objectRefrence.GetChild(i).gameObject);
                    }
                }
                else if (overrideInfo.overrideType == OverrideType.StatisticList)
                {
                    float listHeight = 6.5f; //Hard coded is bad but whatever
                    float heigth = Mathf.Clamp(listHeight / GameStateManager.statisticEntryList.Count, 0f, 1f);

                    foreach (GameStateManager.StatisticEntry entry in GameStateManager.statisticEntryList)
                    {
                        Transform copy = Instantiate(overrideEntry.objectRefrence2);
                        copy.SetParent(overrideEntry.objectRefrence);
                        ((RectTransform)copy).localPosition = Vector3.zero;
                        ((RectTransform)copy).sizeDelta = new Vector2(((RectTransform)copy).sizeDelta.x, heigth);

                        copy.Find("Name").GetComponent<TMP_Text>().text = entry.name;

                        float value = Mathf.Round(entry.value * 100f) / 100f;
                        string str = value.ToString();
                        if (entry.type == GameStateManager.StatisticType.Money)
                            str += "$";
                        else if (entry.type == GameStateManager.StatisticType.DistanceMeters)
                            str += " m";
                        else if (entry.type == GameStateManager.StatisticType.DistanceKilometers)
                            str += " km";
                        copy.Find("Value").GetComponent<TMP_Text>().text = str;

                    }

                    for (int i = 0; i < overrideEntry.objectRefrence.childCount; i++)
                    {
                        destroyList.Add(overrideEntry.objectRefrence.GetChild(i).gameObject);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Override skipped: " + overrideInfo.targetName);
            }
        }

        //Render
        paper.paperObject.SetActive(true);

        RenderTexture renderTexture = new RenderTexture(paper.renderResolution.x, paper.renderResolution.y,16);
        //renderTexture.Create();

        renderCamera.targetTexture = renderTexture;
        renderCamera.forceIntoRenderTexture = true;
        RenderTexture.active = renderTexture;
        renderCamera.Render();

        Texture2D texture = new Texture2D(paper.paperResolution.x, paper.paperResolution.y);
        texture.ReadPixels(new Rect(0, 0, paper.renderResolution.x, paper.renderResolution.y), 0, 0);
        texture.Apply();

        paper.paperObject.SetActive(false);

        for (int i = 0; i < destroyList.Count; i++)
        {
            destroyList[i].SetActive(false);
            Destroy(destroyList[i]);
        }

        return texture;
    }
    private Paper GetPaper(string paperName)
    {
        foreach (Paper paper in paperList)
        {
            if(paper.name.ToLower() == paperName.ToLower())
            {
                return paper;
            }
        }

        Debug.LogWarning("Did not find paper with name: " + paperName);
        return null;
    }
}
