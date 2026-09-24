using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerScript : NetworkBehaviour
{
    public NetworkManager networkManager;

    private readonly NetworkVariable<int> hostTeam = new(-1);
    private readonly NetworkVariable<int> guestTeam = new(-1);
    private readonly NetworkVariable<bool> guestConnected = new(false);
    private readonly NetworkVariable<ulong> guestClientId = new(ulong.MaxValue);

    private TMP_Text azadPlayersText;
    private TMP_Text kamoPlayersText;
    private TMP_Text statusText;
    private Button azadTeamButton;
    private Button kamoTeamButton;
    private Button startButton;
    private bool uiBound;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        BindLobbyUi();
    }

    private void Start()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        BindLobbyUi();

        if (networkManager == null || networkManager.IsListening)
            return;

        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            if (SceneManagerScript.isHost)
                transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            else
                transport.SetConnectionData(SceneManagerScript.joinAddress, 7777);
        }

        if (SceneManagerScript.isHost)
            networkManager.StartHost();
        else
            networkManager.StartClient();
    }

    public override void OnNetworkSpawn()
    {
        hostTeam.OnValueChanged += OnTeamChanged;
        guestTeam.OnValueChanged += OnTeamChanged;
        guestConnected.OnValueChanged += OnGuestConnectionChanged;

        if (IsServer)
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            foreach (ulong clientId in networkManager.ConnectedClientsIds)
            {
                if (clientId != NetworkManager.ServerClientId)
                {
                    guestClientId.Value = clientId;
                    guestConnected.Value = true;
                    break;
                }
            }
        }

        UpdateLobbyUi();
    }

    public override void OnNetworkDespawn()
    {
        hostTeam.OnValueChanged -= OnTeamChanged;
        guestTeam.OnValueChanged -= OnTeamChanged;
        guestConnected.OnValueChanged -= OnGuestConnectionChanged;

        if (networkManager != null && IsServer)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void BindLobbyUi()
    {
        if (uiBound)
            return;

        azadTeamButton = BindButton("Joind Team Azad", ChooseAzad);
        kamoTeamButton = BindButton("Joind Team Kamo", ChooseKamo);
        if (azadTeamButton != null)
            azadTeamButton.interactable = false;
        if (kamoTeamButton != null)
            kamoTeamButton.interactable = false;

        GameObject readyObject = GameObject.Find("Ready?");
        if (readyObject != null)
        {
            startButton = readyObject.GetComponent<Button>();
            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
                startButton.interactable = false;
            }
        }

        azadPlayersText = FindText("Team Azad Spieler Liste");
        kamoPlayersText = FindText("Team Kamo Spieler Liste");
        statusText = FindText("Warning MSG");
        uiBound = true;
    }

    private Button BindButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        if (button != null)
            button.onClick.AddListener(action);
        else
            Debug.LogWarning($"Lobby button '{objectName}' was not found.");
        return button;
    }

    private static TMP_Text FindText(string objectName)
    {
        GameObject textObject = GameObject.Find(objectName);
        return textObject != null ? textObject.GetComponent<TMP_Text>() : null;
    }

    public void ChooseAzad() => RequestTeam(TeamManagerScript.Team.Azad);
    public void ChooseKamo() => RequestTeam(TeamManagerScript.Team.Kamo);

    private void RequestTeam(TeamManagerScript.Team team)
    {
        if (networkManager == null || !networkManager.IsListening || !IsSpawned)
        {
            Debug.LogWarning("Team selection is waiting for the lobby network object to spawn.");
            return;
        }

        if (IsServer)
            SetTeamForClient(NetworkManager.ServerClientId, (int)team);
        else
            ChooseTeamServerRpc((int)team);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChooseTeamServerRpc(int requestedTeam, ServerRpcParams rpcParams = default)
    {
        if (requestedTeam != (int)TeamManagerScript.Team.Azad && requestedTeam != (int)TeamManagerScript.Team.Kamo)
            return;

        SetTeamForClient(rpcParams.Receive.SenderClientId, requestedTeam);
    }

    private void SetTeamForClient(ulong clientId, int requestedTeam)
    {
        if (clientId != NetworkManager.ServerClientId && !guestConnected.Value)
            return;

        int otherTeam = clientId == NetworkManager.ServerClientId ? guestTeam.Value : hostTeam.Value;
        if (otherTeam == requestedTeam)
        {
            if (statusText != null)
                statusText.text = "Dieses Team ist bereits belegt.";
            return;
        }

        if (clientId == NetworkManager.ServerClientId)
            hostTeam.Value = requestedTeam;
        else
            guestTeam.Value = requestedTeam;

        TeamManagerScript.SetTeamForClient(clientId, (TeamManagerScript.Team)requestedTeam);
        UpdateLobbyUi();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId != NetworkManager.ServerClientId)
        {
            if (guestConnected.Value && guestClientId.Value != clientId)
            {
                networkManager.DisconnectClient(clientId);
                return;
            }

            guestClientId.Value = clientId;
            guestConnected.Value = true;
        }
        UpdateLobbyUi();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
            return;

        guestConnected.Value = false;
        guestClientId.Value = ulong.MaxValue;
        guestTeam.Value = -1;
        TeamManagerScript.RemoveClientTeam(clientId);
        UpdateLobbyUi();
    }

    private void OnTeamChanged(int previousValue, int newValue) => UpdateLobbyUi();
    private void OnGuestConnectionChanged(bool previousValue, bool newValue) => UpdateLobbyUi();

    private void UpdateLobbyUi()
    {
        if (!uiBound)
            BindLobbyUi();

        TeamManagerScript.SetTeamForClient(NetworkManager.ServerClientId, ToTeam(hostTeam.Value));
        if (guestClientId.Value != ulong.MaxValue)
            TeamManagerScript.SetTeamForClient(guestClientId.Value, ToTeam(guestTeam.Value));

        if (azadPlayersText != null)
            azadPlayersText.text = FormatTeam(TeamManagerScript.Team.Azad);
        if (kamoPlayersText != null)
            kamoPlayersText.text = FormatTeam(TeamManagerScript.Team.Kamo);

        bool canChooseTeam = IsSpawned && networkManager != null && networkManager.IsListening && IsClient;
        if (azadTeamButton != null)
            azadTeamButton.interactable = canChooseTeam;
        if (kamoTeamButton != null)
            kamoTeamButton.interactable = canChooseTeam;
        if (startButton != null)
            startButton.interactable = IsSpawned && IsServer && CanStartGame();

        TeamManagerScript.LocalTeam = IsClient
            ? ToTeam(networkManager.LocalClientId == NetworkManager.ServerClientId ? hostTeam.Value : guestTeam.Value)
            : TeamManagerScript.Team.None;
    }

    private string FormatTeam(TeamManagerScript.Team team)
    {
        string host = (TeamManagerScript.Team)hostTeam.Value == team ? "Host" : string.Empty;
        string guest = guestConnected.Value && (TeamManagerScript.Team)guestTeam.Value == team ? "Gast" : string.Empty;
        if (host.Length > 0 && guest.Length > 0)
            return $"{host}\n{guest}";
        if (host.Length > 0 || guest.Length > 0)
            return host.Length > 0 ? host : guest;
        return "Noch niemand";
    }

    private static TeamManagerScript.Team ToTeam(int value)
    {
        return value == (int)TeamManagerScript.Team.Azad ? TeamManagerScript.Team.Azad :
               value == (int)TeamManagerScript.Team.Kamo ? TeamManagerScript.Team.Kamo :
               TeamManagerScript.Team.None;
    }

    private bool CanStartGame()
    {
        return guestConnected.Value && hostTeam.Value >= 0 && guestTeam.Value >= 0 && hostTeam.Value != guestTeam.Value;
    }

    public void StartGame()
    {
        if (!IsServer || !CanStartGame())
            return;

        NetworkManager.SceneManager.LoadScene("InGameScene", LoadSceneMode.Single);
    }
}
