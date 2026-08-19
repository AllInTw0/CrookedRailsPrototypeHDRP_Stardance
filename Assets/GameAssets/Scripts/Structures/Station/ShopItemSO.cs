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
    [Header("Shop Bonus")] 
    public List<ShopItemSO> bonusItems;
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
    private int lastEvaluationLevel = -1;

    public void ResetLastEvaluation()
    {
        lastEvaluationLevel = -1;
    }
    public void EvaluateCurves(int level)
    {
        System.Random rng = new System.Random(GameStateManager.gameSeed.Value + description.Length + level);
        
        if (level == lastEvaluationLevel)
        {
            Debug.Log("Didnt evaluate item. LastEvaluationLevel: " + lastEvaluationLevel + ", Level: " +  level);
            return;
        }

        randomness = randomnessCurve.Evaluate(level);

        probability = probabilityCurve.Evaluate(level) * ((float)rng.NextDouble() * randomness * 0.2f + 1f);
        price = Mathf.RoundToInt(Mathf.Clamp(priceCurve.Evaluate(level) + ((float)rng.NextDouble() * randomness * 20f), 1f, float.MaxValue));
        if (price > 20)
        {
            price = Mathf.RoundToInt(price / 5f) * 5;
        }
        
        sell = Mathf.RoundToInt(Mathf.Clamp(sellCurve.Evaluate(level) + ((float)rng.NextDouble() * randomness * 5f), 1f, price));
        lastEvaluationLevel = level;
    }

    public void EvaluateStock(int level)
    {
        System.Random rng = new System.Random(GameStateManager.gameSeed.Value + description.Length + level);
        
        stock = Mathf.RoundToInt(stockCurve.Evaluate(level) * ((float)rng.NextDouble() * randomness * 1f + 1f));
        if(stock < 1) stock = 1;
    }
}
