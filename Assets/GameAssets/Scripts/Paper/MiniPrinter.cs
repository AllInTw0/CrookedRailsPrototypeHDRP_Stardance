using System.Collections.Generic;
using UnityEngine;


public class MiniPrinter : AnimatedPrinter
{
    public static MiniPrinter active;
    [SerializeField]
    private Camera renderCamera;
    [SerializeField]
    private GameObject UIgameobject;
    void Start()
    {
        animator.SetBool("Enabled", false);
        renderCamera.enabled = false;
        UIgameobject.SetActive(false);
        active = this;
    }

    private void Update()
    {
        if (InputManager.moneyAction.WasPressedThisFrame())
        {
            Override moneySumOverride = new Override("Ammount", OverrideType.Text, Money.GetAmount() + "$");
            AddNotification(PaperRenderer.active.RenderPaper("Money", new List<Override>() { moneySumOverride }));
        }
        UpdatePrinterCall();
    }
    public override void BeginPrint()
    {
        base.BeginPrint();
        renderCamera.enabled = true;
        UIgameobject.SetActive(true);
    }
    public override void DisablePrint()
    {
        base.DisablePrint();
        renderCamera.enabled = false;
        UIgameobject.SetActive(false);
    }

    public override bool OverrideClearTime()
    {
        return InputManager.confirmAction.WasPressedThisFrame();
    }
}
