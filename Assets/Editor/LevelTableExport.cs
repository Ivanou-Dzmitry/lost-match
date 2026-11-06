using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lost Match > Tools > Export Level Table (CSV): one row per level with the numbers
/// that matter for balance and a "pressure" estimate (moves needed for the goals / moves available).
/// The table is always made from the level assets, so it cannot get out of date.
/// </summary>
public static class LevelTableExport
{
    // ----- model constants: tune them after play tests -----
    private const float PiecesPerMove = 4.5f;      // pieces of all colours cleared by one move (with cascades)
    private const float ObstacleHitsPerMove = 2f;  // hits on obstacles by one move
    private const float LineBombMoves = 2f;        // moves to make one bomb
    private const float WrapBombMoves = 3f;
    private const float ColorBombMoves = 4f;

    // ----- target curve: the base rises from world to world, the same wave repeats inside every world -----
    private const float TargetBaseFirstWorld = 0.55f;
    private const float TargetBaseStep = 0.05f;
    private const float TargetBaseMax = 1.2f;
    // position in the world 1..10: easy start, a small peak at 5, the biggest peak at the world finale (10)
    private static readonly float[] WaveInWorld = { 0.70f, 0.85f, 1.00f, 0.80f, 1.20f, 0.75f, 0.95f, 1.05f, 0.85f, 1.35f };
    public const float ReviewDelta = 0.3f;        // |pressure - target| above this = "Review"
    public const float PeakFactor = 1.25f;        // Role: Peak if pressure > local median x this
    public const float BreatherFactor = 0.7f;     // Role: Breather if pressure < local median x this
    private const int NeighbourWindow = 5;         // levels on each side for the local median

    private static readonly string[] Separator = { ";" };

    [MenuItem("Lost Match/Tools/Export Level Table (CSV)", priority = 50)]
    public static void Export()
    {
        string[] guids = AssetDatabase.FindAssets("t:WorldManager");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Lost Match", "WorldManager asset not found.", "OK");
            return;
        }

        var worldManager = AssetDatabase.LoadAssetAtPath<WorldManager>(AssetDatabase.GUIDToAssetPath(guids[0]));

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string docs = Path.Combine(projectRoot, "docs");
        Directory.CreateDirectory(docs);

        string path = EditorUtility.SaveFilePanel("Export level table", docs, "level_table", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var sb = new StringBuilder();
        AppendRow(sb, "Level", "World", "Cols", "Rows", "Cells", "Colours", "Moves/Time", "Type", "Goals",
            "Obstacles", "Obstacle HP", "Empty", "Preload", "Stars 1", "Stars 2", "Stars 3", "Tag",
            "Pressure", "Rating", "Score per move", "Role", "Target", "Delta", "Check", "Notes");

        // 1st pass: the numbers of every level
        var numbers = new List<int>();
        var rows = new List<string[]>();
        var pressures = new List<float>();

        int total = worldManager.GetTotalLevelsCount();
        for (int n = 1; n <= total; n++)
        {
            Level level = worldManager.GetLevel(n, out World _);
            if (level == null) continue;

            rows.Add(BuildRow(n, level, out float pressure));
            numbers.Add(n);
            pressures.Add(pressure);
        }

        // 2nd pass: role in the wave and the distance to the target curve
        for (int i = 0; i < rows.Count; i++)
        {
            float p = pressures[i];
            float local = LocalMedian(pressures, i);
            float target = TargetPressure(numbers[i]);
            float delta = p - target;

            string role = p <= 0f ? "" : p > local * PeakFactor ? "Peak" : p < local * BreatherFactor ? "Breather" : "Normal";
            string check = p <= 0f ? "" : Mathf.Abs(delta) > ReviewDelta ? (delta > 0 ? "Review: harder" : "Review: easier") : "";

            string[] cells = rows[i];
            string notes = cells[cells.Length - 1];
            var all = cells.Take(cells.Length - 1).Concat(new[] { role, Num(target), Num(delta), check, notes });
            AppendRow(sb, all.ToArray());
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true)); // BOM: Excel reads UTF-8
        Debug.Log($"[LostMatch] Level table saved: {path}");
        EditorUtility.RevealInFinder(path);
    }

    private static string[] BuildRow(int n, Level level, out float pressureOut)
    {
        int colours = level.element != null ? level.element.Length : 0;
        int moves = level.endGameRequrimentsForLevel != null ? level.endGameRequrimentsForLevel.counterValue : 0;
        bool timed = level.endGameRequrimentsForLevel != null && level.endGameRequrimentsForLevel.gameType == GameType.Time;

        // objects on the board
        var count = new Dictionary<TileKind, int>();
        if (level.boardLayout != null)
            foreach (TileType t in level.boardLayout)
                count[t.tileKind] = count.TryGetValue(t.tileKind, out int c) ? c + 1 : 1;

        int Count(params TileKind[] kinds) => kinds.Sum(k => count.TryGetValue(k, out int c) ? c : 0);

        int empty = Count(TileKind.Empty);
        int area = PlayableCells(level);
        int cells = Math.Max(0, area - empty);

        int obstacleCount = Count(TileKind.Blocker01, TileKind.Blocker02, TileKind.Blocker03,
            TileKind.Breakable01, TileKind.Breakable02, TileKind.Breakable03,
            TileKind.Locked01, TileKind.Locked02, TileKind.Locked03,
            TileKind.Expanding01, TileKind.Expanding02, TileKind.Expanding03);

        int obstacleHp = Count(TileKind.Blocker01, TileKind.Breakable01, TileKind.Locked01, TileKind.Expanding01)
            + 2 * Count(TileKind.Blocker02, TileKind.Breakable02, TileKind.Locked02, TileKind.Expanding02)
            + 3 * Count(TileKind.Blocker03, TileKind.Breakable03, TileKind.Locked03, TileKind.Expanding03);

        // goals and the pressure
        var notes = new List<string>();
        var goalList = new List<(string match, int need)>();
        if (level.levelGoals != null)
            foreach (BlankGoalClass g in level.levelGoals)
                goalList.Add((g.matchValue, g.numberGoalsNeeded));

        var goalTexts = goalList.Select(g => $"{g.match} x{g.need}").ToList();
        var elementTags = (level.element ?? new GameObject[0]).Where(e => e != null).Select(e => e.tag).ToList();

        float pressure = EstimatePressure(new PressureInput
        {
            Colours = colours, Moves = moves, Goals = goalList, Count = count, ElementTags = elementTags,
        }, notes);

        if (level.preloadBoardLayout != null && level.preloadBoardLayout.Any(t => (int)t.tileKind >= colours))
            notes.Add("preload uses a missing element type");

        pressureOut = pressure;
        int[] stars = ReadStars(level);
        float scorePerMove = moves > 0 && stars.Length >= 3 ? stars[2] / (float)moves : 0f;

        return new[]
        {
            n.ToString(),
            (level.name.Length > 0 ? ((n - 1) / 10 + 1).ToString() : ""),
            level.columns.ToString(),
            level.rows.ToString(),
            cells.ToString(),
            colours.ToString(),
            moves.ToString(),
            timed ? "Time" : "Moves",
            string.Join(", ", goalTexts),
            obstacleCount.ToString(),
            obstacleHp.ToString(),
            empty.ToString(),
            (level.preloadBoardLayout != null ? level.preloadBoardLayout.Length : 0).ToString(),
            stars.Length > 0 ? stars[0].ToString() : "",
            stars.Length > 1 ? stars[1].ToString() : "",
            stars.Length > 2 ? stars[2].ToString() : "",
            level.levelDifficulty.ToString(),
            Num(pressure),
            Rate(pressure),
            Num(scorePerMove),
            string.Join(" | ", notes),
        };
    }

    /// <summary>What the pressure estimate needs. The Level Editor fills it from the level that is being painted.</summary>
    public struct PressureInput
    {
        public int Colours;
        public int Moves;
        public IList<(string match, int need)> Goals;
        public Dictionary<TileKind, int> Count;     // objects on the board by kind
        public IList<string> ElementTags;           // tags of the element prefabs of the level
    }

    /// <summary>The pressure of a saved level asset.</summary>
    public static float PressureOfAsset(Level level)
    {
        var count = new Dictionary<TileKind, int>();
        if (level.boardLayout != null)
            foreach (TileType t in level.boardLayout)
                count[t.tileKind] = count.TryGetValue(t.tileKind, out int c) ? c + 1 : 1;

        var goals = new List<(string match, int need)>();
        if (level.levelGoals != null)
            foreach (BlankGoalClass g in level.levelGoals)
                goals.Add((g.matchValue, g.numberGoalsNeeded));

        return EstimatePressure(new PressureInput
        {
            Colours = level.element != null ? level.element.Length : 0,
            Moves = level.endGameRequrimentsForLevel != null ? level.endGameRequrimentsForLevel.counterValue : 0,
            Goals = goals,
            Count = count,
            ElementTags = (level.element ?? new GameObject[0]).Where(e => e != null).Select(e => e.tag).ToList(),
        }, null);
    }

    /// <summary>Parts of the pressure, in moves needed.</summary>
    public struct PressureResult
    {
        public float Pressure;        // all moves needed / moves available
        public float ElementMoves;    // moves for the colour goals
        public float ObstacleMoves;   // moves for blockers, breakables, lockers
        public float BombMoves;       // moves to make the bombs
        public float Needed => ElementMoves + ObstacleMoves + BombMoves;
    }

    /// <summary>Estimated moves needed for the goals / moves available. notes gets the problems that were found.</summary>
    public static float EstimatePressure(PressureInput input, List<string> notes)
    {
        return EstimateDetailed(input, notes).Pressure;
    }

    /// <summary>The same estimate with the parts (colour goals, obstacle goals, bombs).</summary>
    public static PressureResult EstimateDetailed(PressureInput input, List<string> notes)
    {
        int Count(TileKind k) => input.Count != null && input.Count.TryGetValue(k, out int c) ? c : 0;

        float elementMoves = 0, obstacleMoves = 0, bombMoves = 0;

        if (input.Goals == null || input.Goals.Count == 0)
        {
            notes?.Add("no goals");
        }
        else
        {
            foreach ((string match, int need) in input.Goals)
            {
                if (string.IsNullOrEmpty(match)) continue;

                if (match.StartsWith("element_"))
                {
                    elementMoves = Mathf.Max(elementMoves, need * Mathf.Max(1, input.Colours) / PiecesPerMove);
                    if (input.ElementTags != null && !input.ElementTags.Contains(match))
                        notes?.Add($"goal {match} is not among the level elements");
                }
                else if (TryObstacleFamily(match, out TileKind[] family, out int[] hp))
                {
                    int available = 0, hitSum = 0;
                    for (int i = 0; i < family.Length; i++)
                    {
                        int c = Count(family[i]);
                        available += c;
                        hitSum += c * hp[i];
                    }

                    float avgHp = available > 0 ? hitSum / (float)available : 1f;
                    obstacleMoves += need * avgHp / ObstacleHitsPerMove;

                    if (available < need)
                        notes?.Add($"goal {match} x{need}: only {available} on the board");
                }
                else if (match == "LineBomb") bombMoves += need * LineBombMoves;
                else if (match == "WrapBomb") bombMoves += need * WrapBombMoves;
                else if (match == "ColorBomb") bombMoves += need * ColorBombMoves;
            }
        }

        if (input.Colours < 3) notes?.Add("less than 3 colours");

        return new PressureResult
        {
            Pressure = input.Moves > 0 ? (elementMoves + obstacleMoves + bombMoves) / input.Moves : 0f,
            ElementMoves = elementMoves,
            ObstacleMoves = obstacleMoves,
            BombMoves = bombMoves,
        };
    }

    // blockers 2 and 3 count as "blocker_01" in the goals (see SpecialElements), the same for breakables and lockers
    private static bool TryObstacleFamily(string match, out TileKind[] family, out int[] hp)
    {
        hp = new[] { 1, 2, 3 };
        switch (match)
        {
            case "blocker_01": family = new[] { TileKind.Blocker01, TileKind.Blocker02, TileKind.Blocker03 }; return true;
            case "breakable_01": family = new[] { TileKind.Breakable01, TileKind.Breakable02, TileKind.Breakable03 }; return true;
            case "locker_01": family = new[] { TileKind.Locked01, TileKind.Locked02, TileKind.Locked03 }; return true;
        }
        family = null;
        return false;
    }

    // cells of the background that can hold elements (ids 26..33 and 38, as in the old Tiled files)
    private static int PlayableCells(Level level)
    {
        if (level.xmlLayoutFile == null) return level.columns * level.rows;

        try
        {
            XElement data = XDocument.Parse(level.xmlLayoutFile.text).Element("data");
            if (data == null) return level.columns * level.rows;

            int area = 0;
            foreach (string v in data.Value.Split(','))
                if (int.TryParse(v.Trim(), out int id) && ((id >= 26 && id <= 33) || id == 38)) area++;
            return area;
        }
        catch (Exception)
        {
            return level.columns * level.rows;
        }
    }

    // the pressure the level should have by the target curve
    public static float TargetPressure(int level)
    {
        int world = (level - 1) / 10;           // 0-based
        int position = (level - 1) % 10;        // 0-based
        float baseValue = Mathf.Min(TargetBaseMax, TargetBaseFirstWorld + TargetBaseStep * world);
        return baseValue * WaveInWorld[position];
    }

    // median of the pressure of the neighbours (and the level itself): what is "normal" around this level
    public static float LocalMedian(IList<float> values, int index)
    {
        int from = Mathf.Max(0, index - NeighbourWindow);
        int to = Mathf.Min(values.Count - 1, index + NeighbourWindow);
        var window = values.Skip(from).Take(to - from + 1).Where(v => v > 0f).OrderBy(v => v).ToList();
        return window.Count == 0 ? 0f : window[window.Count / 2];
    }

    private static int[] ReadStars(Level level) => level.scoreGoals ?? new int[0];

    public static string Rate(float p)
    {
        if (p <= 0f) return "";
        if (p < 0.55f) return "Easy";
        if (p < 0.8f) return "Normal";
        if (p < 1.0f) return "Hard";
        return "Very hard";
    }

    // decimal comma: the spreadsheet in the Russian locale reads it as a number
    private static string Num(float v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

    private static void AppendRow(StringBuilder sb, params string[] cells)
    {
        sb.Append(string.Join(Separator[0], cells.Select(Escape)));
        sb.Append("\r\n");
    }

    private static string Escape(string s)
    {
        if (s == null) return "";
        return s.IndexOfAny(new[] { ';', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }
}
