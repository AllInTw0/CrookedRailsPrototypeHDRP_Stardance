using Unity.Netcode;
using UnityEngine;

public class PayloadCraneRailCar : RailCar
{
    [Header("Crane")] 
    [SerializeField]
    private float maxPickUpDistance;
    [SerializeField] 
    private CargoSO payloadCargoSO;
    private Payload targetPayload;
    public override void Update()
    {
        base.Update();

        if (targetPayload == null)
        {
            if (IsMoving() == false)
            {
                for (int i = 0; i < Payload.activePayloadList.Count; i++)
                {
                    float distance = Vector3.Distance(Payload.activePayloadList[i].transform.position, transform.position);
                    if (Payload.activePayloadList[i].CanBePickedUp() && distance < maxPickUpDistance)
                    {
                        targetPayload = Payload.activePayloadList[i];
                        Payload.activePayloadList[i].LinkCrane(this);
                    }
                }
            }
        }
        else
        {
            if (GetCargoInfo().GetCargoSO() == null)
            {
                if (IsServer)
                {
                    SetCargo(payloadCargoSO, Random.Range(payloadCargoSO.payRange.x, payloadCargoSO.payRange.y), GetCargoInfo().railCarValue);
                    Health railCarHealth = targetPayload.GetConsist()[0].GetCargoInfo().GetRailCarHealth();
                    Health cargoHealth = GetCargoInfo().GetCargoHealth();
                    cargoHealth.SetMaxHealth(railCarHealth.GetMaxHealth());
                    cargoHealth.SetHealth(railCarHealth.GetHealth());
                    targetPayload.GetComponent<NetworkObject>().Despawn(true);
                }
            }
        }
    }
}
