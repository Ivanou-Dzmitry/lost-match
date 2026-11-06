using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

/// <summary>One row of the leaderboard.</summary>
public struct LeaderboardRow
{
    public int Rank;
    public string Name;
    public long Score;
    public bool IsLocalPlayer;
}

/// <summary>
/// Google Play Games Services: sign-in and the leaderboard.
/// Works only on an Android device. In the Editor and on other platforms every call
/// fails softly (not signed in, no scores), so the game still runs offline.
/// </summary>
public static class PlayServices
{
    public const string LeaderboardId = GPGSIds.leaderboard_lostmatch_total_score;
    public const string DefaultName = "Player";

    private static bool activated;
    private static Task<bool> pendingSignIn;

    public static bool IsSignedIn
    {
        get
        {
#if UNITY_ANDROID
            return !Application.isEditor && activated && PlayGamesPlatform.Instance.IsAuthenticated();
#else
            return false;
#endif
        }
    }

    /// <summary>Name from the Google account, or "Player" when not signed in.</summary>
    public static string PlayerName
    {
        get
        {
#if UNITY_ANDROID
            if (IsSignedIn)
            {
                string name = PlayGamesPlatform.Instance.localUser.userName;
                if (!string.IsNullOrEmpty(name))
                    return name;
            }
#endif
            return DefaultName;
        }
    }

    /// <summary>
    /// Signs in. interactive = false: the silent automatic sign-in.
    /// interactive = true: shows the Google sign-in dialog (use it from a button).
    /// </summary>
    public static Task<bool> SignInAsync(bool interactive = false)
    {
#if UNITY_ANDROID
        // The Editor has no Google Play services: run offline there
        if (Application.isEditor)
            return Task.FromResult(false);

        if (IsSignedIn)
            return Task.FromResult(true);

        if (!interactive && pendingSignIn != null && !pendingSignIn.IsCompleted)
            return pendingSignIn;

        if (!activated)
        {
            PlayGamesPlatform.Activate();
            activated = true;
        }

        var tcs = new TaskCompletionSource<bool>();
        Action<SignInStatus> onResult = status => tcs.TrySetResult(status == SignInStatus.Success);

        try
        {
            if (interactive)
                PlayGamesPlatform.Instance.ManuallyAuthenticate(onResult);
            else
                PlayGamesPlatform.Instance.Authenticate(onResult);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Play Games sign-in failed: " + e.Message);
            tcs.TrySetResult(false);
        }

        pendingSignIn = tcs.Task;
        return pendingSignIn;
#else
        return Task.FromResult(false);
#endif
    }

    /// <summary>Sends the score to the leaderboard. Google keeps only the best one.</summary>
    public static Task<bool> ReportScoreAsync(long score)
    {
#if UNITY_ANDROID
        if (!IsSignedIn)
            return Task.FromResult(false);

        var tcs = new TaskCompletionSource<bool>();
        try
        {
            PlayGamesPlatform.Instance.ReportScore(score, LeaderboardId, ok => tcs.TrySetResult(ok));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Play Games ReportScore failed: " + e.Message);
            tcs.TrySetResult(false);
        }
        return tcs.Task;
#else
        return Task.FromResult(false);
#endif
    }

    /// <summary>Opens the standard Google leaderboard screen.</summary>
    public static void ShowLeaderboardUI()
    {
#if UNITY_ANDROID
        if (IsSignedIn)
            PlayGamesPlatform.Instance.ShowLeaderboardUI(LeaderboardId);
#endif
    }

    /// <summary>Top players of all time (Google allows up to 25 rows per request).</summary>
    public static Task<List<LeaderboardRow>> LoadTopScoresAsync(int maxResults = 25)
    {
        var tcs = new TaskCompletionSource<List<LeaderboardRow>>();
        var rows = new List<LeaderboardRow>();

#if UNITY_ANDROID
        if (!IsSignedIn)
        {
            tcs.TrySetResult(rows);
            return tcs.Task;
        }

        try
        {
            PlayGamesPlatform.Instance.LoadScores(
                LeaderboardId,
                LeaderboardStart.TopScores,
                Mathf.Clamp(maxResults, 1, 25),
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.AllTime,
                data =>
                {
                    if (data == null || !data.Valid || data.Scores == null || data.Scores.Length == 0)
                    {
                        tcs.TrySetResult(rows);
                        return;
                    }

                    var scores = data.Scores;
                    string[] ids = new string[scores.Length];
                    for (int i = 0; i < scores.Length; i++)
                        ids[i] = scores[i].userID;

                    // Scores hold only user IDs, names come from a second request.
                    PlayGamesPlatform.Instance.LoadUsers(ids, profiles =>
                    {
                        var names = new Dictionary<string, string>();
                        if (profiles != null)
                        {
                            foreach (var p in profiles)
                                names[p.id] = p.userName;
                        }

                        string localId = PlayGamesPlatform.Instance.localUser.id;
                        for (int i = 0; i < scores.Length; i++)
                        {
                            names.TryGetValue(scores[i].userID, out string name);
                            rows.Add(new LeaderboardRow
                            {
                                Rank = scores[i].rank,
                                Name = string.IsNullOrEmpty(name) ? DefaultName : name,
                                Score = scores[i].value,
                                IsLocalPlayer = scores[i].userID == localId,
                            });
                        }
                        tcs.TrySetResult(rows);
                    });
                });
        }
        catch (Exception e)
        {
            Debug.LogWarning("Play Games LoadScores failed: " + e.Message);
            tcs.TrySetResult(rows);
        }
#else
        tcs.TrySetResult(rows);
#endif
        return tcs.Task;
    }
}
