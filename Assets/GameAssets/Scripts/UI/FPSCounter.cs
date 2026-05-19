using UnityEngine;
using TMPro;
using System.Collections.Generic;
public class FPSCounter : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;
    [SerializeField]
    private float updateInterval;

    private List<int> fpsSamples = new List<int>();
    private float time;

    private float fps = 0;
    private void Update()
    {
        fpsSamples.Add((int)(1f / Time.unscaledDeltaTime));
        time += Time.deltaTime;

        if (time >= updateInterval)
        {
            int sum = 0;
            foreach (int fpsSample in fpsSamples)
            {
                sum += fpsSample;
            }
            fps = sum / fpsSamples.Count;
            time = 0;
            fpsSamples = new List<int>();

            text.text = fps.ToString() + " FPS";
        }
    }
}
