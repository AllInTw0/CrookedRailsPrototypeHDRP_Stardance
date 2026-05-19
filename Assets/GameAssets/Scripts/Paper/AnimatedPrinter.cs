using UnityEngine;

public class AnimatedPrinter : Printer
{
    //Variables
    [Header("Animated Printer")]
    [SerializeField]
    public SkinnedMeshRenderer paperMesh;
    [SerializeField]
    public Animator animator;
    [SerializeField]
    public string printerLoweredAnimationName;

    void Start()
    {
        animator.SetBool("Enabled", false);
    }

    public override void BeginPrint()
    {
        base.BeginPrint();

        paperMesh.sharedMaterial.mainTexture = activeNotification.texture;

        animator.SetBool("Enabled", true);
    }
    public override void ClearPrint()
    {
        animator.SetBool("Enabled", false);
        base.ClearPrint();
    }
    public override bool CanPlayNotification()
    {
        return animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == printerLoweredAnimationName;
    }
}
