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

    // ----- recommendations: numbers from the play log (LM_GameSessionLog.csv, won games, about 120 rows) -----
    private const float BlockerHitsPerMoveLog = 1.8f;     // hits on obstacles per move that players really make (median of wins 1.9, losses pull it down)
    private const float BreakableHitsPerMoveLog = 1.5f;   // median of wins 1.6
    private const float LockerHitsPerMoveLog = 1.1f;      // median of wins 1.1: ice is the slowest to clear
    private const float ExpandableHitsPerMoveLog = 1.2f;  // no level in the log has this goal: a guess, slower than breakables because the blocks grow
    private const float EasyMoveUse = 0.65f;              // share of the moves a player uses to win: the less, the easier
    private const float NormalMoveUse = 0.85f;            // what the log shows for wins
    private const float HardMoveUse = 0.95f;
    // score of a won game, from the log: about 8.8 points per available move (the middle half of wins: 6.4 to 10),
    // less on a small board (under 45 playable cells about 5 per move)
    private const float TypicalScorePerMove = 8.8f;
    private const float LowScorePerMove = 6.4f;
    private const float HighScorePerMove = 10f;
    private const float FullBoardCells = 55f;             // from this many cells the board no longer lowers the score
    // the stars: 1 for nearly every win, 2 for about 3 wins of 4, 3 for about half (as the stars of the 100 levels: 3.9 / 6.2 / 8.8 per move)
    private const float Star1PerMove = 4f;
    private const float Star2PerMove = 6f;
    private const float Star3PerMove = 9f;

    // ----- shape of the board (GDD: voids, protrusions, dividers and small boards make a level harder) -----
    private const float VoidWeight = 0.8f;         // x share of empty cells inside the bounding box of the board
    private const float DeadCellWeight = 0.8f;     // x share of cells that are in no row or column of 3 (protrusions)
    private const float NeckWeight = 0.15f;        // per neck (a row or column with 2 cells or less) or separate part
    private const int MaxNecks = 4;
    private const float SmallBoardWeight = 0.03f;  // per column or row less than the reference size
    private const int ReferenceSize = 7;

    // ----- target curve: the wave of levels 1-10 (made after Candy Crush) repeats in every world, the base grows -----
    private const float TargetBaseFirstWorld = 1.08f;   // median pressure of levels 1-10 (with the shape of the board)
    private const float TargetBaseGrowth = 0.04f;       // the base grows by 4% of the first world per world
    private const float TargetBaseMax = 1.6f;
    // pressure of levels 1-10 divided by their median: the shape of the wave, by position in the world
    private static readonly float[] WaveInWorld = { 0.74f, 0.44f, 0.58f, 0.52f, 1.56f, 1.34f, 1.55f, 0.87f, 1.13f, 1.29f };
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
            "Pressure", "Shape", "Rating", "Score per move", "Role", "Target", "Delta", "Check", "Notes");

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

        PressureResult estimate = EstimateDetailed(new PressureInput
        {
            Colours = colours, Moves = moves, Goals = goalList, Count = count, ElementTags = elementTags,
            Playable = PlayableMask(level), Columns = level.columns, Rows = level.rows,
        }, notes);
        float pressure = estimate.Pressure;

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
            Num(estimate.Shape.Multiplier),
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
        public bool[] Playable;                     // cells that can hold elements, top row first (null = no shape)
        public int Columns;
        public int Rows;
    }

    /// <summary>The shape of the playable area and how much harder it makes the level.</summary>
    public struct ShapeResult
    {
        public float Multiplier;   // 1 = an open rectangle
        public int Playable;       // cells that can hold elements
        public int Width;          // bounding box of the playable cells
        public int Height;
        public float VoidShare;    // empty cells inside the bounding box
        public int DeadCells;      // cells that are in no row or column of 3, so they cannot start a match by themselves
        public int Necks;          // rows or columns with 2 cells or less, and separate parts
    }

    /// <summary>Voids, protrusions, dividers and the size of the board. Cells are stored top row first.</summary>
    public static ShapeResult Shape(bool[] playable, int cols, int rows)
    {
        var result = new ShapeResult { Multiplier = 1f };
        if (playable == null || cols <= 0 || rows <= 0 || playable.Length != cols * rows) return result;

        bool Cell(int r, int c) => r >= 0 && r < rows && c >= 0 && c < cols && playable[r * cols + c];

        int count = playable.Count(v => v);
        if (count == 0) return result;

        int minR = rows, maxR = -1, minC = cols, maxC = -1;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (Cell(r, c)) { minR = Math.Min(minR, r); maxR = Math.Max(maxR, r); minC = Math.Min(minC, c); maxC = Math.Max(maxC, c); }

        int width = maxC - minC + 1, height = maxR - minR + 1;
        float voidShare = 1f - count / (float)(width * height);

        int dead = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!Cell(r, c)) continue;
                int h = 1, v = 1;
                for (int x = c - 1; Cell(r, x); x--) h++;
                for (int x = c + 1; Cell(r, x); x++) h++;
                for (int y = r - 1; Cell(y, c); y--) v++;
                for (int y = r + 1; Cell(y, c); y++) v++;
                if (h < 3 && v < 3) dead++;
            }
        }

        int necks = 0;
        for (int r = minR + 1; r < maxR; r++)
        {
            int n = 0;
            for (int c = 0; c < cols; c++) if (Cell(r, c)) n++;
            if (n <= 2) necks++;
        }
        for (int c = minC + 1; c < maxC; c++)
        {
            int n = 0;
            for (int r = 0; r < rows; r++) if (Cell(r, c)) n++;
            if (n <= 2) necks++;
        }
        necks += CountParts(playable, cols, rows) - 1;
        necks = Math.Min(necks, MaxNecks);

        float multiplier = 1f + VoidWeight * voidShare + DeadCellWeight * dead / count + NeckWeight * necks
            + SmallBoardWeight * (Math.Max(0, ReferenceSize - width) + Math.Max(0, ReferenceSize - height));

        return new ShapeResult
        {
            Multiplier = multiplier, Playable = count, Width = width, Height = height,
            VoidShare = voidShare, DeadCells = dead, Necks = necks,
        };
    }

    // separate parts of the playable area (cells that touch by a side are one part)
    private static int CountParts(bool[] playable, int cols, int rows)
    {
        var seen = new bool[playable.Length];
        var stack = new Stack<int>();
        int parts = 0;
        for (int i = 0; i < playable.Length; i++)
        {
            if (!playable[i] || seen[i]) continue;
            parts++;
            seen[i] = true;
            stack.Push(i);
            while (stack.Count > 0)
            {
                int cell = stack.Pop();
                int r = cell / cols, c = cell % cols;
                void Visit(int nr, int nc)
                {
                    if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) return;
                    int ni = nr * cols + nc;
                    if (!playable[ni] || seen[ni]) return;
                    seen[ni] = true;
                    stack.Push(ni);
                }
                Visit(r - 1, c); Visit(r + 1, c); Visit(r, c - 1); Visit(r, c + 1);
            }
        }
        return parts;
    }

    /// <summary>Cells of the level that can hold elements: background present and not an Empty object. Top row first.</summary>
    public static bool[] PlayableMask(Level level)
    {
        int cols = level.columns, rows = level.rows;
        var mask = Enumerable.Repeat(true, cols * rows).ToArray();

        try
        {
            XElement data = level.xmlLayoutFile != null ? XDocument.Parse(level.xmlLayoutFile.text).Element("data") : null;
            if (data != null)
            {
                string[] values = data.Value.Split(',');
                if (values.Length == cols * rows)
                    for (int i = 0; i < values.Length; i++)
                        mask[i] = int.TryParse(values[i].Trim(), out int id) && ((id >= 26 && id <= 33) || id == 38);
            }
        }
        catch (Exception)
        {
            // no readable background: the whole rectangle
        }

        if (level.boardLayout != null)
            foreach (TileType t in level.boardLayout)
                if (t.tileKind == TileKind.Empty && t.columnX >= 0 && t.columnX < cols && t.rowY >= 0 && t.rowY < rows)
                    mask[(rows - 1 - t.rowY) * cols + t.columnX] = false;

        return mask;
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
            Playable = PlayableMask(level), Columns = level.columns, Rows = level.rows,
        }, null);
    }

    /// <summary>Moves and star scores to start from, made from the play log. This is a first guess, not a rule.</summary>
    public struct Recommendation
    {
        public float NeededMoves;   // moves a typical player needs, the shape of the board included
        public int EasyMoves;       // a winner uses about 65% of the moves
        public int NormalMoves;     // about 85%, as in the log
        public int HardMoves;       // about 95%
        public int HitsOnObstacles; // hits that the obstacle goals need
    }

    /// <summary>What a win scores for this number of moves and board, and the star thresholds to start from.</summary>
    public struct ScoreGuide
    {
        public int Low;       // a quarter of the wins score less
        public int Typical;   // the middle
        public int High;      // a quarter of the wins score more
        public int Star1;
        public int Star2;
        public int Star3;
    }

    public static ScoreGuide ScoreFor(int moves, int playableCells)
    {
        float board = playableCells > 0 ? Mathf.Clamp(playableCells / FullBoardCells, 0.6f, 1f) : 1f;
        int Round5(float v) => Mathf.Max(5, Mathf.RoundToInt(v / 5f) * 5);

        return new ScoreGuide
        {
            Low = Round5(moves * LowScorePerMove * board),
            Typical = Round5(moves * TypicalScorePerMove * board),
            High = Round5(moves * HighScorePerMove * board),
            Star1 = Round5(moves * Star1PerMove * board),
            Star2 = Round5(moves * Star2PerMove * board),
            Star3 = Round5(moves * Star3PerMove * board),
        };
    }

    private static float HitsPerMoveFromLog(string match)
    {
        switch (match)
        {
            case "blocker_01": return BlockerHitsPerMoveLog;
            case "breakable_01": return BreakableHitsPerMoveLog;
            case "expandable_01": return ExpandableHitsPerMoveLog;
            default: return LockerHitsPerMoveLog;
        }
    }

    /// <summary>Moves and the stars for the goals and the board of the level.</summary>
    public static Recommendation Recommend(PressureInput input)
    {
        PressureResult model = EstimateDetailed(input, null);   // colours and bombs, and the shape

        int Count(TileKind k) => input.Count != null && input.Count.TryGetValue(k, out int c) ? c : 0;

        float obstacleMoves = 0f, hits = 0f;
        if (input.Goals != null)
        {
            foreach ((string match, int need) in input.Goals)
            {
                if (!TryObstacleFamily(match, out TileKind[] family, out int[] hp)) continue;

                int available = 0, hitSum = 0;
                for (int i = 0; i < family.Length; i++)
                {
                    available += Count(family[i]);
                    hitSum += Count(family[i]) * hp[i];
                }

                float goalHits = need * (available > 0 ? hitSum / (float)available : 1f);
                hits += goalHits;
                obstacleMoves += goalHits / HitsPerMoveFromLog(match);
            }
        }

        float needed = (model.ElementMoves + obstacleMoves + model.BombMoves) * model.Shape.Multiplier;
        int MovesFor(float use) => Mathf.Max(1, Mathf.CeilToInt(needed / use));

        return new Recommendation
        {
            NeededMoves = needed,
            EasyMoves = MovesFor(EasyMoveUse),
            NormalMoves = MovesFor(NormalMoveUse),
            HardMoves = MovesFor(HardMoveUse),
            HitsOnObstacles = Mathf.RoundToInt(hits),
        };
    }

    /// <summary>Parts of the pressure, in moves needed.</summary>
    public struct PressureResult
    {
        public float Pressure;        // all moves needed / moves available
        public float ElementMoves;    // moves for the colour goals
        public float ObstacleMoves;   // moves for blockers, breakables, lockers
        public float BombMoves;       // moves to make the bombs
        public ShapeResult Shape;     // the board shape; Pressure = Needed x Shape.Multiplier / moves
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

        ShapeResult shape = Shape(input.Playable, input.Columns, input.Rows);

        return new PressureResult
        {
            Pressure = input.Moves > 0 ? (elementMoves + obstacleMoves + bombMoves) * shape.Multiplier / input.Moves : 0f,
            Shape = shape,
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
            case "expandable_01": family = new[] { TileKind.Expanding01, TileKind.Expanding02, TileKind.Expanding03 }; return true;
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
        float baseValue = Mathf.Min(TargetBaseMax, TargetBaseFirstWorld * (1f + TargetBaseGrowth * world));
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
