using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundInfoSO", menuName = "ScriptableObjects/SoundInfoSO", order = 1)]
public class SoundInfoSO : ScriptableObject
{
    public List<SoundInfo> soundList = new List<SoundInfo>();
}

[System.Serializable]
public class SoundInfo
{
    public string name;
    public List<AudioClip> AudioClips = new List<AudioClip>();
    [Range(0f,1f)]
    public float volume = 1f;

    public Vector2 pitch = new Vector2(1f,1f);

    public AudioGroupType groupType;
}
