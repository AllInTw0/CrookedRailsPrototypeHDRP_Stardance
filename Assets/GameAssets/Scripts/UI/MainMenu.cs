using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Fade mainMenuFade;

    [SerializeField]
    private Button playButton;
    [SerializeField]
    private Button settingsButton;
    [SerializeField]
    private Button quitButton;
    [SerializeField]
    private string clickSoundString = "Click";
    [SerializeField]
    private AudioSource musicAudioSource;

    private bool gameStarting = false;
    private float musicStartVolume;
    private float fadeTime;
    private float timer;

    private void Start()
    {
        musicStartVolume = musicAudioSource.volume;

        playButton.onClick.AddListener(() =>
        {  
            if (gameStarting || mainMenuFade.GetAlphaChangeSpeed() < 0f) return;
            SoundManager.active.Play(clickSoundString);
            Debug.Log("Play");
            fadeTime = 1.3f;
            musicStartVolume = musicAudioSource.volume;
            gameStarting = true;
            Fade.screenFade.FadeTo(1f, fadeTime-0.1f);
            timer = 0f;
        });
        settingsButton.onClick.AddListener(() =>
        {
            if (gameStarting || mainMenuFade.GetAlphaChangeSpeed() < 0f) return;
            SoundManager.active.Play(clickSoundString);
            Debug.Log("Settings Open");
            Settings.active.Open();
            mainMenuFade.FadeTo(0f, 0.5f);
        });
        quitButton.onClick.AddListener(() =>
        {
            SoundManager.active.Play(clickSoundString);
            Debug.Log("Quit");
            Application.Quit();
        });

        Settings.active.onClose.AddListener(() =>
        {
            mainMenuFade.FadeTo(1f, 0.5f);
        });
        mainMenuFade.SetAlpha(0f);
        mainMenuFade.FadeTo(1f, 1f, 2f);
    }
    private void Update()
    {
        if (gameStarting)
        {
            timer += Time.deltaTime;

            musicAudioSource.volume = Mathf.Lerp(musicStartVolume, 0, timer / fadeTime);

            if (timer >= fadeTime)
            {
                SceneManager.LoadScene("Game");
            }
        }
        else
        {
            timer += Time.deltaTime;
            if (timer > 0.35f)
            {         
                musicAudioSource.volume = Mathf.Lerp(0, musicStartVolume, (timer - 0.35f) / 1.5f);
                if (musicAudioSource.isPlaying == false) musicAudioSource.Play();
            }
        }
    }
    public void test()
    {
        Debug.Log("test");
    }
}
