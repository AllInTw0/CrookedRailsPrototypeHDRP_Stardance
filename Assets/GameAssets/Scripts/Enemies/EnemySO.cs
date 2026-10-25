using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "ScriptableObjects/EnemySO", order = 1)]
public class EnemySO : ScriptableObject
{
    [System.Serializable]
    public class HealthWeightParams
    {
        public HealthType healthType;
        public float sightDistance;
        public float importanceWeight;
    }

    [Header("Enemy")]
    public GameObject prefab;

//    [Header("Speed")]
//    public float speed = 1f;
//    public float acceleration = 1f;
//    public float deceleration = 1f;
//    public float rotationSpeed = 1f;

    [Header("Sight")]
    public List<HealthWeightParams> HealthWeightParamsList = new List<HealthWeightParams>();
    public float sightDistance = 5f;
    public float playerDistanceFromTrainToAttack = 5f;
    
    [Header("Spawning")]
    public float dangerValue;
    public float minWaveSpawnedDangerValue;
    
//    [Header("Misc")]
//    public float gravityForce = 10f;

    
}
