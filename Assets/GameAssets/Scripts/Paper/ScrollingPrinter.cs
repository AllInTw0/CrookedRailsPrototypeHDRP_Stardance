using System.Collections.Generic;
using UnityEngine;

public class ScrollingPrinter : Printer
{
    //Variables
    [Header("Scrolling Printer")]
    [SerializeField]
    public MeshRenderer paperMesh;
    [SerializeField]
    public float printSpeed;

    void Update()
    {
        if (InputManager.confirmAction.WasPerformedThisFrame())
            AddNotification(PaperRenderer.active.RenderPaper("HaulingJob", new List<Override>()));

        UpdatePrinterCall();

        if(activeNotification != null)
        {
            //Debug.Log("1");
            if (paperMesh.material.mainTextureOffset.y != 0)
            {
                paperMesh.material.mainTextureOffset += new Vector2(0, printSpeed * Time.deltaTime);
                if (paperMesh.material.mainTextureOffset.y >= 1)
                    paperMesh.material.mainTextureOffset = new Vector2(0, 0);
            }
        }
        else
        {
            //Debug.Log("2");
            if (paperMesh.material.mainTextureOffset.y != 0.5f)
            {
                paperMesh.material.mainTextureOffset += new Vector2(0, printSpeed * Time.deltaTime);
                if (paperMesh.material.mainTextureOffset.y >= 0.5f)
                    paperMesh.material.mainTextureOffset = new Vector2(0, 0.5f);
            }
        }
    }
    public override void BeginPrint()
    {
        base.BeginPrint();

        paperMesh.material.mainTexture = activeNotification.texture;
    }
    //public override void ClearPrint()
    //{
    //    base.ClearPrint();
    //}
    public override bool CanPlayNotification()
    {
        return paperMesh.material.mainTextureOffset.y == 0.5f;
    }
}
