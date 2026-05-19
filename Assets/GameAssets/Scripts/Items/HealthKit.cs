using UnityEngine;

public class HealthKit : Item
{
    [Header("HealthKit")]
    [SerializeField]
    private float useTime;
    [SerializeField]
    private float healAmmount;
    [SerializeField]
    private AudioSource healSoundSource;

    private float time;
    private void Update()
    {
        if (PlayerInventory.active.tool == this)
        {
            if (InputManager.attackAction.IsPressed())
            {
                if (healSoundSource.isPlaying == false)
                    healSoundSource.Play();

                time += Time.deltaTime;
                if(time >= useTime)
                {
                    PlayerInventory.active.UnEquipTool();
                    healSoundSource.Stop();
                    PlayerHealth.active.TakeDamage(-healAmmount);
                    Destroy(gameObject);
                    time = -99f;
                    return;
                }
            }
            else
            {
                time = 0f;
                if (healSoundSource.isPlaying)
                    healSoundSource.Stop();
            }
        }
        else
        {
            time = 0f;
            if (healSoundSource.isPlaying)
                healSoundSource.Stop();
        }
        UpdateItem();
    }
}
