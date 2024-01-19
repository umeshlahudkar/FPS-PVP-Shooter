using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public enum EventCodes : byte
    {
        NewPlayer = 0,
        ListPlayer,
        UpdateStats,
        NextMatch,
        TimerSync
    }

    private List<PlayerInfo> allPlayersInfo = new List<PlayerInfo>();
    private int index;

    private float matchTime = 180f;
    private float currentMatchTime;
    private float sendTimer = 0.0166f;

    private float waitAfterEnding = 5f;

    public GameState gameState = GameState.Waiting;
    [SerializeField] private Transform mapCameraPoint;
    [SerializeField] private PlayerSpawner playerSpawner;

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            NewPlayerSendEvent(PhotonNetwork.NickName);
            gameState = GameState.Playing;
            SetUpTimer();
        }

        if(!PhotonNetwork.IsMasterClient)
        {
            UIController.Instance.ToggleTimerText(false);
        }
    }

    private void Update()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            if (matchTime > 0 && gameState == GameState.Playing)
            {
                currentMatchTime -= Time.deltaTime;

                if (currentMatchTime <= 0)
                {
                    currentMatchTime = 0f;

                    gameState = GameState.Ending;

                    ListPlayersSendEvent();
                }

                UIController.Instance.UpdateTimerDisplay(currentMatchTime);

                sendTimer -= Time.deltaTime;

                if(sendTimer <= 0)
                {
                    sendTimer += 1f;
                    TimerSendEvent();
                }
            }
        }
       
    }

    public void OnEvent(EventData photonEvent)
    {
       if(photonEvent.Code < 200)
       {
            EventCodes code = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            switch(code)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceiveEvent(data);
                    break;

                case EventCodes.ListPlayer:
                    ListPlayersReceiveEvent(data);
                    break;

                case EventCodes.UpdateStats:
                    UpdateStatsSReceiveEvent(data);
                    break;

                case EventCodes.NextMatch:
                    NextMatchReceiveEvent();
                    break;

                case EventCodes.TimerSync:
                    TimerReceiveEvent(data);
                    break;
            }
       }
    }


    public void NewPlayerSendEvent(string userName)
    {
        object[] package = new object[4];
        package[0] = userName;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient},
            new SendOptions { Reliability = true}
            );
    }

    public void NewPlayerReceiveEvent(object[] data)
    {
        PlayerInfo info = new PlayerInfo((string)data[0], (int)data[1], (int)data[2], (int)data[3]);

        allPlayersInfo.Add(info);

        ListPlayersSendEvent();
    }

    public void ListPlayersSendEvent()
    {
        object[] package = new object[allPlayersInfo.Count + 1];
        package[0] = gameState;

        for(int i = 0; i < allPlayersInfo.Count; i++)
        {
            object[] piece = new object[4];
            piece[0] = allPlayersInfo[i].name;
            piece[1] = allPlayersInfo[i].actor;
            piece[2] = allPlayersInfo[i].kills;
            piece[3] = allPlayersInfo[i].deaths;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
           (byte)EventCodes.ListPlayer,
           package,
           new RaiseEventOptions { Receivers = ReceiverGroup.All },
           new SendOptions { Reliability = true }
           );
    }

    public void ListPlayersReceiveEvent(object[] data)
    {
        allPlayersInfo.Clear();

        gameState = (GameState)data[0];

        for(int i = 1; i < data.Length; i++)
        {
            object[] piece = (object[])data[i];

            PlayerInfo player = new PlayerInfo(
                (string)piece[0], (int)piece[1], (int)piece[2], (int)piece[3]
                );

            allPlayersInfo.Add(player);

            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i - 1;
            }
        }

       StateCheck();
    }

    public void UpdateStatsSendEvent(int actorSending, int statsToUpdate, int amount)
    {
        object[] package = new object[] { actorSending, statsToUpdate, amount };

        PhotonNetwork.RaiseEvent(
          (byte)EventCodes.UpdateStats,
          package,
          new RaiseEventOptions { Receivers = ReceiverGroup.All },
          new SendOptions { Reliability = true }
          );
    }

    public void UpdateStatsSReceiveEvent(object[] data)
    {
        int actor = (int)data[0];
        int statType = (int)data[1];
        int amount = (int)data[2];

        for(int i = 0; i < allPlayersInfo.Count; i++)
        {
            if(allPlayersInfo[i].actor == actor)
            {
                switch(statType)
                {
                    case 0: //kills
                        allPlayersInfo[i].kills += amount;
                        break;

                    case 1: //Deaths
                        allPlayersInfo[i].deaths += amount;
                        break;
                }

                if(i == index)
                {
                    UIController.Instance.UpdateStatsDisplay(allPlayersInfo[i].deaths, allPlayersInfo[i].kills);
                }

                if (UIController.Instance.IsLeaderBoardActive())
                {
                    UIController.Instance.ShowLeaderBoard();
                }

                break;
            }
        }
         //ScoreCheck();
    }

    /*
    public void ScoreCheck()
    {
        bool winnerFound = false;

        foreach (PlayerInfo player in allPlayersInfo)
        {
            if (killsToWin > 0 && player.kills >= killsToWin)
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && gameState != GameState.Ending)
            {
                gameState = GameState.Ending;
                ListPlayersSendEvent();
            }
        }
    }
    */

    public void StateCheck()
    {
        if (gameState == GameState.Ending)
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        gameState = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        Camera.main.transform.SetPositionAndRotation(mapCameraPoint.position, mapCameraPoint.rotation);

        UIController.Instance.ToggleGameOverScreen(true);
        UIController.Instance.ShowLeaderBoard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(EndCo());
    }

    private IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitAfterEnding);

        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        SceneManager.LoadScene(0);
    }

    public List<PlayerInfo> GetAllPlayer() { return allPlayersInfo; }

    public void NextMatchSendEvent()
    {
        PhotonNetwork.RaiseEvent(
           (byte)EventCodes.NextMatch,
           null,
           new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
           new SendOptions { Reliability = true }
           );
    }

    public void NextMatchReceiveEvent()
    {
        gameState = GameState.Playing;

        UIController.Instance.ToggleGameOverScreen(false);
        UIController.Instance.ToggleLeaderBoardScreen(false);

        foreach (PlayerInfo player in allPlayersInfo)
        {
            player.kills = 0;
            player.deaths = 0;
        }

        UIController.Instance.UpdateStatsDisplay(allPlayersInfo[index].deaths, allPlayersInfo[index].kills);

        playerSpawner.SpawnPlayer();
        SetUpTimer();
    }

    public void SetUpTimer()
    {
        if(matchTime > 0)
        {
            currentMatchTime = matchTime;
            UIController.Instance.UpdateTimerDisplay(currentMatchTime);
        }
    }

    public void TimerSendEvent()
    {
        object[] package = new object[] { (int)currentMatchTime, gameState };

        PhotonNetwork.RaiseEvent(
           (byte)EventCodes.TimerSync,
           package,
           new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
           new SendOptions { Reliability = true }
           );
    }

    public void TimerReceiveEvent(object[] data)
    {
        currentMatchTime = (int)data[0];
        gameState = (GameState)data[1];

        UIController.Instance.UpdateTimerDisplay(currentMatchTime);
        UIController.Instance.ToggleTimerText(true);
    }

    public Transform GetMapCameraPoint() { return mapCameraPoint; }


    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor;
    public int kills;
    public int deaths;

    public PlayerInfo(string _name, int _actor, int _kills, int _deaths)
    {
        name = _name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }
}

public enum GameState
{
    Waiting,
    Playing,
    Pause,
    Ending
}