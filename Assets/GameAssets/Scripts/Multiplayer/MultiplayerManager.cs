using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager active = null;
    private void Awake()
    {
        if(active != null)
            Destroy(gameObject);
        else
        {
            active = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("Multiplayer Manager initialized");
        }
    }
    private void OnDestroy()
    {
        active = null;
    }
    
    [Header("Local Testing")]
    [SerializeField] private bool hostLocally = true;
    [SerializeField] private string hostLocalTargetScene = "Testing";
    [SerializeField] private bool editorIsClient = false;
    private async void Start()
    {
        if (hostLocally)
        {
            if (Application.isEditor)
            {
                if(editorIsClient)
                    NetworkManager.Singleton.StartClient();
                else
                    NetworkManager.Singleton.StartHost();
            }
            else
            {
                await SceneManager.LoadSceneAsync(hostLocalTargetScene);
                if(editorIsClient)
                    NetworkManager.Singleton.StartHost();
                else
                    NetworkManager.Singleton.StartClient();
            }
            return;
        }
        
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "MainMenu")
        {
            await Authenticate();
            CreateLobby();
        }
    }
    
    private string playerID;
    
    private async Task Authenticate()
    {
        InitializationOptions options = new InitializationOptions();

        options.SetProfile(Application.isEditor ? "editor" : "default");//With this it will probably only work with 2 instances. (Editor and build)
        
        await UnityServices.InitializeAsync(options);
        
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerID = AuthenticationService.Instance.PlayerId;
        
        Debug.Log("Authentication Successful. PlayerID: " + playerID);
    }
    
    //Lobby
    private Lobby connectedLobby;
    public async void CreateOrJoinLobby()
    {
        await Authenticate();
        
        connectedLobby = await QuickJoinLobby() ?? await CreateLobby();
    }

    private async Task<Lobby> QuickJoinLobby()
    {
        try
        {
            //Try to join lobby
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            if (SceneManager.GetActiveScene().name != lobby.Data["scene"].Value)
            {
                await SceneManager.LoadSceneAsync(lobby.Data["scene"].Value);
            }
            
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(lobby.Data["joinCode"].Value);
            
            FindFirstObjectByType<UnityTransport>().SetClientRelayData(joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key,
                joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("Failed joining lobby: " + e.Message);
            return null;
        }
    }

    [Header("Lobby Creation")] 
    [SerializeField] private bool manuallySetRegion = true;
    [SerializeField] private string region = "europe-north1";//Finland https://docs.unity.com/en-us/relay/locations-and-regions
    private async Task<Lobby> CreateLobby()
    {
        try
        {
            const int maxPlayers = 10;
            
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                await SceneManager.LoadSceneAsync("Game");
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers, manuallySetRegion ? region : null);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>()
                {
                    {"joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode)},
                    {"scene", new DataObject(DataObject.VisibilityOptions.Public, SceneManager.GetActiveScene().name)},
                },
                IsPrivate = false
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby Name", maxPlayers, options);

            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15f));

            FindFirstObjectByType<UnityTransport>().SetHostRelayData(allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key,
                allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();
            return lobby;
        }
        catch(Exception e)
        {
            Debug.Log("Failed creating lobby: " + e.Message);
            return null;
        }
    }

    private static IEnumerator HeartbeatLobbyCoroutine(string lobbyID, float delay)
    {
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyID);
            yield return new WaitForSecondsRealtime(delay);
        }
    }
}
