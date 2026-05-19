using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;

[CustomEditor(typeof(ShopItemSO),editorForChildClasses : true)]
public class ShopItemSOEditor : Editor
{
    private ShopItemSO itemInfo;
    
    private GameObject parent;
    private Camera renderCam;
    private GameObject itemObject;
    private RenderTexture renderTexture;
    void OnEnable()
    {
        itemInfo = (ShopItemSO)target;
    }
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI();
        
        if(GUILayout.Button("Setup Rendering"))
        {
            Setup();
        }
        if(GUILayout.Button("Render Icon"))
        {
            RenderIcon();
        }
        if(GUILayout.Button("Cleanup Rendering"))
        {
            CleanUp();
        }
    }
    
    private void Setup()
    {
        //Setup
        parent = new GameObject("IconRendering");
        
        renderCam = Util.CreateCamera(itemInfo.renderLayer, CameraClearFlags.Color);
        renderCam.nearClipPlane = 0.05f;
        renderCam.farClipPlane = 4f;
        //Disable Post Processing
        HDAdditionalCameraData data = renderCam.gameObject.AddComponent<HDAdditionalCameraData>();
        data.customRenderingSettings = true;
        data.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Postprocess] = true;
        data.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
        data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        
        renderCam.transform.SetParent(parent.transform);

        renderTexture = new RenderTexture(256, 256, 32, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        renderTexture.Create();

        renderCam.targetTexture = renderTexture;
        renderCam.forceIntoRenderTexture = true;
        
        itemObject = Instantiate(itemInfo.prefab, Vector3.zero, Quaternion.identity);
        itemObject.transform.SetParent(parent.transform);
        Util.ChangeObjectsLayer(itemObject,itemInfo.renderLayer);
    }
    private void RenderIcon()
    {
        //Render
        itemObject.transform.rotation = Quaternion.Euler(itemInfo.rotation);
        renderCam.transform.position = itemInfo.offset;
        renderCam.transform.LookAt(itemInfo.origin);
        
        Texture2D texture = new Texture2D(256, 256);
        RenderTexture.active = renderTexture;
        renderCam.Render();
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        
        AssetDatabase.CreateAsset(texture, "Assets/GameAssets/SO/Icons/"+itemInfo.GetName()+"Icon.asset");
        itemInfo.icon = texture;
        
    }

    private void CleanUp()
    {
        if(parent != null)
            DestroyImmediate(parent);
        
        RenderTexture.active = null;
        
        if(renderTexture != null)
            renderTexture.Release();
    }
    

}
