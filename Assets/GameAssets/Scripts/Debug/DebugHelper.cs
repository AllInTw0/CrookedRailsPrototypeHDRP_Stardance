using UnityEngine;

public class DebugHelper : MonoBehaviour
{
    [Header("Train")]
    public RailCarSO railCar;
    public CargoSO cargo;
    [Header("Enemy")] 
    public EnemySO enemy;
    [Header("Item")]
    public ItemSO item;
    [Header("Camera shake")]
    public float shakeStrength;
    public float shakeSpring;
    public float shakeDamp;
}
