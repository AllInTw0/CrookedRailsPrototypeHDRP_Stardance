using UnityEngine;

public class EnemyWaveTrigger : MonoBehaviour
{
    public WaveEntry wave;
    public void Trigger()
    {
        EnemySpawner.active.SetNextWave(wave);
    }
}
