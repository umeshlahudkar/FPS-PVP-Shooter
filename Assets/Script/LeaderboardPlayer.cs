
using UnityEngine;
using TMPro;

public class LeaderboardPlayer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI srNoText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI killText;
    [SerializeField] private TextMeshProUGUI deathText;

    public void SetDetails(int _srNo, string _name, int _kills, int _deaths)
    {
        srNoText.text = _srNo.ToString();
        nameText.text = _name;
        killText.text = _kills.ToString();
        deathText.text = _deaths.ToString();
    }
}
