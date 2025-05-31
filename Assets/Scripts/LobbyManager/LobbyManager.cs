using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance { get; private set; }

    [Header("Menu Elements")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button quitButton;

    [Header("Lobby Elements")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Transform roomEntryContent;
    [SerializeField] private RoomEntry roomEntryPrefab;
    [SerializeField] private Button createButton;
    [SerializeField] private Button disconnectButton;

    [Header("Room Elements")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private Transform playerEntryTransform;
    [SerializeField] private PlayerEntry playerEntryPrefab;
    [SerializeField] private Button leaveButton;


    private readonly List<RoomEntry> roomEntryList = new();
    private readonly List<PlayerEntry> playerEntryList = new();

    private float timeBetweenUpdates = 1.5f;
    private float nextUpdateTime;


    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!ValidateElements()) return;
        AssignButtonListeners();
        ShowMenu();
    }


    private bool ValidateElements()
    {
        if (lobbyPanel == null)
        {
            Debug.LogError("Some panels are not assigned in the Inspector!");
            return false;
        }

        if (usernameInput == null)
        {
            Debug.LogError("Some elements are not assigned in the Inspector!");
            return false;
        }

        if (connectButton == null)
        {
            Debug.LogError("Some buttons are not assigned in the Inspector!");
            return false;
        }

        return true;
    }

    private void AssignButtonListeners()
    {
        connectButton.onClick.AddListener(OnClickConnect);
        quitButton.onClick.AddListener(OnClickQuit);
        createButton.onClick.AddListener(OnClickCreate);
        leaveButton.onClick.AddListener(OnClickLeave);
        disconnectButton.onClick.AddListener(OnClickDisconnect);
    }

    public override void OnConnectedToMaster()
    {
        ShowLobby();
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedRoom()
    {
        ShowRoom();
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        UpdatePlayerList();

        ReadyButtonController.Instance.ResetReadyButton();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateRoomList(roomList);
            nextUpdateTime = Time.time + timeBetweenUpdates;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnLeftRoom()
    {
        ShowLobby();
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        ShowMenu();
    }

    public void JoinRoom(string room)
    {
        PhotonNetwork.JoinRoom(room);
    }


    private void UpdateRoomList(List<RoomInfo> list)
    {
        roomEntryList.ForEach(entry =>
        {
            Destroy(entry.gameObject);
        });

        roomEntryList.Clear();

        list.ForEach(room =>
        {
            if (room.PlayerCount <= 0)
            {
                return;
            }

            RoomEntry newRoom = Instantiate(roomEntryPrefab, roomEntryContent);
            newRoom.Setup(room);
            roomEntryList.Add(newRoom);
        });
    }

    private void UpdatePlayerList()
    {
        playerEntryList.ForEach(entry =>
        {
            if (entry != null && entry.gameObject != null)
            {
                PhotonNetwork.Destroy(entry.gameObject);
            }
        });

        playerEntryList.Clear();

        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        foreach (KeyValuePair<int, Player> player in PhotonNetwork.CurrentRoom.Players)
        {
            GameObject newPlayerObject = PhotonNetwork.Instantiate("PlayerEntryPrefab", playerEntryTransform.position, Quaternion.identity);
            newPlayerObject.transform.SetParent(playerEntryTransform, false);
            PlayerEntry newPlayer = newPlayerObject.GetComponent<PlayerEntry>();

            newPlayer.Setup(player.Value);
            playerEntryList.Add(newPlayer);
        }
    }

    private void OnClickConnect()
    {
        if (usernameInput.text.Length >= 1)
        {
            PhotonNetwork.NickName = usernameInput.text;
            connectButton.GetComponentInChildren<TMP_Text>().text = "CONNECTING...";
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void OnClickDisconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            disconnectButton.GetComponentInChildren<TMP_Text>().text = "Disconnecting...";
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.Disconnect();
        }
    }

    private void OnClickCreate()
    {
        if (roomNameInput.text.Length >= 1)
        {
            RoomOptions roomOptions = new()
            {
                MaxPlayers = 2,
                EmptyRoomTtl = 0, // Room is immediately removed once all players leave
                CleanupCacheOnLeave = true
            };

            PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
        }
    }

    private void OnClickLeave()
    {
        PhotonNetwork.LeaveRoom();
    }


    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }

    private void ShowMenu()
    {
        DeactivatePanels();
        menuPanel.SetActive(true);
        usernameInput.text = "";
        connectButton.GetComponentInChildren<TMP_Text>().text = "CONNECT";

    }

    private void ShowLobby()
    {
        DeactivatePanels();
        lobbyPanel.SetActive(true);
        roomNameInput.text = "";
        disconnectButton.GetComponentInChildren<TMP_Text>().text = "Disconnect";
    }

    private void ShowRoom()
    {
        DeactivatePanels();
        roomPanel.SetActive(true);
    }

    private void DeactivatePanels()
    {
        menuPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
    }
}