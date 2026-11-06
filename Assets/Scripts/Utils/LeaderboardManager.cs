using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Leaderboard screen. Google Play Games gives the top 25 players in one request,
/// the pages are made on the client.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    private const int MaxRows = 25;

    [SerializeField] private int playersPerPage = 5; 
    [SerializeField] private LeaderboardPlayerItem playerItemPrefab = null; 
    [SerializeField] private RectTransform playersContainer = null; 
    [SerializeField] public TextMeshProUGUI pageText = null; 
    [SerializeField] private Button nextButton = null; 
    [SerializeField] private Button prevButton = null; 

    private readonly List<LeaderboardRow> rows = new List<LeaderboardRow>();

    private int currentPage = 1;
    private int totalPages = 0;

    private async void Start()
    {
        ClearPlayersList();

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (prevButton != null)
            prevButton.onClick.AddListener(PrevPage);

        await PlayServices.SignInAsync();
        await SyncLocalScore();
        await Reload();
    }

    /// <summary>
    /// Sends the total of the saved best scores. Players who already have progress
    /// (it was made before Google Play Games or without a new record) get on the board too.
    /// Google keeps the best value, so sending it again is safe.
    /// </summary>
    private async Task SyncLocalScore()
    {
        if (!PlayServices.IsSignedIn)
            return;

        GameObject gameDataObject = GameObject.FindWithTag("GameData");
        GameData gameData = gameDataObject != null ? gameDataObject.GetComponent<GameData>() : null;

        if (gameData == null || gameData.saveData == null || gameData.saveData.highScore == null)
            return;

        long total = 0;
        foreach (int score in gameData.saveData.highScore)
            total += score;

        if (total > 0 && await PlayServices.ReportScoreAsync(total))
        {
            // the server needs a moment before the new score shows in the list
            await Task.Delay(1500);
        }
    }

    /// <summary>Opens the Google leaderboard screen (can be wired to a button).</summary>
    public void ShowGoogleLeaderboard()
    {
        PlayServices.ShowLeaderboardUI();
    }

    /// <summary>Sends a new score and refreshes the list.</summary>
    public async Task AddScoreLeaderboard(int score)
    {
        if (await PlayServices.ReportScoreAsync(score))
        {
            await Reload();
        }
    }

    private async Task Reload()
    {
        List<LeaderboardRow> loaded = await PlayServices.LoadTopScoresAsync(MaxRows);

        // the screen can be closed while the request is running
        if (this == null)
            return;

        rows.Clear();
        rows.AddRange(loaded);

        totalPages = Mathf.Max(1, Mathf.CeilToInt(rows.Count / (float)playersPerPage));
        ShowPage(1);
    }

    private void ShowPage(int page)
    {
        currentPage = Mathf.Clamp(page, 1, totalPages);

        ClearPlayersList();

        int first = (currentPage - 1) * playersPerPage;
        int last = Mathf.Min(first + playersPerPage, rows.Count);

        for (int i = first; i < last; i++)
        {
            LeaderboardPlayerItem playerItem = Instantiate(playerItemPrefab, playersContainer);
            playerItem.Inialize(rows[i]);

            // highlight the player
            if (rows[i].IsLocalPlayer)
            {
                playerItem.nameText.fontStyle = FontStyles.Bold;
                playerItem.nameText.color = new Color32(0x5F, 0x77, 0x91, 0xFF);
            }
        }

        if (pageText != null)
            pageText.text = rows.Count > 0 ? $"{currentPage}/{totalPages}" : "-";

        if (nextButton != null)
            nextButton.interactable = currentPage < totalPages;

        if (prevButton != null)
            prevButton.interactable = currentPage > 1;
    }

    private void NextPage()
    {
        if (currentPage < totalPages)
        {
            ShowPage(currentPage + 1);
        }
    }

    private void PrevPage()
    {
        if (currentPage > 1)
        {
            ShowPage(currentPage - 1);
        }
    }

    public void ClearPlayersList()
    {
        LeaderboardPlayerItem[] playersItems = playersContainer.GetComponentsInChildren<LeaderboardPlayerItem>();

        if (playersItems != null && playersItems.Length > 0)
        {
            foreach (LeaderboardPlayerItem item in playersItems)
            {
                Destroy(item.gameObject);
            }
        }
    }
}
