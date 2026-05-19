using System.Collections.Generic;
using UnityEngine;

public class Flickerer : MonoBehaviour
{
    [Header("Params")]
    [SerializeField]
    private float turnOnOffTime = 0.5f;
    [SerializeField]
    private List<GameObject> objectList;

    //Run time
    private float time;
    private bool on;

    void Start()
    {

    }

    void Update()
    {
        if (time <= turnOnOffTime)
        {
            time += Time.deltaTime;
            float normlized = (1f - (time / turnOnOffTime)) * 0.5f;
            float pow = normlized * normlized;
            int num = Mathf.RoundToInt(pow * 10f);

            DisableEnableObjects(num % 2 == 0);
        }
        else
        {
            DisableEnableObjects(on);
        }
    }
    public void TurnOn()
    {
        time = 0f;
        on = true;
    }
    public void TurnOff()
    {
        time = turnOnOffTime;
        on = false;
    }
    private void DisableEnableObjects(bool enable)
    {
        foreach (GameObject obj in objectList)
        {
            obj.SetActive(enable);
        }
    }
}
