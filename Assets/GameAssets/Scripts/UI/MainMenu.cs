using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Fade mainMenuFade;

    [SerializeField]
    private Button singleplayerButton;
    [SerializeField]
    private Button settingsButton;
    [SerializeField]
    private Button quitButton;
    [SerializeField]
    private string clickSoundString = "Click";
    [SerializeField]
    private AudioSource musicAudioSource;

    [Header("Parents")]
    [SerializeField] 
    private GameObject mainMenuParent;
    [SerializeField] 
    private GameObject lobbyListParent;
    [SerializeField] 
    private GameObject lobbyParent;
    [Header("LobbyList")]
    [SerializeField] 
    private GameObject lobbyListVerticalGroupParent;
    [SerializeField] 
    private GameObject lobbyPrefab;
    [Header("Lobby")]
    [SerializeField] 
    private GameObject lobbyPlayerVerticalGroupParent;
    [SerializeField] 
    private GameObject playerPrefab;
    [SerializeField] 
    private Button createGameButton;

    private enum OpenMenu
    {
        Main,
        LobbyList,
        Lobby
    }
    private OpenMenu currentOpenMenu = OpenMenu.Main;
    
    private bool gameStarting = false;
    private float musicStartVolume;
    [SerializeField]
    private float fadeTime;
    private float timer;

    private bool CanNotPressButtons()
    {
        return gameStarting || mainMenuFade.GetAlphaChangeSpeed() < 0f || PopUp.active.IsAPopUpActive();
    }
    private void Start()
    {
        musicStartVolume = musicAudioSource.volume;
        
        settingsButton.onClick.AddListener(() =>
        {
            if (CanNotPressButtons()) return;
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

        createGameButton.onClick.AddListener(() =>
        {
            if (CanNotPressButtons()) return;
            PopUp.active.LoadingNotification("Starting Game...");
            MultiplayerManager.active.StartGame();
        });
        
        Settings.active.onClose.AddListener(() =>
        {
            mainMenuFade.FadeTo(1f, 0.5f);
        });
        mainMenuFade.SetAlpha(0f);
        mainMenuFade.FadeTo(1f, 1f, 2f);

        UpdateCoroutine();
        MultiplayerManager.onLobbyUpdate.AddListener(OnLobbyUpdate);

    }
    private async Task UpdateCoroutine()
    {
        while (true)
        {

            if (currentOpenMenu == OpenMenu.LobbyList)
            {
                UpdateLobbyList();
                await Task.Delay(2000);
            }
            await Task.Delay(100);
        }
    }

    private async void OnLobbyUpdate(ILobbyChanges changes)
    {
        Debug.Log("Updating Lobby");
        if (currentOpenMenu == OpenMenu.Lobby)
        {
            if (changes.Data.Changed)
            {
                if (changes.Data.Value["joinCode"].Value.Value != "")
                {
                    await MultiplayerManager.active.JoinHostGame(changes.Data.Value["joinCode"].Value.Value,changes.Data.Value["scene"].Value.Value);
                }
            }

            if (changes.PlayerData.Changed || changes.PlayerLeft.Changed || changes.PlayerJoined.Changed)
            {
                UpdateLobby();
            }
        }
    }

    public void SinglePlayer()
    {
        if (CanNotPressButtons()) return;
        SoundManager.active.Play(clickSoundString);
        gameStarting = true;
        timer = 0f;
        Fade.screenFade.FadeTo(1f, fadeTime);
    }
    public void OpenMain()
    {
        if (CanNotPressButtons()) return;
        SoundManager.active.Play(clickSoundString);
        SetOpenMenu(OpenMenu.Main);
    }
    public void OpenLobbyList()
    {
        if (CanNotPressButtons()) return;
        SoundManager.active.Play(clickSoundString);
        SetOpenMenu(OpenMenu.LobbyList);
    }
    private void OpenLobby()
    {
        if (CanNotPressButtons()) return;
        SoundManager.active.Play(clickSoundString);
        SetOpenMenu(OpenMenu.Lobby);
    }
    public async void CreateLobby()
    {
        string lobbyName = await PopUp.active.TextInput("Enter Lobby Name");
        PopUp.active.LoadingNotification("Creating Lobby...");
        await MultiplayerManager.active.CreateLobby(lobbyName);
        await SetOpenMenu(OpenMenu.Lobby);
        PopUp.active.DestroyCurrentPopUp();
        createGameButton.gameObject.SetActive(true);
    }

    private async void UpdateLobbyList()
    {
        List<Lobby> lobbyList = await MultiplayerManager.active.GetLobbyList();

        foreach (Transform child in lobbyListVerticalGroupParent.GetComponentsInChildren<Transform>())
        {
            if(child.gameObject != lobbyListVerticalGroupParent)
                Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            GameObject clone = Instantiate(lobbyPrefab, lobbyListVerticalGroupParent.transform);
            clone.transform.Find("Name").GetComponent<TMP_Text>().text = lobby.Name;
            clone.transform.Find("PlayerCount").GetComponent<TMP_Text>().text = lobby.Players.Count +"/" + lobby.MaxPlayers;
            Lobby l = lobby;
            clone.transform.Find("JoinButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                JoinLobby(l);
            });
        }
        
    }

    private async void JoinLobby(Lobby lobby)
    {
        createGameButton.gameObject.SetActive(false);
        Lobby joinedLobby = await MultiplayerManager.active.JoinLobbyByID(lobby.Id);
        if (joinedLobby != null)
        {
            SetOpenMenu(OpenMenu.Lobby);
        }
        else
        {
            PopUp.active.Notification("Could not Join Lobby");
        }
    }
    private async void UpdateLobby()
    {
        Lobby connectedLobby = await MultiplayerManager.active.GetConnectedLobby();
        if (connectedLobby == null)
        {
            SetOpenMenu(OpenMenu.LobbyList);
            PopUp.active.Notification("Lobby closed");
            return;
        }

        lobbyParent.transform.Find("Title").GetComponent<TMP_Text>().text = connectedLobby.Name + " " + connectedLobby.Players.Count + "/" + connectedLobby.MaxPlayers;
        UpdateLobbyPlayers(connectedLobby.Players);
    }

    private void UpdateLobbyPlayers(List<Unity.Services.Lobbies.Models.Player> playerList)
    {
        foreach (Transform child in lobbyPlayerVerticalGroupParent.GetComponentsInChildren<Transform>())
        {
            if(child.gameObject != lobbyPlayerVerticalGroupParent)
                Destroy(child.gameObject);
        }

        foreach (var player in playerList)
        {
            GameObject clone = Instantiate(playerPrefab, lobbyPlayerVerticalGroupParent.transform);
            clone.transform.Find("Name").GetComponent<TMP_Text>().text = player.Data["username"].Value;
        }
    }

    private async Task SetOpenMenu(OpenMenu openMenu)
    {
        if (currentOpenMenu == OpenMenu.Lobby && openMenu != OpenMenu.Lobby)
        {
            await MultiplayerManager.active.LeaveLobby();
        }
        
        mainMenuParent.SetActive(openMenu == OpenMenu.Main);
        lobbyListParent.SetActive(openMenu == OpenMenu.LobbyList);
        lobbyParent.SetActive(openMenu == OpenMenu.Lobby);
        
        if (openMenu == OpenMenu.LobbyList)
        {
            if (MultiplayerManager.username == "")
            {
                MultiplayerManager.username = await PopUp.active.TextInput("Enter Username");
                PopUp.active.LoadingNotification("Authenticating...");
                await MultiplayerManager.active.Authenticate();
                PopUp.active.DestroyCurrentPopUp();
            }
            UpdateLobbyList();
        }
        if (openMenu == OpenMenu.Lobby)
        {
            UpdateLobby();
        }
        currentOpenMenu = openMenu;
    }
    private void Update()
    {
        if (gameStarting)
        {
            timer += Time.deltaTime;

            musicAudioSource.volume = Mathf.Lerp(musicStartVolume, 0, timer / fadeTime);

            if (timer >= fadeTime)
            {
                gameStarting = false;
                timer = 0f;
                MultiplayerManager.active.StartSinglePlayer();
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
