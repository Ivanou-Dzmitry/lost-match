using UnityEngine;
using TMPro;    

public class LeaderboardPlayerItem : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI rankText = null;
    [SerializeField] public TextMeshProUGUI nameText = null;
    [SerializeField] public TextMeshProUGUI scoreText = null;

    public void Inialize(LeaderboardRow row)
    {
        rankText.text = row.Rank.ToString();
        nameText.text = row.Name;
        scoreText.text = row.Score.ToString();
    }
}
