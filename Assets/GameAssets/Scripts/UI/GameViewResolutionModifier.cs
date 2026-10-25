using UnityEngine;

public class GameViewResolutionModifier : MonoBehaviour
{

    public Vector2 gameViewAspectRatio;
    public RectTransform gameViewRect;
    void Update()
    {
        //Debug.Log($"{Screen.currentResolution}, {Screen.height},{Screen.width}");
        
        float mult = Screen.height / gameViewAspectRatio.y;
        gameViewRect.sizeDelta = new Vector2(gameViewAspectRatio.x * mult, gameViewAspectRatio.y * mult);
    }
}
