
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
public enum AudioGroupType
{
    Effects,
    HitSound,
    Music
}
public class SoundManager : MonoBehaviour
{

    public static SoundManager active;
    [SerializeField] 
    private SoundInfoSO soundInfoSO;
    [System.Serializable]
    private class GroupEntry
    {
        public AudioGroupType type;
        public AudioMixerGroup group;
    }
    [SerializeField]
    private List<GroupEntry> groupEntryList;
    private Dictionary<AudioGroupType, AudioMixerGroup> mixerGroupDictionary;
    [SerializeField]
    private AudioMixer audioMixer;
    private void Awake()
    {
        active = this;
        mixerGroupDictionary = new Dictionary<AudioGroupType, AudioMixerGroup>();
        foreach (GroupEntry entry in groupEntryList)
        {
            mixerGroupDictionary.Add(entry.type, entry.group);
        }
    }
    public static void SetMixerParam(string param, float value)
    {
        active.audioMixer.SetFloat(param, value);
    }

    public void PlayAtPos(Vector3 pos, string soundName, float spatialBlend = 1f, Transform parent = null)
    {
        if(string.IsNullOrEmpty(soundName)) return;
        SoundInfo soundInfo = GetSoundInfo(soundName);
        if (soundInfo != null)
        {
            GameObject soundObject = new GameObject(soundName);
            soundObject.transform.position = pos;
            soundObject.transform.SetParent(parent);
            
            AudioSource source = soundObject.AddComponent<AudioSource>();
            
            source.clip = soundInfo.AudioClips[Random.Range(0, soundInfo.AudioClips.Count - 1)];
            source.volume = soundInfo.volume;
            source.pitch = Random.Range(soundInfo.pitch.x, soundInfo.pitch.y);

            source.spatialBlend = spatialBlend;

            source.outputAudioMixerGroup = mixerGroupDictionary[soundInfo.groupType];

            source.Play();
            
            Destroy(soundObject,source.clip.length);
        }
        else
        {
            Debug.LogWarning("Sound Not Found: " + soundName);
        }
    }
    public void Play(string soundName)
    {
        if(string.IsNullOrEmpty(soundName)) return;
        SoundInfo soundInfo = GetSoundInfo(soundName);
        if (soundInfo != null)
        {
            GameObject soundObject = new GameObject(soundName);

            AudioSource source = soundObject.AddComponent<AudioSource>();

            source.clip = soundInfo.AudioClips[Random.Range(0, soundInfo.AudioClips.Count - 1)];
            source.volume = soundInfo.volume;
            source.pitch = Random.Range(soundInfo.pitch.x, soundInfo.pitch.y);

            source.spatialBlend = 0f;

            source.outputAudioMixerGroup = mixerGroupDictionary[soundInfo.groupType];

            source.Play();

            Destroy(soundObject, source.clip.length);
        }
        else
        {
            Debug.LogWarning("Sound Not Found: " + soundName);
        }
    }
    private SoundInfo GetSoundInfo(string soundName)
    {
        for (int i = 0; i < soundInfoSO.soundList.Count; i++)
        {
            if (soundInfoSO.soundList[i].name == soundName)
                return soundInfoSO.soundList[i];
        }

        return null;
    }
}
