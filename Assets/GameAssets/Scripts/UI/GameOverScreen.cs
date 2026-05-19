using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen active;
    public static UnityEvent onGameOver;
    private static bool gameOver;
    public static bool IsGameOver()
    {
        return gameOver;
    }
    [Header("Monitor")]
    [SerializeField]
    private Camera monitorRenderCamera;
    [SerializeField]
    private MonitorArm monitor;
    [Header("Buttons")]
    [SerializeField]
    private Button retryButton;
    [SerializeField]
    private Button quitButton;
    [Header("Refrences")]
    [SerializeField]
    private GameObject mainParent;
    [Header("Fade")]
    [SerializeField]
    private Fade gameFade;
    [SerializeField]
    private Fade mainFade;
    [SerializeField]
    private Fade buttonFade;
    [Header("Sound")]
    [SerializeField]
    private string clickSoundString = "Click";

    private float timmer = 1f;
    private bool ignoreInput = false;
    private void Awake()
    {
        onGameOver = new UnityEvent();
        gameOver = false;
    }
    private void Start()
    {
        active = this;
        monitorRenderCamera.enabled = false;
        mainParent.SetActive(false);

        retryButton.onClick.AddListener(() =>
        {
            if (ignoreInput) return;
            SoundManager.active.Play(clickSoundString);
            ignoreInput = true;
            StartCoroutine(LoadScene("Game"));
        });
        quitButton.onClick.AddListener(() =>
        {
            if (ignoreInput) return;
            SoundManager.active.Play(clickSoundString);
            ignoreInput = true;
            StartCoroutine(LoadScene("MainMenu"));
        });
    }
    private void Update()
    {
        if (gameOver == false) return;

        timmer += Time.unscaledDeltaTime;
        Time.timeScale = Mathf.Clamp(1f - (timmer - 4f) * 0.2f, 0.1f, 1f);
    }
    public void StartGameOver()
    {
        StartCoroutine(GameOverCoroutine());
        onGameOver.Invoke();
    }
    private IEnumerator GameOverCoroutine()
    {
        gameOver = true;

        monitorRenderCamera.enabled = true;
        mainParent.SetActive(true);

        gameFade.SetAlpha(0f);
        mainFade.SetAlpha(0f);
        buttonFade.SetAlpha(0f);

        gameFade.FadeTo(0.8f, 4f, 1f);
        mainFade.FadeTo(1f, 4f, 1f);
        buttonFade.FadeTo(1f, 3f, 6f);

        yield return new WaitForSecondsRealtime(4f);
        monitor.EnableMonitor(false);
        yield return new WaitForSecondsRealtime(1f);
        monitor.printer.AddNotification(PaperRenderer.active.RenderPaper("GameOver", new List<Override>() { new Override("Statistics", OverrideType.StatisticList) }), float.MaxValue);
    }
    private IEnumerator LoadScene(string scene)
    {
        Fade.gameFade.FadeTo(1f, 1.2f);
        yield return new WaitForSecondsRealtime(1.3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }
}
