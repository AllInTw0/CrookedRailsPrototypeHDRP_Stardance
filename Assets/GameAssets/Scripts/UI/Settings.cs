using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public static Settings active;
    [HideInInspector]
    public UnityEvent onClose;

    //Values
    public static float fov;

    public static float sensitivity;
    public static bool invertedY;
    public static bool invertedX;

    [Header("Fade")]
    public Fade settingsFade;
    [Header("Prefabs")]
    [SerializeField]
    private RectTransform _sectionButtonPrefab; private static RectTransform sectionButtonPrefab;
    [SerializeField]
    private RectTransform _sectionPrefab; private static RectTransform sectionPrefab;
    [Header("References")]
    [SerializeField]
    private RectTransform _sectionButtonParent; private static RectTransform sectionButtonParent;
    [SerializeField]
    private RectTransform _sectionParent; private static RectTransform sectionParent;
    [SerializeField]
    private Button backButton;
    [Header("Setting Prefabs")]
    [SerializeField]
    private RectTransform _checkboxPrefab; private static RectTransform checkboxPrefab;
    [SerializeField]
    private RectTransform _sliderPrefab; private static RectTransform sliderPrefab;
    [SerializeField]
    private RectTransform _dropdownPrefab; private static RectTransform dropdownPrefab;
    [SerializeField]
    private RectTransform _titlePrefab; private static RectTransform titlePrefab;
    [Header("Sound")]
    [SerializeField]
    private string _clickSoundString = "Click"; private static string clickSoundString;

    private static List<RectTransform> sectionList = new List<RectTransform>();
    public class Section
    {
        public RectTransform sectionButton;
        public RectTransform section;

        public RectTransform sectionSettingListParent;

        VerticalLayoutGroup verticalLayoutGroup;
        float height;
        public Section(string name)
        {
            sectionButton = Instantiate(sectionButtonPrefab, sectionButtonParent);
            sectionButton.GetComponentInChildren<TMP_Text>().text = name;

            section = Instantiate(sectionPrefab, sectionParent);
            sectionList.Add(section);

            verticalLayoutGroup = section.GetComponentInChildren<VerticalLayoutGroup>();
            height -= verticalLayoutGroup.spacing;

            sectionSettingListParent = (RectTransform)section.Find("Viewport").Find("Content");

            sectionButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                SoundManager.active.Play(clickSoundString);
                OpenSection();
            });
        }
        public void OpenSection()
        {
            foreach (RectTransform section in sectionList)
            {
                section.gameObject.SetActive(false);
            }
            section.gameObject.SetActive(true);
        }
        public Setting InitializeSetting(RectTransform prefab, string name)
        {
            RectTransform copy = Instantiate(prefab, sectionSettingListParent);
            copy.Find("Name").GetComponentInChildren<TMP_Text>().text = name;
            height += copy.sizeDelta.y + verticalLayoutGroup.spacing;
            ((RectTransform)verticalLayoutGroup.transform).sizeDelta = new Vector2(((RectTransform)verticalLayoutGroup.transform).sizeDelta.x, height);
            return copy.GetComponentInChildren<Setting>();
        }
        public Setting AddCheckbox(string name, bool defaultValue)
        {
            Setting setting = InitializeSetting(checkboxPrefab, name);

            ((CheckboxSetting)setting).SetTicked(defaultValue);

            return setting;
        }
        public Setting AddSlider(string name, float minValue, float maxValue, float defaultValue)
        {
            Setting setting = InitializeSetting(sliderPrefab, name);

            ((SliderSetting)setting).SetValues(minValue, maxValue, defaultValue);

            return setting;
        }
        public Setting AddDropdown(string name, List<string> optionList, int index = 0)
        {
            Setting setting = InitializeSetting(dropdownPrefab, name);

            ((DropdownSetting)setting).SetValues(optionList, index);

            return setting;
        }
        public void AddTitle(string title)
        {
            InitializeSetting(titlePrefab, title);
        }
    }
    private void Awake()
    {
        active = this;
        onClose = new UnityEvent();

        sectionButtonPrefab = _sectionButtonPrefab;
        sectionPrefab = _sectionPrefab;
        sectionButtonParent = _sectionButtonParent;
        sectionParent = _sectionParent;
        checkboxPrefab = _checkboxPrefab;
        sliderPrefab = _sliderPrefab;
        dropdownPrefab = _dropdownPrefab;
        titlePrefab = _titlePrefab;
        clickSoundString = _clickSoundString;

        sectionList = new List<RectTransform>();
    }
    private void Start()
    {
        Section graphicsSection = new Section("Graphics");

        bool fullscreen = Screen.fullScreen;
        Resolution currentResolution = Screen.currentResolution;
        int currentResolutionIndex = 0;
        Resolution[] resolutionArray = Screen.resolutions;
        string[] strResolutionList = new string[resolutionArray.Length];
        for (int i = 0; i < resolutionArray.Length; i++)
        {
            strResolutionList[i] = resolutionArray[i].width + "x" + resolutionArray[i].height; // + " : " + Mathf.Round((float)resolutionArray[i].refreshRateRatio.value) +"Hz";
            if (resolutionArray[i].width == currentResolution.width && resolutionArray[i].height == currentResolution.height)
                currentResolutionIndex = i;
        }
        graphicsSection.AddTitle("Resolution");
        graphicsSection.AddCheckbox("Fullscreen", fullscreen).onValueChanged.AddListener((float newValue) =>
        {
            fullscreen = newValue == 1f;
            Screen.SetResolution(currentResolution.width, currentResolution.height, fullscreen);
        });
        graphicsSection.AddDropdown("Resolution", new List<string>(strResolutionList), currentResolutionIndex).onValueChanged.AddListener((float newValue) =>
        {
            currentResolutionIndex = Mathf.RoundToInt(newValue);
            currentResolution = resolutionArray[currentResolutionIndex];
            Screen.SetResolution(currentResolution.width, currentResolution.height, fullscreen);
        });

        graphicsSection.AddTitle("View");

        fov = PlayerPrefs.GetFloat("fov", 60f);
        graphicsSection.AddSlider("Camera's FOV", 10f, 160f, fov).onValueChanged.AddListener((float newValue) =>
        {
            fov = newValue;
            PlayerPrefs.SetFloat("fov", newValue);
        });



        Section controlsSection = new Section("Controls");

        controlsSection.AddTitle("Mouse");
        sensitivity = PlayerPrefs.GetFloat("sensitivity", 0.2f);
        controlsSection.AddSlider("Sensitivity", 0f, 10f, sensitivity).onValueChanged.AddListener((float newValue) =>
        {
            sensitivity = newValue;
            PlayerPrefs.SetFloat("sensitivity", sensitivity);
        });
        invertedY = PlayerPrefs.GetInt("invertedY", 0) == 1;
        controlsSection.AddCheckbox("Inverted Y", invertedY).onValueChanged.AddListener((float newValue) =>
        {
            invertedY = newValue == 1;
            PlayerPrefs.SetInt("invertedY", Mathf.RoundToInt(newValue));
        });
        invertedX = PlayerPrefs.GetInt("invertedX", 0) == 1;
        controlsSection.AddCheckbox("Inverted X", invertedY).onValueChanged.AddListener((float newValue) =>
        {
            invertedX = newValue == 1;
            PlayerPrefs.SetInt("invertedX", Mathf.RoundToInt(newValue));
        });


        Section soundsSection = new Section("Sounds");
        void SoundSlider(string str, string visualStr)
        {
            float value = PlayerPrefs.GetFloat(str, 1f);
            SetVolume(value);
            soundsSection.AddSlider(visualStr, 0f, 2f, value).onValueChanged.AddListener((float newValue) =>
            {
                SetVolume(newValue);
            });
            void SetVolume(float value)
            {
                if (value <= 0f)
                    value = 0.00001f;
                float db = Mathf.Log10(value) * 20f;
                SoundManager.SetMixerParam(str, db);
                PlayerPrefs.SetFloat(str, value);
            }
        }
        soundsSection.AddTitle("Main");
        SoundSlider("MasterVolume", "Master Volume");
        SoundSlider("MusicVolume", "Music Volume");
        SoundSlider("EffectVolume", "SFX Volume");
        soundsSection.AddTitle("Hit Sound Effects");
        SoundSlider("HitSoundVolume", "HitSound Volume");
        //Other
        graphicsSection.OpenSection();

        backButton.onClick.AddListener(() =>
        {
            if (settingsFade.GetAlpha() == 1f)
            {
                PlayClick();
                Close();
            }
        });

        settingsFade.AddGraphics(settingsFade.transform.GetComponentsInChildren<Graphic>());

        StartCoroutine(CloseCoroutine(0f));


        ////Create a settings "section" (the buttons at the top to choose the category)
        //Section graphicsSection = new Section("Graphics");
        ////Create the checkbox and listen for onValueChanged
        ////.AddCheckbox(name, is ticked by default)
        //graphicsSection.AddCheckbox("Fullscreen", true).onValueChanged.AddListener((float newValue) =>
        //{
        //    //Plays sound effect
        //    PlayClick();
        //    //Set fullscreen bool. if newValue is 1, then the box is ticked, 0 if not
        //    Screen.fullScreen = newValue == 1f;
        //});

        ////
        ////OTHER EXAMPLES
        ////

        ////Creates a title
        //graphicsSection.AddTitle("Resolution");

        ////Dropdown showed in the video
        ////.AddDropdown(name, list of options, default option index)
        //graphicsSection.AddDropdown("Resolution", new List<string>(strResolutionList), currentResolutionIndex).onValueChanged.AddListener((float newValue) =>
        //{
        //    PlayClick();
        //    currentResolutionIndex = Mathf.RoundToInt(newValue);
        //    currentResolution = resolutionArray[currentResolutionIndex];
        //    Screen.SetResolution(currentResolution.width, currentResolution.height, fullscreen);
        //});

        ////Slider showed in the video
        ////.AddSlider(name, min value, max value, default value)
        //controlsSection.AddSlider("Sensitivity", 0f, 50f, 5f).onValueChanged.AddListener((float newValue) =>
        //{
        //    Debug.Log("Sensitivity: " + newValue);
        //});
    }

    public void Open()
    {
        settingsFade.gameObject.SetActive(true);
        settingsFade.FadeTo(1f, 0.5f);
    }
    public void Close()
    {
        onClose.Invoke();
        StartCoroutine(CloseCoroutine(0.5f));
    }
    public IEnumerator CloseCoroutine(float closeTime)
    {
        settingsFade.FadeTo(0f, closeTime);
        if (closeTime == 0f)
            yield return new WaitForEndOfFrame();
        else
            yield return new WaitForSecondsRealtime(closeTime);
        settingsFade.gameObject.SetActive(false);
    }
    public static void PlayClick()
    {
        SoundManager.active.Play(clickSoundString);
    }
}
