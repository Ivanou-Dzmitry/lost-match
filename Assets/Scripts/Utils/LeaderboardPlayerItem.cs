using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;    

public class LeaderboardPlayerItem : MonoBehaviour
{
    private LeaderboardEntry player = null;
    [SerializeField] public TextMeshProUGUI rankText = null;
    [SerializeField] public TextMeshProUGUI nameText = null;
    [SerializeField] public TextMeshProUGUI scoreText = null;

    public void Inialize(LeaderboardEntry player)
    {
        this.player = player;
        rankText.text = (player.Rank+1).ToString();
        nameText.text = player.PlayerName;
        scoreText.text = player.Score.ToString();
    }
}
