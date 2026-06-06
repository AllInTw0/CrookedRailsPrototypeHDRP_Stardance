using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(Shop))]
public class ShopEditor : Editor
{
    private Shop shop;
    
    void OnEnable()
    {
        shop = (Shop)target;
    }
    
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI();

        if (GUILayout.Button("Reroll items"))
        {
            shop.ClearShop();
            shop.Initialize();
        }
    }
}
