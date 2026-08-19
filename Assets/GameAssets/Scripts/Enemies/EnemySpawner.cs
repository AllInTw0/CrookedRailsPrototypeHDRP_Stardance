using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class WaveEntry
{
    public string name;
    [Header("Total Danger")]
    public Vector2 minMaxTargetDangerPrecent;
    [Header("Spawning")]
    public Vector2 minMaxSpawnDangerAtOnce;
    public Vector2 minMaxSpawnCooldown;
    public Vector2 minMaxDistance;
    public Vector2 minMaxRot;

    [Header("CoolDown")]
    public Vector2 minMaxWaveCooldown;
    public Vector2 minMaxTravelDistanceCooldown;
    public float travelDistanceDecay;

    [Header("Probability")]
    public float minDistanceTravelled;
    public float probability;
    public bool canRepeat;

    public void ResetValues(float maxDanger)
    {
        spawnedDanger = 0f;
        targetSpawnedDanger = maxDanger * Random.Range(minMaxTargetDangerPrecent.x, minMaxTargetDangerPrecent.y);
        distanceTravelled = 0f;
    }

    [HideInInspector]
    public float spawnedDanger = 0f;
    [HideInInspector]
    public float targetSpawnedDanger = 0f;
    [HideInInspector]
    public float distanceTravelled;
}

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner active;

    [Header("Wave types")]
    [SerializeField]
    private List<WaveEntry> waveEntryList;
    [Header("Danger")]
    [SerializeField]
    private AnimationCurve baseLevelDanger;
    [Header("Spawning")]
    [SerializeField]
    private int minTrainSectionCountAhead;
    [SerializeField]
    private int minTrainSectionCountBehind;
    [Header("Debug")]
    [SerializeField]
    private bool enemiesSpawnAlways;
    [SerializeField]
    private bool disableEnemySpawning;

    private EnemySO[] inGameEnemyArray;
    private Enemy[] spawnedEnemyArray;

    private float updateCoolDown;
    private float lastCoolDownLength;

    //Wave variables
    private WaveEntry nextWave;
    private WaveEntry currentWave;
    private List<WaveEntry> lastWaves = new List<WaveEntry>();
    private float waveCooldown;
    private float travelDistanceCooldown;
    private float travelDistanceDecay;
    private float lastTrainDistance;

    //Spawning variables

    private void Start()
    {
        active = this;
        inGameEnemyArray = EnemyManager.active.GetInGameEnemies();

        //Sort wave entry list by probability
        bool sorted = false;
        while (sorted == false)
        {
            sorted = true;
            for (int i = 0; i < waveEntryList.Count - 1; i++)
            {
                if (waveEntryList[i].probability < waveEntryList[i + 1].probability)
                {
                    var temp = waveEntryList[i];
                    waveEntryList[i] = waveEntryList[i + 1];
                    waveEntryList[i + 1] = temp;
                    sorted = false;
                }
            }
        }
    }
    private void Update()
    {
        if(NetworkManager.Singleton && NetworkManager.Singleton.IsServer == false)
            return;
        

        if (CanEnemiesSpawn() == false || disableEnemySpawning)
        {
            SetUpdateCoolDown(2f);
            return;
        }

        updateCoolDown -= Time.deltaTime;
        if(updateCoolDown <= 0f)
        {
            UpdateEnemySpawning(lastCoolDownLength);
        }
    }
    public static void ResetWaveValues(float cooldown)
    {
        active.nextWave = null;
        active.currentWave = null;
        active.waveCooldown = cooldown;
        active.travelDistanceCooldown = 0f;
        active.lastWaves = new List<WaveEntry>();
        foreach (WaveEntry wave in active.waveEntryList)
        {
            wave.ResetValues(0f);
        }
    }
    public bool CanEnemiesSpawn()
    {
        if (enemiesSpawnAlways)
            return true;

        if (Train.playerTrain)
        {
            if (Train.playerTrain.controlls.currentState.Value == LocomotiveControls.State.supersonic)
                return false;

            int sectionsForward = 0;
            TrackSection section = Train.playerTrain.frontTrackSection;
            while (section != null)
            {
                sectionsForward++;
                section = section.nextSection;
            }

            if (sectionsForward < active.minTrainSectionCountAhead)
                return false;

            int sectionsBackwards = 0;
            section = Train.playerTrain.frontTrackSection;
            while (section != null)
            {
                sectionsBackwards++;
                section = section.previousSection;
            }

            if (sectionsBackwards < active.minTrainSectionCountBehind)
                return false;
        }

        return true;
    }
    private void UpdateEnemySpawning(float deltaTime)
    {
        spawnedEnemyArray = EnemyManager.active.GetEnemies();
        float totalSpawnedDanger = GetTotalDanger();

        int totalMaxHealth = 0;
        int totalHealth = 0;
        foreach (Player player in Player.playerList)
        {
            totalMaxHealth += player.health.maxHealth;
            totalHealth += player.health.GetHealth();
        }
        
        float maxDanger = GetMaxDanger() * (1f - 0.2f * ((totalMaxHealth - totalHealth) / totalMaxHealth));

        float freeDanger = maxDanger - totalSpawnedDanger;

        //Train travel distance
        float traveledDistance = GameStateManager.distanceTravelled - lastTrainDistance;
        lastTrainDistance = GameStateManager.distanceTravelled;

        foreach (WaveEntry wave in waveEntryList)
        {
            wave.distanceTravelled += traveledDistance;
        }

        if (currentWave != null)
        {
            //A wave is active
            float randomSpawnDanger = Random.Range(currentWave.minMaxSpawnDangerAtOnce.x, currentWave.minMaxSpawnDangerAtOnce.y);
            if(freeDanger < randomSpawnDanger)
            {
                SetUpdateCoolDown(1f);
                return;
            }

            Debug.Log("Spawning: " + randomSpawnDanger);
            float spawnedDanger = SpawnDanger(randomSpawnDanger, currentWave.minMaxDistance, currentWave.minMaxRot);

            currentWave.spawnedDanger += spawnedDanger;
            if(currentWave.spawnedDanger >= currentWave.targetSpawnedDanger)
            {
                EndWave();
                return;
            }
            SetUpdateCoolDown(Random.Range(currentWave.minMaxSpawnCooldown.x, currentWave.minMaxSpawnCooldown.y));
            return;
        }
        else if(nextWave != null)
        {
            //Next wave is picked

            travelDistanceCooldown -= traveledDistance;
            travelDistanceCooldown -= travelDistanceDecay * Time.deltaTime;
            waveCooldown -= deltaTime;
            if (waveCooldown <= 0f && travelDistanceCooldown <= 0f)
            {
                currentWave = nextWave;
                nextWave = null;

                currentWave.ResetValues(maxDanger);
                Debug.Log("Wave Strated! " + currentWave.name);
            }
        }
        else
        {
            //Pick next wave
            List<WaveEntry> waveEntryListCopy = new List<WaveEntry>(waveEntryList);
            for (int i = 0; i < waveEntryListCopy.Count; i++)
            {
                if (waveEntryListCopy[i].minDistanceTravelled > waveEntryListCopy[i].distanceTravelled && Train.playerTrain)
                {
                    waveEntryListCopy.RemoveAt(i);
                    i--;
                }
            }
            foreach (WaveEntry waveEntry in lastWaves)
            {
                if(waveEntry.canRepeat == false)
                {
                    waveEntryListCopy.Remove(waveEntry);
                }
            }

            float probabilitySum = 0f;
            foreach (WaveEntry waveEntry in waveEntryListCopy)
            {
                probabilitySum += waveEntry.probability;
            }

            float randomProbability = Random.Range(0f, probabilitySum);
            foreach (WaveEntry waveEntry in waveEntryListCopy)
            {
                randomProbability -= waveEntry.probability;
                if(randomProbability <= 0f)
                {
                    nextWave = waveEntry;
                    Debug.Log("Wave Picked! " + nextWave.name);
                    return;
                }
            }
        }

        SetUpdateCoolDown(0.5f);
    }
    private float SpawnDanger(float targetDanger, Vector2 minMaxDistance, Vector2 minMaxRot)
    {
        float spawnedDanger = 0f;
        float realSpawnedDanger = 0f;
        int playerIndex = 0;
        while (spawnedDanger < targetDanger)
        {
            float freeDanger = targetDanger - spawnedDanger;

            EnemySO enemy = null;
            foreach (EnemySO enemySO in inGameEnemyArray)
            {
                if(enemy == null)
                {
                    enemy = enemySO;
                    continue;
                }

                if (enemySO.dangerValue < freeDanger && enemySO.dangerValue > enemy.dangerValue)
                {
                    enemy = enemySO;
                }
            }

            if (enemy == null)
                return spawnedDanger;

            Vector3 startPos = Player.playerList[playerIndex].transform.position;
            Vector3 travelDir = Player.playerList[playerIndex].GetTravelDir();
                
            Vector3 spawnDir = Quaternion.AngleAxis(Random.Range(minMaxRot.x, minMaxRot.y), Vector3.up) * new Vector3(travelDir.x, 0f, travelDir.y);
            Vector3 spawnPos = startPos + spawnDir.normalized * Random.Range(minMaxDistance.x, minMaxDistance.y);

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 10f, 1 << NavMesh.GetAreaFromName("Walkable"))) {

                EnemyManager.active.SpawnEnemy(enemy.prefab, hit.position, Quaternion.identity);
                realSpawnedDanger += enemy.dangerValue;
            }
            else
            {
                Debug.LogWarning("Did not spawn enemy! Couldnt sample position on nav!");
            }

            spawnedDanger += enemy.dangerValue;
            playerIndex++;
            if (playerIndex >= Player.playerList.Count)
                playerIndex = 0;
        }

        return spawnedDanger;
    }
    public void SetNextWave(WaveEntry wave)
    {
        if(currentWave != null)
            EndWave();

        waveCooldown *= 0.2f;
        travelDistanceCooldown = 0f;
        nextWave = wave;
        SetUpdateCoolDown(0.5f);
    }
    private void EndWave()
    {
        Debug.Log("Wave Ended! " + currentWave.name + " spawnedD: " + currentWave.spawnedDanger + " targetD: " + currentWave.targetSpawnedDanger);

        waveCooldown = Random.Range(currentWave.minMaxWaveCooldown.x, currentWave.minMaxWaveCooldown.y);
        travelDistanceCooldown = Random.Range(currentWave.minMaxTravelDistanceCooldown.x, currentWave.minMaxTravelDistanceCooldown.y);
        travelDistanceDecay = currentWave.travelDistanceDecay;
            
        lastWaves.Add(currentWave);
        currentWave = null;
        SetUpdateCoolDown(0.5f);
    }
    private float GetTotalDanger()
    {
        float sum = 0f;
        foreach (Enemy enemy in spawnedEnemyArray)
        {
            sum += enemy.enemyInfo.dangerValue;
        }
        return sum;
    }

    private float GetMaxDanger()
    {
        float maxDanger = baseLevelDanger.Evaluate(GameStateManager.currentLevel.Value);
        return maxDanger;
    }
    
    public void SetUpdateCoolDown(float value)
    {
       // Debug.Log("Cooldown: " + value);
        updateCoolDown = value;
        lastCoolDownLength = value;
    }

}
