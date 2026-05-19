using UnityEngine;
using UnityEngine.Events;

public class MonitorArm : AnimationPlayer
{
    [Header("Light")]
    [SerializeField]
    private Flickerer lightFlickerer;
    [Header("Button")]
    [SerializeField]
    private AnimationPlayer buttonAnimationPlayer;
    [SerializeField]
    public EventInteractable buttonInteractable;
    [Header("Printer")]
    [SerializeField]
    public Printer printer;

    public UnityEvent onInteract;

    private bool monitorEnabled;
    private bool buttonEnabled;

    private void Awake()
    {
        if (buttonInteractable != null) buttonInteractable.interactEvent.AddListener(() => { onInteract.Invoke(); });
        buttonEnabled = true;
        DisableButton();
    }
    public void TurnOnLight()
    {
        lightFlickerer.TurnOn();
    }
    public void TurnOffLight()
    {
        lightFlickerer.TurnOff();
    }
    public void EnableButton()
    {
        if (buttonEnabled) return;

        if (buttonAnimationPlayer != null) buttonAnimationPlayer.PlayAniamtion(buttonAnimationPlayer.animName, 1f);
        if (buttonInteractable != null) buttonInteractable.gameObject.SetActive(true);
        buttonEnabled = true;
    }
    public void DisableButton()
    {
        if (buttonEnabled == false) return;

        if (buttonAnimationPlayer != null) buttonAnimationPlayer.PlayAniamtion(buttonAnimationPlayer.animName, -1f);
        if (buttonInteractable != null) buttonInteractable.gameObject.SetActive(false);
        buttonEnabled = false;
    }

    public void EnableMonitor(bool enableButton = true)
    {
        if (monitorEnabled == true) return;

        TurnOnLight();
        PlayAniamtion(animName, 1f);
        if(enableButton) EnableButton();
        monitorEnabled = true;
    }
    public void DisableMonitor()
    {
        if (monitorEnabled == false) return;

        TurnOffLight();
        PlayAniamtion(animName, -1f);
        DisableButton();
        monitorEnabled = false;
    }
}
