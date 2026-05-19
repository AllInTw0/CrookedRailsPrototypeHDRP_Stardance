using System.Collections.Generic;
using UnityEngine;


public class MiniPrinter : AnimatedPrinter
{
    public static MiniPrinter active;
    [SerializeField]
    private Camera renderCamera;
    void Start()
    {
        animator.SetBool("Enabled", false);
        renderCamera.enabled = false;
        active = this;
    }

    private void Update()
    {
        if (InputManager.moneyAction.WasPressedThisFrame())
        {
            Override moneySumOverride = new Override("Ammount", OverrideType.Text, Money.GetAmmount() + "$");
            AddNotification(PaperRenderer.active.RenderPaper("Money", new List<Override>() { moneySumOverride }));
        }
        UpdatePrinterCall();
    }
    public override void BeginPrint()
    {
        base.BeginPrint();
        renderCamera.enabled = true;
    }
    public override void DisablePrint()
    {
        base.DisablePrint();
        renderCamera.enabled = false;
    }

    public override bool OverrideClearTime()
    {
        return InputManager.confirmAction.WasPressedThisFrame();
    }
}
