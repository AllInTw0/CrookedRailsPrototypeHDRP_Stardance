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
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager active = null;
    public static string username = "";
    public static UnityEvent<ILobbyChanges> onLobbyUpdate = new UnityEvent<ILobbyChanges>();
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
        if(active == this)
            active = null;
    }
    LobbyEventCallbacks lobbyCallbacks = new LobbyEventCallbacks();
    
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
        
        lobbyCallbacks.LobbyChanged += OnLobbyChanged;
        
    }

    public async void StartSinglePlayer()
    {
        await SceneManager.LoadSceneAsync("Game");
        NetworkManager.Singleton.StartHost();
    }
    private void OnLobbyChanged(ILobbyChanges changes)
    {
        onLobbyUpdate.Invoke(changes);
    }
    
    private string playerID;
    
    public async Task Authenticate()
    {
        InitializationOptions options = new InitializationOptions();

        options.SetProfile(Application.isEditor ? "editor" : "default");//With this it will probably only work with 2 instances. (Editor and build)
        
        await UnityServices.InitializeAsync(options);
        
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerID = AuthenticationService.Instance.PlayerId;
        
        Debug.Log("Authentication Successful. PlayerID: " + playerID);
    }
    
    //Lobby
    private string joinedLobbyID;
    private bool isHost;
    public async Task<Lobby> GetConnectedLobby()
    {
        if (string.IsNullOrEmpty(joinedLobbyID))
            return null;

        int tries = 5;
        while (tries > 0)
        {
            try
            {
                return await LobbyService.Instance.GetLobbyAsync(joinedLobbyID);
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldnt get lobby: " + e);
            }

            Task.Delay(1000);
            tries--;
        }

        return null;
    }
    public async void CreateOrJoinLobby()
    {
        await Authenticate();
        
        Lobby lobby = await QuickJoinLobby() ?? await CreateLobby();
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
            joinedLobbyID = lobby.Id;
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("Failed joining lobby: " + e.Message);
            return null;
        }
    }

    public async Task<List<Lobby>> GetLobbyList()
    {
        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
        return response.Results;
    }

    public async Task<Lobby> JoinLobbyByID(string lobbyID)
    {
        try
        {
            //Try to join lobby
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions();
            options.Player = new Unity.Services.Lobbies.Models.Player
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {
                        "username", new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member, value: username)
                    }
                }
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID,options);
            joinedLobbyID = lobby.Id;
            isHost = false;
            
            Task.Delay(1000);
            
            ILobbyEvents lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID,lobbyCallbacks);
            
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("Failed joining lobby: " + e.Message);
            return null;
        }
    }

    public async Task LeaveLobby()
    {
        if (isHost)
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobbyID);
        }
        else
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobbyID, AuthenticationService.Instance.PlayerId);
            ILobbyEvents lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(joinedLobbyID, lobbyCallbacks);
            await lobbyEvents.UnsubscribeAsync();
        }

        joinedLobbyID = "";
        StopCoroutine(nameof(HeartbeatLobbyCoroutine));
    }
    [Header("Lobby Creation")] 
    [SerializeField] private bool manuallySetRegion = true;
    [SerializeField] private string region = "europe-north1";//Finland https://docs.unity.com/en-us/relay/locations-and-regions
    const int maxPlayers = 10;
    public async Task<Lobby> CreateLobby(string lobbyName = "Lobby Name")
    {
        try
        {
            
            
            Unity.Services.Lobbies.Models.Player player = new Unity.Services.Lobbies.Models.Player
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {"username", new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member, value: username)}
                }
            };
            
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>()
                {
                    {"joinCode", new DataObject(DataObject.VisibilityOptions.Public, "")},
                    {"scene", new DataObject(DataObject.VisibilityOptions.Public, SceneManager.GetActiveScene().name)},
                },
                IsPrivate = false,
                Player = player
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15f));

            joinedLobbyID = lobby.Id;
            isHost = true;
            
            Task.Delay(1000);
            
            ILobbyEvents lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(joinedLobbyID,lobbyCallbacks);
            
            return lobby;
        }
        catch(Exception e)
        {
            Debug.Log("Failed creating lobby: " + e.Message);
            return null;
        }
    }

    public async Task StartGame()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            await SceneManager.LoadSceneAsync("Game");
        }

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers, manuallySetRegion ? region : null);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
        UpdateLobbyOptions options = new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>()
            {
                {"joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode)},
                {"scene", new DataObject(DataObject.VisibilityOptions.Public, SceneManager.GetActiveScene().name)},
            }
        };
        await LobbyService.Instance.UpdateLobbyAsync(joinedLobbyID, options);

        FindFirstObjectByType<UnityTransport>().SetHostRelayData(allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key,
            allocation.ConnectionData);

        NetworkManager.Singleton.StartHost();
    }

    public async Task JoinHostGame(string joinCode, string scene)
    {
        if (SceneManager.GetActiveScene().name != scene)
        {
            await SceneManager.LoadSceneAsync(scene);
        }
            
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
        FindFirstObjectByType<UnityTransport>().SetClientRelayData(joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key,
            joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

        NetworkManager.Singleton.StartClient();
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
