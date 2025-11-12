using System;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;  
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LeaderboardManager : MonoBehaviour
{
    private GameData gameDataClass;

    [SerializeField] private int playersPerPage = 5; 
    [SerializeField] private LeaderboardPlayerItem playerItemPrefab = null; 
    [SerializeField] private RectTransform playersContainer = null; 
    [SerializeField] public TextMeshProUGUI pageText = null; 
    [SerializeField] private Button nextButton = null; 
    [SerializeField] private Button prevButton = null; 

    private int currentPage = 1;
    private int totalPages = 0;

    private bool isInitialized = false;

    private async void Start()
    {
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        await InitializeServices();
        await LoadPlayers(1); // Load first page after initialization
    }

    private async Task InitializeServices()
    {
        ClearPlayersList();

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (prevButton != null)
            prevButton.onClick.AddListener(PrevPage);

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            isInitialized = true;
            //Debug.Log("Unity Services initialized successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Services initialization failed: {e.Message}");
        }
    }

    public async Task AddScoreLeaderboard(int score)
    {
        try
        {
            //important set ID of your leaderboard
            var playerEntry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId: "LMLBoard", score);

            await LoadPlayers(currentPage); // await the async method properly
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message);
        }
    }

    private async Task LoadPlayers(int page)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Services not initialized yet!");
            return;
        }

        try
        {
            nextButton.interactable = false;
            prevButton.interactable = false;

            GetScoresOptions options = new GetScoresOptions
            {
                Offset = (page - 1) * playersPerPage,
                Limit = playersPerPage
            };

            var score = await LeaderboardsService.Instance.GetScoresAsync("LMLBoard", options);

            ClearPlayersList();

            for (int i = 0; i < score.Results.Count; i++)
            {
                LeaderboardPlayerItem playerItem = Instantiate(playerItemPrefab, playersContainer);
                playerItem.Inialize(score.Results[i]);
            }

            totalPages = Mathf.CeilToInt((float)score.Total / (float)score.Limit);
            currentPage = page;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }

        pageText.text = $"{currentPage}/{totalPages}";
        nextButton.interactable = currentPage < totalPages && totalPages > 1;
        prevButton.interactable = currentPage > 1 && totalPages > 1;
    }

    private async void NextPage()
    {
        // Go forward unless we're already on the last page
        if (currentPage < totalPages)
        {
            await LoadPlayers(currentPage + 1);
        }
        else
        {
            Debug.Log("Already on the last page.");
        }
    }

    private async void PrevPage()
    {
        // Go back unless we're already on the first page
        if (currentPage > 1)
        {
            await LoadPlayers(currentPage - 1);
        }
        else
        {
            Debug.Log("Already on the first page.");
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
