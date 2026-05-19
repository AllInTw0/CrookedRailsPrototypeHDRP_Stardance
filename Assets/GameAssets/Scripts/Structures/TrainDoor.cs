using UnityEngine;

public class TrainDoor : Door
{
    [Header("Train Door")]
    [SerializeField]
    private float minOpenSpeed;
    [SerializeField]
    private float maxTrainDistance;
    void Start()
    {
        
    }

    void Update()
    {
        float timeToOpen = 1f / minOpenSpeed;

        RailCar frontRailCar = Train.playerTrain.GetRailCarAtIndex(0);
        Vector3 trainPos = frontRailCar.transform.position + frontRailCar.transform.forward * (frontRailCar.frontLength + 1f);

        float distanceToTrain = Vector3.Distance(transform.position,trainPos);

        //Debug.DrawRay(trainPos, Vector3.up, Color.blueViolet);

        float trainSpeed = Train.playerTrain.GetSpeed();
        float trainTime = distanceToTrain / trainSpeed;

        //Debug.Log(trainTime + ", " + timeToOpen);
        if(trainTime <= timeToOpen)
        {
            Open();
            doorSpeed = 1f / trainTime;
        }

        UpdateDoor();
    }
}
