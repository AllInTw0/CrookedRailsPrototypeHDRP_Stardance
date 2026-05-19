using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    public static Fade screenFade;
    public static Fade gameFade;
    public static Fade hitFade;

    [Header("Params")]
    [SerializeField]
    private List<Graphic> fadeGraphicList;
    [SerializeField]
    private bool addChildGrpahics;
    [Header("Global Access")]
    [SerializeField]
    private bool isScreenFade;
    [SerializeField]
    private bool isGameFade;
    [SerializeField]
    private bool isHitFade;

    bool fadeIn;
    private float speed;
    private float targetAlpha = 1f;
    private float value = 1f;
    private float lastValue = -1f;
    private void Awake()
    {
        if (isScreenFade) 
        { 
            screenFade = this;
            SetAlpha(1f);
            FadeTo(0f, 2f, 0.5f);
        }
        if (isGameFade)
        {
            gameFade = this;
            SetAlpha(0f);
        }
        if (isHitFade)
        {
            hitFade = this;
            SetAlpha(0f);
        }
        if (addChildGrpahics)
        {
            AddGraphics(transform.GetComponentsInChildren<Graphic>());
        }
    }

    private void Update()
    {
        value += speed * Time.unscaledDeltaTime;
        if (speed > 0)
            value = Mathf.Clamp(value, 0f, targetAlpha);
        else
            value = Mathf.Clamp(value, targetAlpha, 1f);

        if (lastValue != value)
        {
            foreach (Graphic fadeGraphic in fadeGraphicList)
            {
                fadeGraphic.color = new Color(fadeGraphic.color.r, fadeGraphic.color.g, fadeGraphic.color.b, value);
            }
        }
        lastValue = value;
    }
    public void SetAlpha(float value)
    {
        lastValue = -1f;
        this.value = value;
        targetAlpha = value;
        speed = 0f;
    }
    
    // 0f - transparent
    // 1f - visible
    public void FadeTo(float targetAlpha, float duration, float delay = 0f)
    {
        if(delay > 0)
            StartCoroutine(SetValues(targetAlpha, duration, delay));
        else
        {
            this.speed = (targetAlpha - value) / duration;
            this.targetAlpha = targetAlpha;
        }
    }
    private IEnumerator SetValues(float targetAlpha, float duration, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        this.speed = (targetAlpha - value) / duration;
        this.targetAlpha = targetAlpha;
    }

    public void AddGraphics(List<Graphic> graphicList)
    {
        fadeGraphicList.AddRange(graphicList);
    }
    public void AddGraphics(Graphic[] graphicArray)
    {
        fadeGraphicList.AddRange(graphicArray);
    }

    public float GetAlpha()
    {
        return value;
    }
    public float GetAlphaChangeSpeed()
    {
        return speed;
    }
}
