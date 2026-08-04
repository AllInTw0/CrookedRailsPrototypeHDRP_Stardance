using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class RailCarAudio : MonoBehaviour
{
    [System.Serializable]
    public class RollingAudio
    {
        public AudioClip clip;
        public float minSpeed;
    }
    
    private RailCar railCar;
    
    [SerializeField]
    private RailCarAudioSO audioSO;

    private AudioSource[] rollingAudioSourceArray;
    private Coroutine rollingCoroutine;
    private void Start()
    {
        railCar = GetComponent<RailCar>();
        rollingAudioSourceArray = new[] { gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>() };
        ConfigureSource(rollingAudioSourceArray[0]);
        ConfigureSource(rollingAudioSourceArray[1]);
    }

    private void Update()
    {
        if (railCar.IsMoving())
        {
            if (rollingCoroutine == null)
                rollingCoroutine = StartCoroutine(RollingAudioCoroutine());
        }
    }

    private IEnumerator RollingAudioCoroutine()
    {
        while (railCar.IsMoving())
        {
            RollingAudio targetAudio = GetTargetRollingClip(Mathf.Abs(railCar.GetSpeed()));
            if (rollingAudioSourceArray[0].clip != targetAudio.clip)
            {
                rollingAudioSourceArray[1].clip = targetAudio.clip;
                rollingAudioSourceArray[1].time = Random.Range(0f, targetAudio.clip.length * 0.69f);
                rollingAudioSourceArray[1].volume = 0f;
                rollingAudioSourceArray[1].Play();
                yield return StartCoroutine(AudioCrossfade.CrossfadeAudio(rollingAudioSourceArray[0], rollingAudioSourceArray[1], Random.Range(0.75f,1.75f), GetTargetVolume()));
                //Swap
                (rollingAudioSourceArray[0], rollingAudioSourceArray[1]) = (rollingAudioSourceArray[1], rollingAudioSourceArray[0]);
            }
            else
            {
                if(rollingAudioSourceArray[0].isPlaying == false)
                    rollingAudioSourceArray[0].Play();
                rollingAudioSourceArray[0].volume = GetTargetVolume();
                yield return null;
            }
        }
        rollingAudioSourceArray[0].Stop();
        rollingAudioSourceArray[1].Stop();
        rollingCoroutine = null;
    }

    private float GetTargetVolume()
    {
        return audioSO.speedToVolumeCurve.Evaluate(Mathf.Abs(railCar.GetSpeed()));
    }
    private RollingAudio GetTargetRollingClip(float speed)
    {
        for (int i = audioSO.rollingAudioArray.Length-1; i >= 0; i--)
        {
            if(speed > audioSO.rollingAudioArray[i].minSpeed)
                return audioSO.rollingAudioArray[i];
        }

        return audioSO.rollingAudioArray[0];
    }

    private void ConfigureSource(AudioSource source)
    {
        source.spatialBlend = 1f;
        source.maxDistance = 3.5f;
        source.loop = true;
    }
}
