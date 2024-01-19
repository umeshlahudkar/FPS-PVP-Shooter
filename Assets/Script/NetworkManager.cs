using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public TextMeshProUGUI loadingText;

    [Header("Menu Buttons Screen")]
    public GameObject menuButtons;

    [Header("Create Room Screen")]
    public GameObject createRoomScreen;
    public TMP_InputField roomNameInput;

    [Header("Room Screen")]
    public GameObject roomScreen;
    public TextMeshProUGUI roomNameText;
    public PlayerDetail playerDetailsPrefab;
    public GameObject startGameButton;

    [Header("Error Screen")]
    public GameObject errorScreen;
    public TextMeshProUGUI errorMsgText;

    [Header("Room Browser Screen")]
    public GameObject roomBrowserScreen;
    public RoomDetail roomDetailPrefab;

    [Header("Username Input Screen")]
    public GameObject nameInputScreen;
    public TMP_InputField playerNameInputField;
    private static bool hasNameSet;

    public string levelToPlay;

    private List<RoomDetail> allRoomDetails = new List<RoomDetail>();
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private List<PlayerDetail> allPlayerDetails = new List<PlayerDetail>();


    private void Start()
    {
        CloseMenus();

        loadingScreen.SetActive(true);
        loadingText.text = "Connecting To Network...";

        if(!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        loadingText.text = "Joining Lobby...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);

        if (!hasNameSet)
        {
            CloseMenus();
            nameInputScreen.SetActive(true);

            if(PlayerPrefs.HasKey("playerName"))
            {
                playerNameInputField.text = PlayerPrefs.GetString("playerName");
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();

        roomScreen.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        ListAllPlayers();

        if(PhotonNetwork.IsMasterClient)
        {
            startGameButton.SetActive(true);
        }
        else
        {
            startGameButton.SetActive(false);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorMsgText.text = "Failed to create Room : " + message;
        CloseMenus();
        errorScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        PlayerDetail label = Instantiate(playerDetailsPrefab, playerDetailsPrefab.transform.parent);
        label.SetDetails(allPlayerDetails.Count + 1, newPlayer.NickName);
        allPlayerDetails.Add(label);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.SetActive(true);
        }
        else
        {
            startGameButton.SetActive(false);
        }
    }





    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];
            if (info.RemovedFromList || info.PlayerCount == info.MaxPlayers)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        ListAllRooms();
    }

    private void ListAllRooms()
    {
        if(!roomBrowserScreen.activeInHierarchy) { return; }
       
        foreach (RoomDetail bt in allRoomDetails)
        {
            Destroy(bt.gameObject);
        }
        allRoomDetails.Clear();

        int index = 1;

        foreach (var roomEntry in cachedRoomList)
        {
            string roomName = roomEntry.Key;
            RoomInfo roomInfo = roomEntry.Value;

            if (roomInfo.PlayerCount != roomInfo.MaxPlayers && !roomInfo.RemovedFromList)
            {
                RoomDetail button = Instantiate(roomDetailPrefab, roomDetailPrefab.transform.parent);
                button.SetDetails(index, roomName, roomInfo.PlayerCount, roomInfo.MaxPlayers, this);
                allRoomDetails.Add(button);
            }

            index++;
        }
    }

    private void ListAllPlayers()
    {
        foreach (PlayerDetail label in allPlayerDetails)
        {
            Destroy(label.gameObject);
        }
        allPlayerDetails.Clear();

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++)
        {
            PlayerDetail label = Instantiate(playerDetailsPrefab, playerDetailsPrefab.transform.parent);
            label.SetDetails(i+1, players[i].NickName);
            label.gameObject.SetActive(true);

            allPlayerDetails.Add(label);
        }
    }

    public void OnCreateRoomButtonClick()
    {
        createRoomScreen.SetActive(true);
    }

    public void OnFindRoomButtonClick()
    {
        CloseMenus();
        roomBrowserScreen.SetActive(true);
        ListAllRooms();
    }

    public void OnQuitButtonClick()
    {
        Application.Quit();
    }

    public void OnCloseRoomBrowserButtonClick()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    // button click
    public void CreateRoom()
    {
        if(!string.IsNullOrEmpty(roomNameInput.text))
        {
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput.text, options);

            CloseMenus();

            loadingText.text = "Creating Room...";
            loadingScreen.SetActive(true);
        }
    }

    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.text = "Leaving Room";
        loadingScreen.SetActive(true);
    }

    

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);

        CloseMenus();
        loadingText.text = "Joining Room";
        loadingScreen.SetActive(true);
    }

    public void SetNickName()
    {
        if(!string.IsNullOrEmpty(playerNameInputField.text))
        {
            PhotonNetwork.NickName = playerNameInputField.text;

            PlayerPrefs.SetString("playerName", playerNameInputField.text);

            CloseMenus();
            menuButtons.SetActive(true);

            hasNameSet = true;
        }
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(levelToPlay);
    }

    private void CloseMenus()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
    }
}
