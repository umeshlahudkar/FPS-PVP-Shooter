
using UnityEngine;
using TMPro;

public class PlayerDetail : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI srNoText;
    [SerializeField] private TextMeshProUGUI playerNameText;

    public void SetDetails(int index, string playerName)
    {
        srNoText.text = index.ToString();
        playerNameText.text = playerName;
    }
}
