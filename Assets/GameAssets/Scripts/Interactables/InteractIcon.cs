using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class InteractIcon : MonoBehaviour
{
    public static InteractIcon active;
    //Variables
    [SerializeField]
    private Vector2 renderTextureResolution;
    [SerializeField]
    private RectTransform mainTransform;
    [SerializeField]
    private float scalingFactor = 1f;
    [SerializeField]
    private Image progressImage;
    [SerializeField]
    private float progressSpeed = 1f;
    public Animator animator;
    
    [Header("Text")] 
    [SerializeField] 
    private TMP_Text objectText;
    [SerializeField] 
    private TMP_Text actionText;
    //Run Time
    private Interactable activeTarget;
    private float currentProgress;
    private float targetProgress;
    private void Awake()
    {
        active = this;
    }
    
    private void LateUpdate()
    {
        if (activeTarget)
        {
            //Position
            if(activeTarget.iconPosition != null)
                UpdatePos(activeTarget.iconPosition.position);
            else
                UpdatePos(PlayerCamera.active.GetRaycastPos());

        }
        else
        {
           // if(mainTransform.gameObject.activeSelf == true)
               // mainTransform.gameObject.SetActive(false);
        }
        //Interaction Progress
        currentProgress = math.lerp(currentProgress, targetProgress, progressSpeed * Time.deltaTime);
        progressImage.fillAmount = currentProgress;
    }

    public void Enable(Interactable interactable)
    {
        activeTarget = interactable;
        objectText.text = interactable.GetName();
        actionText.text = interactable.GetAction();
    }
    public void Enable(string objectTextString, string actionTextString, Vector3 worldPos)
    {
        //activeTarget = interactable;
        objectText.text = objectTextString;
        actionText.text = actionTextString;
        UpdatePos(worldPos);
    }
    public bool IsEnabled()
    {
        return mainTransform.gameObject.activeSelf;
    }
    private void UpdatePos(Vector3 worldPos)
    {
        PlayerCamera.active.WorldPosToUI(worldPos, out Vector3 screenPos, out bool onScreen);
        if (onScreen == false)
        {
            if (mainTransform.gameObject.activeSelf == true)
                mainTransform.gameObject.SetActive(false);
        }
        else
        {
            if (mainTransform.gameObject.activeSelf == false)
                mainTransform.gameObject.SetActive(true);
            mainTransform.position = screenPos;
        }

        //Scale
        float scale = scalingFactor / Vector3.Distance(PlayerCamera.active.transform.position, worldPos);
        mainTransform.localScale = new Vector3(scale, scale, scale);
    }

    public void Refresh()
    {
        if(activeTarget == null)
            return;
        
        objectText.text = activeTarget.GetName();
        actionText.text = activeTarget.GetAction();
    }
    public void Disable()
    {
        activeTarget = null;
        mainTransform.gameObject.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        targetProgress = progress;
    }
}
