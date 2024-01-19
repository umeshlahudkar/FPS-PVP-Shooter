using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class UIController : Singleton<UIController>
{
    [SerializeField] private TextMeshProUGUI overHeatedText;
    [SerializeField] private Slider weaponHeatSlider;

    [SerializeField] private TextMeshProUGUI deathMsgText;
    [SerializeField] private GameObject deathScreen;

    [SerializeField] private Slider healthSlider;

    [SerializeField] private TextMeshProUGUI deathText;
    [SerializeField] private TextMeshProUGUI killText;

    [SerializeField] private GameObject leaderBoardScreen;
    [SerializeField] private LeaderboardPlayer playerInfo;

    [SerializeField] private GameObject gameOverScreen;

    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private GameObject pauseScreen;

    [SerializeField] private MatchManager matchManager;

    private List<LeaderboardPlayer> leaderBoardPlayers = new List<LeaderboardPlayer>();

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            ShowHidePauseScreen();
        }

        if(pauseScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        if (Input.GetKeyDown(KeyCode.Tab) && matchManager.gameState != GameState.Ending)
        {
            if (leaderBoardScreen.activeInHierarchy)
            {
               leaderBoardScreen.SetActive(false);
            }
            else
            {
                ShowLeaderBoard();
            }
        }
    }

    public void ShowLeaderBoard()
    {
        ToggleLeaderBoardScreen(true);

        foreach (LeaderboardPlayer pl in leaderBoardPlayers)
        {
            Destroy(pl.gameObject);
        }
        leaderBoardPlayers.Clear();

        List<PlayerInfo> sorted = SortPlayers(matchManager.GetAllPlayer());

        for (int i = 0; i < sorted.Count; i++)
        {
            LeaderboardPlayer info = Instantiate(playerInfo, playerInfo.transform.parent);
            info.SetDetails(i + 1, sorted[i].name, sorted[i].kills, sorted[i].deaths);

            leaderBoardPlayers.Add(info);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach (PlayerInfo pl in players)
            {
                if (!sorted.Contains(pl))
                {
                    if (pl.kills > highest)
                    {
                        highest = pl.kills;
                        selectedPlayer = pl;
                    }
                }
            }

            sorted.Add(selectedPlayer);
        }

        return sorted;
    }

    public void UpdateStatsDisplay(int death, int kill)
    {
        deathText.text = "Death : " + death.ToString();
        killText.text = "Kill : " + kill.ToString();
    }

    public void UpdateTimerDisplay(float currentMatchTime)
    {
        var displayTime = System.TimeSpan.FromSeconds(currentMatchTime);
        timerText.text = displayTime.Minutes.ToString("00") + ":" + displayTime.Seconds.ToString("00");
    }

    public void ToggleDeathScreen(bool status, string damager = "")
    {
        if(status == true)
        {
            deathMsgText.text = "You were Killed by " + damager;
        }
        deathScreen.SetActive(status);
    }

    public void ToggleTimerText(bool status)
    {
        timerText.gameObject.SetActive(status);
    }

    public void ToggleGameOverScreen(bool status)
    {
        gameOverScreen.SetActive(status);
    }

    public void ToggleLeaderBoardScreen(bool status)
    {
        leaderBoardScreen.SetActive(status);
    }

    public void ToggleOverheatedText(bool status)
    {
        overHeatedText.gameObject.SetActive(status);
    }

    public bool IsLeaderBoardActive()
    {
        return leaderBoardScreen.activeInHierarchy;
    }

    public void UpdateHealthSliderValue(int currentHealth)
    {
        healthSlider.value = currentHealth;
    }

    public void SetMaxHealthSliderValue(int maxValue)
    {
        healthSlider.minValue = 0;
        healthSlider.maxValue = maxValue;
    }

    public void UpdateWeaponHeatSliderValue(float temp)
    {
        weaponHeatSlider.value = temp;
    }

    public void SetMaxWeaponTempSliderValue(float maxValue)
    {
        weaponHeatSlider.minValue = 0;
        weaponHeatSlider.maxValue = maxValue;
    }

    public void ShowHidePauseScreen()
    {
        if(pauseScreen.activeInHierarchy)
        {
            pauseScreen.SetActive(false);
            matchManager.gameState = GameState.Playing;
        }
        else
        {
            pauseScreen.SetActive(true);
            matchManager.gameState = GameState.Pause;
        }
    }

    public void ReturnToMainMenu()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
