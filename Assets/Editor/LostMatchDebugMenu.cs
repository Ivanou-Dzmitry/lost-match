using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lost Match > Debug: quick changes of the test save (levels, coins, boosters, lives).
/// Works on the save of the Editor (Application.persistentDataPath).
/// In Play Mode the game data in memory is changed, so reopen the screen or the scene to see the result.
/// </summary>
public static class LostMatchDebugMenu
{
    private const string Root = "Lost Match/Debug/";

    // ───────────────────────── levels ─────────────────────────

    [MenuItem(Root + "Open all levels", priority = 100)]
    private static void OpenAllLevels()
    {
        Modify("All levels opened", data =>
        {
            EnsureSize(data);
            for (int i = 0; i < data.isActive.Length; i++) data.isActive[i] = true;
        });
    }

    [MenuItem(Root + "Open next 10 levels", priority = 101)]
    private static void OpenNextTen()
    {
        Modify("Next 10 levels opened", data =>
        {
            EnsureSize(data);
            int last = Array.FindLastIndex(data.isActive, a => a);
            for (int i = 0; i <= Mathf.Min(last + 10, data.isActive.Length - 1); i++) data.isActive[i] = true;
        });
    }

    [MenuItem(Root + "Close all levels (keep level 1)", priority = 102)]
    private static void CloseAllLevels()
    {
        Modify("All levels closed except level 1", data =>
        {
            EnsureSize(data);
            for (int i = 0; i < data.isActive.Length; i++) data.isActive[i] = i == 0;
            data.levelToLoad = 1;
        });
    }

    [MenuItem(Root + "3 stars on all opened levels", priority = 103)]
    private static void ThreeStars()
    {
        Modify("3 stars on opened levels", data =>
        {
            EnsureSize(data);
            for (int i = 0; i < data.stars.Length; i++)
                if (data.isActive[i]) data.stars[i] = 3;
        });
    }

    // ───────────────────────── coins, boosters, lives ─────────────────────────

    [MenuItem(Root + "Add 1000 coins", priority = 120)]
    private static void Add1000() => Modify("+1000 coins", data => data.credits += 1000);

    [MenuItem(Root + "Add 10000 coins", priority = 121)]
    private static void Add10000() => Modify("+10000 coins", data => data.credits += 10000);

    [MenuItem(Root + "Set coins to 0", priority = 122)]
    private static void ZeroCoins() => Modify("Coins set to 0", data => data.credits = 0);

    [MenuItem(Root + "Fill all boosters to max", priority = 123)]
    private static void FillBoosters()
    {
        Modify("Boosters filled", data =>
        {
            if (data.bonuses == null || data.maxBonusCount == null) return;
            for (int i = 0; i < data.bonuses.Length && i < data.maxBonusCount.Length; i++)
                if (data.maxBonusCount[i] > 0) data.bonuses[i] = Mathf.Max(data.bonuses[i], data.maxBonusCount[i]);
        });
    }

    [MenuItem(Root + "Refill lives", priority = 124)]
    private static void RefillLives()
    {
        Modify("Lives refilled", data =>
        {
            data.lifeRecoveryTime = "";
            if (data.bonuses != null && data.bonuses.Length > 5)
                data.bonuses[5] = Mathf.Max(data.bonuses[5], 5);
        });
    }

    // ───────────────────────── save file ─────────────────────────

    [MenuItem(Root + "Print save summary", priority = 140)]
    private static void PrintSummary()
    {
        Modify(null, data =>
        {
            int opened = data.isActive != null ? data.isActive.Count(a => a) : 0;
            int stars = data.stars != null ? data.stars.Sum() : 0;
            Debug.Log($"[LostMatch] Save: coins {data.credits}, opened levels {opened}, stars {stars}, name '{data.playerName}'");
        }, writeBack: false);
    }

    [MenuItem(Root + "Reset save (delete file)", priority = 141)]
    private static void ResetSave()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Lost Match", "Exit Play Mode first.", "OK");
            return;
        }

        string path = SavePath;
        if (!File.Exists(path))
        {
            Debug.Log("[LostMatch] No save file.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Lost Match", "Delete the test save? Progress, coins and boosters will be lost.", "Delete", "Cancel"))
            return;

        File.Delete(path);
        if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        Debug.Log("[LostMatch] Save deleted. A new one is created on the next Play.");
    }

    [MenuItem(Root + "Open save folder", priority = 142)]
    private static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(File.Exists(SavePath) ? SavePath : Application.persistentDataPath);
    }

    // ───────────────────────── helpers ─────────────────────────

    private static string SavePath => Path.Combine(Application.persistentDataPath, GameData.PLAYER_SAVES);

    private static int TotalLevels()
    {
        string[] guids = AssetDatabase.FindAssets("t:WorldManager");
        if (guids.Length == 0) return 0;
        var wm = AssetDatabase.LoadAssetAtPath<WorldManager>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return wm != null ? wm.GetTotalLevelsCount() : 0;
    }

    // the level arrays must have a slot for every level
    private static void EnsureSize(SaveData data)
    {
        int total = Mathf.Max(TotalLevels(), 1);
        if (data.isActive == null || data.isActive.Length < total) Array.Resize(ref data.isActive, total);
        if (data.stars == null || data.stars.Length < total) Array.Resize(ref data.stars, total);
        if (data.highScore == null || data.highScore.Length < total) Array.Resize(ref data.highScore, total);
        data.isActive[0] = true;
    }

    /// <param name="message">log text after the change, null = no log</param>
    private static void Modify(string message, Action<SaveData> change, bool writeBack = true)
    {
        // Play Mode: the game keeps the save in memory and writes it on exit, so change it there
        if (EditorApplication.isPlaying)
        {
            GameData gameData = UnityEngine.Object.FindFirstObjectByType<GameData>();
            if (gameData != null && gameData.saveData != null)
            {
                change(gameData.saveData);
                if (writeBack) gameData.SaveToFile();
                if (message != null) Debug.Log($"[LostMatch] {message} (Play Mode: reopen the screen to see it).");
                return;
            }
        }

        string path = SavePath;
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Lost Match",
                "No save file yet. Press Play once so that the game creates it, then run this command again.", "OK");
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        if (data == null)
        {
            Debug.LogError("[LostMatch] Cannot read the save file: " + path);
            return;
        }

        change(data);

        if (writeBack)
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            if (message != null) Debug.Log($"[LostMatch] {message}.");
        }
    }
}
