using UnityEngine;

public class TrainEffects : MonoBehaviour
{
    [SerializeField]
    private RunningGear targetRunningGear;
    [Header("Effects")]
    [SerializeField]
    private ParticleSystem chuffParticleSystem;
    [SerializeField]
    private string chuffSoundString;
    [SerializeField]
    private Transform[] chuffSoundOriginArray;

    private float rotation = 0f;
    private bool piston1 = false;
    private void Update()
    {
        float rotationTravelled = targetRunningGear.GetRotationTravelled();
        rotation += rotationTravelled;
        if(rotation >= 90f)
        {
            rotation = 0f;
            PreformChuff();
        }
        else if (rotation <= -90f)
        {
            rotation = 0f;
            PreformChuff();
        }
    }
    private void PreformChuff()
    {
        chuffParticleSystem.Emit(Random.Range(8, 10));
        SoundManager.active.PlayAtPos(chuffSoundOriginArray[piston1 ? 0 : 1].position, chuffSoundString);
        piston1 = !piston1;
    }

}
