using System.Collections.Generic;
using UnityEngine;

public class ShopItemSO : ScriptableObject
{
    [Header("Shop Item")]
    public string nameOverride;
    public GameObject prefab;
    public string description;

    [Header("Shop Params")]
    public AnimationCurve probabilityCurve;
    public AnimationCurve priceCurve;
    public AnimationCurve stockCurve;
    public AnimationCurve randomnessCurve;
    public AnimationCurve sellCurve;
    public List<ShopItemSO> neededBoughtItems;
    public int maxCountBought = -1;

    [Header("Icon Rendering")]
    public Vector3 offset = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 origin;
    public Vector3 rotation;
    public Texture2D icon;

    public LayerMask renderLayer;
    public string GetName()
    {
        return nameOverride == "" ? this.name : nameOverride;
    }


    //Per level stat evaluation
    [HideInInspector] public float probability;
    [HideInInspector] public int price;
    [HideInInspector] public int stock;
    [HideInInspector] public int sell;
    [HideInInspector] public float randomness;
    private float lastEvaluation;
    public void EvaluateCurves(float value, int forcedStock = -1)
    {
        if (value == lastEvaluation && value != 0f) return;

        randomness = randomnessCurve.Evaluate(value);

        probability = probabilityCurve.Evaluate(value) * (Random.value * randomness * 0.2f + 1f);
        price = Mathf.RoundToInt(Mathf.Clamp(priceCurve.Evaluate(value) + (Random.value * randomness * 20f), 1f, float.MaxValue));
        if (price > 20f)
        {
            price = Mathf.RoundToInt(price / 5f) * 5;
        }

        if (forcedStock != -1)
        {
            stock = Mathf.RoundToInt(Mathf.Clamp(stockCurve.Evaluate(value) * (Random.value * randomness * 1f + 1f), 1f,
                float.MaxValue));
            if (stock < 1)
                stock = 1;
        }
        else stock = forcedStock;

        sell = Mathf.RoundToInt(Mathf.Clamp(sellCurve.Evaluate(value) + (Random.value * randomness * 5f), 1f, price));
        lastEvaluation = value;
    }
}
