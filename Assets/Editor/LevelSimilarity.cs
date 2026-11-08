using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// How much levels look alike: the shape of the board, the obstacles, the goals and the colours.
/// The Level Editor shows the most similar levels while you paint, and Lost Match > Tools > Repetition Report
/// writes docs/level_repetition.md for the whole game. Levels that look alike are fine if they are far apart,
/// so the rules are about the distance in levels.
/// </summary>
public static class LevelSimilarity
{
    // ----- rules: tune them for the game -----
    public const int MinShapeGap = 20;        // the same shape again not sooner than this many levels
    public const int MinGoalGapMany = 8;      // the same set of 2 or more goal types
    public const int MinGoalGapOne = 4;       // the same single goal type
    public const int MaxSameGoalRun = 3;      // levels in a row with the same goals, more is a warning
    public const float SameShape = 0.95f;     // playable cells that match, 1 = the same board (mirrors count)
    public const float SimilarEnough = 0.75f; // the report lists the pairs above this

    // ----- weights of the similarity -----
    private const float ShapeWeight = 0.35f;
    private const float ObstacleWeight = 0.25f;
    private const float GoalWeight = 0.30f;
    private const float ColourWeight = 0.10f;

    /// <summary>The "fingerprint" of a level. Cells are stored top row first.</summary>
    public class LevelPrint
    {
        public int Number;
        public int Cols;
        public int Rows;
        public bool[] Playable;
        public byte[] Obstacles;          // 0 none, 1 blocker, 2 breakable, 3 locker, 4 expanding
        public HashSet<string> Goals;     // goal types: colour, blocker_01, breakable_01, locker_01, LineBomb...
        public int Colours;
        public int Moves;
        public bool Plain => Playable == null || Playable.All(v => v);   // an open rectangle has no shape to repeat

        public string GoalText => Goals.Count == 0 ? "no goals" : string.Join(" + ", Goals.OrderBy(g => g));
        public bool HasObstacles => Obstacles != null && Obstacles.Any(o => o != 0);
    }

    public struct Match
    {
        public float Score;       // 0..1
        public float Shape;
        public float Obstacles;
        public float Goals;
        public bool SameShapeAndPlace;
        public string Reason;
    }

    // ───────────────────────── building ─────────────────────────

    public static byte ObstacleCategory(TileKind k)
    {
        switch (k)
        {
            case TileKind.Blocker01: case TileKind.Blocker02: case TileKind.Blocker03: return 1;
            case TileKind.Breakable01: case TileKind.Breakable02: case TileKind.Breakable03: return 2;
            case TileKind.Locked01: case TileKind.Locked02: case TileKind.Locked03: return 3;
            case TileKind.Expanding01: case TileKind.Expanding02: case TileKind.Expanding03: return 4;
            default: return 0;
        }
    }

    private static string GoalCategory(string match)
    {
        if (string.IsNullOrEmpty(match)) return null;
        return match.StartsWith("element_") ? "colour" : match;
    }

    public static LevelPrint Build(int number, int cols, int rows, bool[] playable, IList<int> objectKinds,
        IEnumerable<string> goals, int colours, int moves)
    {
        var obstacles = new byte[cols * rows];
        if (objectKinds != null)
            for (int i = 0; i < obstacles.Length && i < objectKinds.Count; i++)
                if (objectKinds[i] >= 0) obstacles[i] = ObstacleCategory((TileKind)objectKinds[i]);

        return new LevelPrint
        {
            Number = number, Cols = cols, Rows = rows,
            Playable = playable ?? Enumerable.Repeat(true, cols * rows).ToArray(),
            Obstacles = obstacles,
            Goals = new HashSet<string>((goals ?? new string[0]).Select(GoalCategory).Where(g => g != null)),
            Colours = colours, Moves = moves,
        };
    }

    public static LevelPrint Build(Level level, int number)
    {
        var kinds = Enumerable.Repeat(-1, level.columns * level.rows).ToArray();
        if (level.boardLayout != null)
            foreach (TileType t in level.boardLayout)
                if (t.columnX >= 0 && t.columnX < level.columns && t.rowY >= 0 && t.rowY < level.rows)
                    kinds[(level.rows - 1 - t.rowY) * level.columns + t.columnX] = (int)t.tileKind;

        return Build(number, level.columns, level.rows, LevelTableExport.PlayableMask(level), kinds,
            level.levelGoals != null ? level.levelGoals.Select(g => g.matchValue) : null,
            level.element != null ? level.element.Length : 0,
            level.endGameRequrimentsForLevel != null ? level.endGameRequrimentsForLevel.counterValue : 0);
    }

    public static List<LevelPrint> BuildAll(WorldManager worldManager)
    {
        var all = new List<LevelPrint>();
        if (worldManager == null) return all;

        int total = worldManager.GetTotalLevelsCount();
        for (int n = 1; n <= total; n++)
        {
            Level level = worldManager.GetLevel(n, out World _);
            if (level != null) all.Add(Build(level, n));
        }
        return all;
    }

    // ───────────────────────── comparing ─────────────────────────

    // a board can be mirrored: bit 1 = left-right, bit 2 = top-bottom
    private static int Mapped(int i, int cols, int rows, int mirror)
    {
        int r = i / cols, c = i % cols;
        if ((mirror & 1) != 0) c = cols - 1 - c;
        if ((mirror & 2) != 0) r = rows - 1 - r;
        return r * cols + c;
    }

    private static float Jaccard<T>(ICollection<T> a, ICollection<T> b)
    {
        int union = a.Union(b).Count();
        return union == 0 ? 1f : a.Intersect(b).Count() / (float)union;
    }

    private static float ShapeSimilarity(LevelPrint a, LevelPrint b, int mirror)
    {
        int inter = 0, union = 0;
        for (int i = 0; i < a.Playable.Length; i++)
        {
            bool x = a.Playable[i], y = b.Playable[Mapped(i, a.Cols, a.Rows, mirror)];
            if (x || y) union++;
            if (x && y) inter++;
        }
        return union == 0 ? 1f : inter / (float)union;
    }

    private static float ObstacleSimilarity(LevelPrint a, LevelPrint b, int mirror)
    {
        int inter = 0, union = 0;
        for (int i = 0; i < a.Obstacles.Length; i++)
        {
            byte x = a.Obstacles[i], y = b.Obstacles[Mapped(i, a.Cols, a.Rows, mirror)];
            if (x == 0 && y == 0) continue;
            union++;
            if (x == y) inter++;
        }
        return union == 0 ? 1f : inter / (float)union;
    }

    public static Match Compare(LevelPrint a, LevelPrint b)
    {
        float goals = Jaccard(a.Goals, b.Goals);
        float colours = a.Colours == b.Colours ? 1f : 0f;
        float shape = 0f, obstacles;

        if (a.Cols == b.Cols && a.Rows == b.Rows)
        {
            // the same mirror for the shape and the obstacles
            float best = -1f; obstacles = 0f;
            for (int mirror = 0; mirror < 4; mirror++)
            {
                float s = ShapeSimilarity(a, b, mirror), o = ObstacleSimilarity(a, b, mirror);
                float combined = ShapeWeight * s + ObstacleWeight * o;
                if (combined > best) { best = combined; shape = s; obstacles = o; }
            }
        }
        else
        {
            // another size: only the kinds of obstacles can look alike
            var ka = new HashSet<byte>(a.Obstacles.Where(o => o != 0));
            var kb = new HashSet<byte>(b.Obstacles.Where(o => o != 0));
            obstacles = 0.5f * Jaccard(ka, kb);
        }

        bool sameShape = !a.Plain && !b.Plain && shape >= SameShape;
        var reasons = new List<string>();
        if (sameShape) reasons.Add("same shape");
        if (a.HasObstacles && b.HasObstacles && obstacles >= 0.8f) reasons.Add("same obstacles");
        if (goals >= 1f) reasons.Add("same goals");
        if (a.Colours == b.Colours && a.Colours > 0) reasons.Add($"{a.Colours} colours");

        return new Match
        {
            Score = ShapeWeight * shape + ObstacleWeight * obstacles + GoalWeight * goals + ColourWeight * colours,
            Shape = shape, Obstacles = obstacles, Goals = goals,
            SameShapeAndPlace = sameShape,
            Reason = string.Join(", ", reasons),
        };
    }

    /// <summary>The most similar saved levels to this one (the level itself is skipped).</summary>
    public static List<(LevelPrint print, Match match)> MostSimilar(LevelPrint level, IList<LevelPrint> all, int count)
    {
        return all.Where(p => p.Number != level.Number)
            .Select(p => (print: p, match: Compare(level, p)))
            .OrderByDescending(x => x.match.Score)
            .Take(count)
            .ToList();
    }

    private static int GoalGap(LevelPrint p) => p.Goals.Count >= 2 ? MinGoalGapMany : MinGoalGapOne;

    /// <summary>Which rules about the distance this level breaks.</summary>
    public static List<string> RuleWarnings(LevelPrint level, IList<LevelPrint> all)
    {
        var warnings = new List<string>();

        // the same shape too close
        LevelPrint shapeNear = null;
        int shapeDistance = int.MaxValue;
        foreach (LevelPrint p in all)
        {
            int d = Math.Abs(p.Number - level.Number);
            if (p.Number == level.Number || d >= MinShapeGap || d >= shapeDistance) continue;
            if (p.Plain || level.Plain || p.Cols != level.Cols || p.Rows != level.Rows) continue;

            float s = Enumerable.Range(0, 4).Max(m => ShapeSimilarity(level, p, m));
            if (s >= SameShape) { shapeNear = p; shapeDistance = d; }
        }
        if (shapeNear != null)
            warnings.Add($"The same shape as level {shapeNear.Number}, {shapeDistance} levels away (keep at least {MinShapeGap}).");

        // the same goals too close
        if (level.Goals.Count > 0)
        {
            int gap = GoalGap(level);
            LevelPrint near = all.Where(p => p.Number != level.Number && Math.Abs(p.Number - level.Number) < gap && p.Goals.SetEquals(level.Goals))
                .OrderBy(p => Math.Abs(p.Number - level.Number)).FirstOrDefault();
            if (near != null)
                warnings.Add($"The same goals ({level.GoalText}) as level {near.Number}, {Math.Abs(near.Number - level.Number)} levels away (keep at least {gap}).");
        }

        // a run of the same goals
        var byNumber = all.ToDictionary(p => p.Number);
        int run = 1;
        for (int n = level.Number - 1; n >= 1 && byNumber.TryGetValue(n, out LevelPrint prev) && prev.Goals.SetEquals(level.Goals) && level.Goals.Count > 0; n--) run++;
        if (run > MaxSameGoalRun)
            warnings.Add($"{run} levels in a row with the goals {level.GoalText}.");

        return warnings;
    }

    // ───────────────────────── report ─────────────────────────

    [MenuItem("Lost Match/Tools/Repetition Report", priority = 51)]
    public static void WriteReport()
    {
        string[] guids = AssetDatabase.FindAssets("t:WorldManager");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Lost Match", "WorldManager asset not found.", "OK");
            return;
        }

        var worldManager = AssetDatabase.LoadAssetAtPath<WorldManager>(AssetDatabase.GUIDToAssetPath(guids[0]));
        List<LevelPrint> all = BuildAll(worldManager);

        string docs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs");
        Directory.CreateDirectory(docs);
        string path = Path.Combine(docs, "level_repetition.md");

        File.WriteAllText(path, BuildReport(all), new UTF8Encoding(false));
        Debug.Log($"[LostMatch] Repetition report saved: {path}");
        EditorUtility.RevealInFinder(path);
    }

    private static string BuildReport(List<LevelPrint> all)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Level repetition report");
        sb.AppendLine();
        sb.AppendLine($"Levels checked: {all.Count}. Made by Lost Match > Tools > Repetition Report. Rules and weights are at the top of `Assets/Editor/LevelSimilarity.cs`.");
        sb.AppendLine();
        sb.AppendLine($"Rules about the distance: the same shape not sooner than {MinShapeGap} levels, the same goals not sooner than {MinGoalGapMany} levels (two or more goal types) or {MinGoalGapOne} levels (one type), no more than {MaxSameGoalRun} levels in a row with the same goals.");
        sb.AppendLine();

        // goals
        sb.AppendLine("## Goals");
        sb.AppendLine();
        sb.AppendLine("| Goals | Levels | Share |");
        sb.AppendLine("|---|---|---|");
        foreach (var g in all.GroupBy(p => p.GoalText).OrderByDescending(g => g.Count()))
            sb.AppendLine($"| {g.Key} | {g.Count()} | {100f * g.Count() / Math.Max(1, all.Count):0}% |");
        sb.AppendLine();

        // shapes that repeat
        sb.AppendLine("## Shapes that repeat");
        sb.AppendLine();
        var used = new HashSet<int>();
        int groups = 0;
        foreach (LevelPrint a in all)
        {
            if (a.Plain || used.Contains(a.Number)) continue;
            var group = all.Where(b => b.Number == a.Number
                || (!b.Plain && !used.Contains(b.Number) && b.Cols == a.Cols && b.Rows == a.Rows
                    && Enumerable.Range(0, 4).Max(m => ShapeSimilarity(a, b, m)) >= SameShape)).ToList();
            if (group.Count < 2) continue;

            foreach (LevelPrint p in group) used.Add(p.Number);
            groups++;

            int minGap = group.Zip(group.Skip(1), (x, y) => y.Number - x.Number).Min();
            sb.AppendLine($"- {a.Cols}x{a.Rows}: levels {string.Join(", ", group.Select(p => p.Number))}" + (minGap < MinShapeGap ? $" (**closest {minGap} apart, too close**)" : $" (closest {minGap} apart)"));
        }
        if (groups == 0) sb.AppendLine("No shape repeats.");
        sb.AppendLine();

        // rule violations
        sb.AppendLine("## Rules broken");
        sb.AppendLine();
        int broken = 0;
        foreach (LevelPrint p in all)
        {
            // only the pairs where the other level is earlier, so a pair is listed once
            var earlier = all.Where(q => q.Number < p.Number).ToList();
            foreach (string w in RuleWarnings(p, earlier))
            {
                sb.AppendLine($"- Level {p.Number}: {w}");
                broken++;
            }
        }
        if (broken == 0) sb.AppendLine("None.");
        sb.AppendLine();

        // runs of the same goals
        sb.AppendLine("## Runs of the same goals");
        sb.AppendLine();
        int runStart = 0, runs = 0;
        for (int i = 1; i <= all.Count; i++)
        {
            bool same = i < all.Count && all[i].Goals.Count > 0 && all[i].Goals.SetEquals(all[runStart].Goals);
            if (same) continue;
            int length = i - runStart;
            if (length >= 3)
            {
                sb.AppendLine($"- levels {all[runStart].Number}-{all[i - 1].Number} ({length} in a row): {all[runStart].GoalText}");
                runs++;
            }
            runStart = i;
        }
        if (runs == 0) sb.AppendLine("None.");
        sb.AppendLine();

        // the most similar pairs
        sb.AppendLine($"## Most similar pairs (similarity from {SimilarEnough * 100:0}%)");
        sb.AppendLine();
        var pairs = new List<(LevelPrint a, LevelPrint b, Match m)>();
        for (int i = 0; i < all.Count; i++)
            for (int j = i + 1; j < all.Count; j++)
            {
                Match m = Compare(all[i], all[j]);
                if (m.Score >= SimilarEnough) pairs.Add((all[i], all[j], m));
            }

        if (pairs.Count == 0)
        {
            sb.AppendLine("None.");
        }
        else
        {
            sb.AppendLine("| Levels | Apart | Similarity | Why |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var p in pairs.OrderByDescending(x => x.m.Score).Take(60))
                sb.AppendLine($"| {p.a.Number} and {p.b.Number} | {p.b.Number - p.a.Number} | {p.m.Score * 100:0}% | {p.m.Reason} |");
            if (pairs.Count > 60) sb.AppendLine($"\n{pairs.Count - 60} more pairs are not listed.");
        }

        return sb.ToString();
    }
}
