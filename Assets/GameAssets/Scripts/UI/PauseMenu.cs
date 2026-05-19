using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private Fade pauseMenuFade;

    [SerializeField]
    private Button resumeButton;
    [SerializeField]
    private Button settingsButton;
    [SerializeField]
    private Button quitButton;
    [SerializeField]
    private string clickSoundString = "Click";

    private void Start()
    {
        resumeButton.onClick.AddListener(() =>
        {
            if (pauseMenuFade.GetAlphaChangeSpeed() < 0f) return;
            SoundManager.active.Play(clickSoundString);
            Resume();
        });
        settingsButton.onClick.AddListener(() =>
        {
            if (pauseMenuFade.GetAlphaChangeSpeed() < 0f) return;
            SoundManager.active.Play(clickSoundString);
            Debug.Log("Settings Open");
            Settings.active.Open();
            pauseMenuFade.FadeTo(0f, 0.5f);
        });
        quitButton.onClick.AddListener(() =>
        {
            Debug.Log("Quit");
            SoundManager.active.Play(clickSoundString);
            StartCoroutine(MainMenu());
        });

        Settings.active.onClose.AddListener(() =>
        {
            pauseMenuFade.FadeTo(1f, 0.5f);
        });

        pauseMenuFade.SetAlpha(0f);
    }
    private IEnumerator MainMenu()
    {
        Fade.screenFade.FadeTo(1f, 0.5f);
        yield return new WaitForSecondsRealtime(0.6f);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    private void Update()
    {
        if (InputManager.escapeAction.WasPerformedThisFrame())
        {
            Debug.Log("Escape: " + Settings.active.settingsFade.GetAlphaChangeSpeed());
            if (Settings.active.settingsFade.GetAlphaChangeSpeed() > 0f)
            {
                //Settings open
                Settings.active.Close();
                return;
            }

            if (pauseMenuFade.GetAlphaChangeSpeed() <= 0f)
            {
                //Pause
                Fade.gameFade.FadeTo(0.9f, 0.3f);
                pauseMenuFade.FadeTo(1f, 0.2f, 0.1f);

                Cursor.lockState = CursorLockMode.None;

                Time.timeScale = 0f;
            }
            else
            { 
                //Resume
                Resume();
            }
        }
    }
    private void Resume()
    {
        Debug.Log("Resume");
        Fade.gameFade.FadeTo(0f, 0.3f);
        pauseMenuFade.FadeTo(0f, 0.3f);

        Cursor.lockState = CursorLockMode.Locked;

        Time.timeScale = 1f;
    }

}
