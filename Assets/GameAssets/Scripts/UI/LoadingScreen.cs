using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen active;

    [Header("Fade")]
    [SerializeField]
    private Fade fade;
    [Header("Text Refrences")]
    [SerializeField]
    private TMP_Text loadingTitleText;
    [SerializeField]
    private TMP_Text loadingPrecentText;
    [SerializeField]
    private TMP_Text loadingDescriptionText;
    [Header("Loading Bar")]
    [SerializeField]
    private RectTransform loadingBarTransform;
    [Header("Speed params")]
    [SerializeField]
    private float dotUpdateInterval;
    [SerializeField]
    private float fadeTime;
    [SerializeField]
    private float progressBarSpeed;
    //run time
    private float dotsTime = 0f;
    private int dots = 0;
    private float progress;
    private float targetProgress;
    private string titleText;
    private string descriptionText;
    private void Awake()
    {
        active = this;
    }
    private void Start()
    {
        fade.SetAlpha(0f);
    }
    private void Update()
    {
        if (fade.GetAlpha() != 0f)
        {
            loadingDescriptionText.text = descriptionText;
            //dots
            dotsTime += Time.unscaledDeltaTime;
            if(dotsTime >= dotUpdateInterval)
            {
                dots++;
                if (dots > 3)
                    dots = 0;
                string str = titleText;
                for (int i = 0; i < dots; i++)
                {
                    str += ".";
                }
                loadingTitleText.text = str;
                dotsTime = 0;
            }

            //progress
            progress = Mathf.Lerp(progress, targetProgress, Time.unscaledDeltaTime * progressBarSpeed);
            loadingBarTransform.anchorMax = new Vector2(progress, 1);
            loadingPrecentText.text = Mathf.FloorToInt(progress * 100f) + "%";
        }
        if(fade.GetAlphaChangeSpeed() > 0)
        {
            Time.timeScale = 1f - Mathf.Clamp(fade.GetAlpha() * 3f, 0f, 1f);
        }
    }
    public void Enable(string titleText = "Loading")
    {
        this.titleText = titleText;
        loadingTitleText.text = titleText;
        dots = 0;
        progress = 0f;
        SetProgress(0f);
        fade.FadeTo(1f, fadeTime);
        Fade.gameFade.FadeTo(0.2f, fadeTime);
    }
    public void SetProgress(float time, string description = "Who knows whats the code is doing")
    {
        //Debug.Log("Time set to: " + time);
        targetProgress = time;
        descriptionText = description;
    }
    public void Disable()
    {
        SetProgress(1f, "Done!");
        fade.FadeTo(0f, fadeTime);
        Fade.gameFade.FadeTo(0f, fadeTime);
        Time.timeScale = 1f;
    }

}
