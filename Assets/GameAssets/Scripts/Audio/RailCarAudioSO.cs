using UnityEngine;

[CreateAssetMenu(fileName = "RailCarAudioSO", menuName = "ScriptableObjects/RailCarAudioSO", order = 2)]
public class RailCarAudioSO : ScriptableObject
{
    public RailCarAudio.RollingAudio[] rollingAudioArray;
    public AnimationCurve speedToVolumeCurve;
}
