using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using UnityEngine.AI;


public class EnemyPack
{
    public Vector3 centerPos;
    public float sizeRadius;
    public float affectRadius;
    public List<Enemy> enemyList = new List<Enemy>();
    public void AddEnemy(Enemy enemy)
    {
        enemyList.Add(enemy);
        sizeRadius += EnemyManager.active.packSizeMult;
        affectRadius += EnemyManager.active.packSizeMult;
    }
    public void RemoveEnemy(Enemy enemy)
    {
        enemyList.Remove(enemy);
        sizeRadius -= EnemyManager.active.packSizeMult;
        affectRadius -= EnemyManager.active.packSizeMult;
    }
    public void AngerPack(Transform target)
    {
        for (int i = 0; i < enemyList.Count; i++)
        {         
            if(enemyList[0] is GroundEnemy)
            {
                ((GroundEnemy)enemyList[0]).Anger(target, 15f); //Should also remove enemy from pack
            }
        }
        EnemyManager.active.activePackList.Remove(this);
    }
}
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager active;
    
    //Variables
    [Header("Temp")]
    [SerializeField] 
    private GameObject enemyPrefab;
    [SerializeField] 
    private int enemyCount_Debug;
    [SerializeField] 
    private int targetEnemyCount_Debug;
    [Header("Enemy List")]
    [SerializeField]
    private List<EnemySO> inGameEnemyList;
    [Header("Updating")]
    [SerializeField] 
    public float behaviourRefreshRate = 0.5f;
    [Header("Enemy Code Refrences")]
    [SerializeField]
    public LayerMask groundLayer;
    [SerializeField]
    public float pathFindingTriggerMovedDistance = 0.35f;
    [Header("Pack params")]
    [SerializeField]
    public Vector2 packSpawnCheckRateMinMax;
    public float packBaseSizeRadius; //Min value
    public float packBaseAffectRadius; //Min value
    public float packSizeMult; //increase radius based on enemy count
    [Header("Player")]
    public float maxDistanceFromPlayer;

    //Run Time
    private List<Enemy> enemyList = new List<Enemy>();
    public List<Health>[] healthTypeArray;
    public List<EnemyPack> activePackList = new List<EnemyPack>();

    private float spawnCoolDownTime;
    private float packSpawnCheckTimer;
    private int j;

    private void Awake()
    {
        active = this;
        healthTypeArray = new List<Health>[System.Enum.GetValues(typeof(HealthType)).Length];
        for (int i = 0; i < healthTypeArray.Length; i++)
        {
            healthTypeArray[i] = new List<Health>();
        }
        j = enemyCount_Debug;
        InvokeRepeating(nameof(UpdateEnemyBehaviour),0f,behaviourRefreshRate);
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < enemyList.Count; i++)
        {
            enemyList[i].FixedUpdateCall();
        }
    }

    void Update()
    {
        //Pack spawning
        packSpawnCheckTimer -= Time.deltaTime;
        if(packSpawnCheckTimer <= 0)
        {
            List<Enemy> enemyListCopy = new List<Enemy>(enemyList);

            while (enemyListCopy.Count > 0)
            {
                int index = Random.Range(0, enemyListCopy.Count);
                if (enemyListCopy[index].IsInPack() == false && ((GroundEnemy)enemyListCopy[index]).GetState() == GroundEnemy.State.Idle && 
                    NavMesh.SamplePosition(enemyListCopy[index].transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    //Check if the pack point isnt too close to others
                    bool allowCreation = true;
                    foreach (EnemyPack pack in activePackList)
                    {
                        if (Vector3.Distance(hit.position, pack.centerPos) < (packBaseAffectRadius + pack.affectRadius) * 0.95f)
                        {
                            allowCreation = false;
                            break;
                        }
                    }

                    if (allowCreation)
                    {
                        EnemyPack enemyPack = new EnemyPack();
                        enemyPack.centerPos = hit.position;
                        enemyPack.sizeRadius = packBaseSizeRadius;
                        enemyPack.affectRadius = packBaseAffectRadius;

                        activePackList.Add(enemyPack);
                        Debug.Log("Pack created");

                        break;
                    }
                }
                enemyListCopy.RemoveAt(index);
            }
            packSpawnCheckTimer = Random.Range(packSpawnCheckRateMinMax.x, packSpawnCheckRateMinMax.y);
        }
        
        for (int i = 0; i < enemyList.Count; i++)
        {
            float dist = Vector3.Distance(PlayerMovement.active.transform.position, enemyList[i].transform.position);
            if(dist > maxDistanceFromPlayer)
            {
                Destroy(enemyList[i].gameObject);
                enemyList.RemoveAt(i);
                i--;
                Debug.Log("Enemy To Far! Destroying");
            }
            else
                enemyList[i].UpdateCall();
        }
        
        //Debug spawning
        if (j > 0)
        {
            enemyList.Add(Instantiate(enemyPrefab, new Vector3(Random.Range(-3f,3f),Random.Range(0f,0f),Random.Range(-3f,3f)), Quaternion.identity).transform.GetComponent<Enemy>());
            j--;
        }

        if (enemyList.Count < targetEnemyCount_Debug && j <= 0)
        {
            enemyList.Add(Instantiate(enemyPrefab, new Vector3(Random.Range(-3f,3f),Random.Range(0f,0f),Random.Range(-3f,3f)), Quaternion.identity).transform.GetComponent<Enemy>());
        }
    }

    private void UpdateEnemyBehaviour()
    {
        for (int i = 0; i < enemyList.Count; i++)
        {
            enemyList[i].UpdateBehaviour();
        }
    }

    public void RemoveEnemy(Enemy enemy)
    {
        enemyList.Remove(enemy);
    }

    public Enemy[] GetEnemies()
    {
        return enemyList.ToArray();
    }
    public EnemySO[] GetInGameEnemies()
    {
        return inGameEnemyList.ToArray();
    }
    public void SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, rotation);
        enemyList.Add(enemy.transform.GetComponent<Enemy>());
    }
    public int GetEnemyCount()
    {
        return enemyList.Count;
    }

    public static void GenerateNavMesh(Vector3 worldPos)
    {
        Vector2Int coord = TerrainGeneration.active.GetChunkCoord(worldPos);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                TerrainGeneration.Chunk chunk = TerrainGeneration.active.CreateOrGetChunk(new Vector2Int(coord.x + x, coord.y + y));
                chunk.GenerateNavMesh();
            }
        }
    }

    public void OnDrawGizmos()
    {
        foreach (EnemyPack pack in activePackList)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pack.centerPos, 0.2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pack.centerPos, pack.sizeRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pack.centerPos, pack.affectRadius);
        }
    }
}
