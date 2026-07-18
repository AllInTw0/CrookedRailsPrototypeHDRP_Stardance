using System.Collections;
using UnityEngine;

public static class AudioCrossfade
{
    public static IEnumerator CrossfadeAudio(AudioSource from, AudioSource to, float duration, float targetVolume = 1f)
    {
        double startTime = AudioSettings.dspTime;
        double endTime = startTime + duration;
        float fromVolume = from.volume;
        while (AudioSettings.dspTime < endTime)
        {
            float time = (float)(AudioSettings.dspTime - startTime)/duration;
            from.volume = fromVolume * (1f-time);
            to.volume = targetVolume * time;
            yield return null;
        }
        to.volume = targetVolume;
        from.Stop();
    }
}
