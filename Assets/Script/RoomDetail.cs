
using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class RoomDetail : MonoBehaviour
{
    private string roomName;

    [SerializeField] private TextMeshProUGUI srNoText;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI maxPlayerCountText;
    [SerializeField] private Button joinButton;

    private NetworkManager networkManager;

    public void SetDetails(int _index, string _roomName, int _playerCount, int _maxPlayer, NetworkManager _networkManager)
    {
        roomName = _roomName;
        srNoText.text = _index.ToString();
        roomNameText.text = _roomName;
        playerCountText.text = _playerCount.ToString();
        maxPlayerCountText.text = _maxPlayer.ToString();
        joinButton.gameObject.SetActive(true);

        networkManager = _networkManager;
    }

    public void OnJoinButtonClick()
    {
        networkManager.JoinRoom(roomName);
    }
}
