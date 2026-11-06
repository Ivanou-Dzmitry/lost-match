using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level editor: Lost Match > Level Editor.
/// Paint the board (background, blockers, preloaded elements), set goals and moves,
/// press Save (new levels are added to the world automatically) and Play level to test.
/// Replaces the Tiled + LevelConverter workflow.
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    // ----- paths and keys -----
    private const string LevelsRoot = "Assets/ScriptableObjects";
    private const string WorldsDir = "Assets/ScriptableObjects/Worlds";
    private const string ElementPrefabsDir = "Assets/Prefabs/Elements";
    private const string GameScene = "Assets/Scenes/GameBoard.unity";
    public const string TestLevelKey = "LM_TestLevel";
    private const string ReturnSceneKey = "LM_TestReturnScene";
    private const int BackFirstId = 26;      // background tile id = 26 + sprite index (as in the old TMX files)
    private const int DefaultBackId = 38;    // the plain tile
    private const int CellSize = 44;

    private enum Layer { Background, Objects, Elements }

    // Board objects that can be painted (the rest of TileKind are elements)
    private static readonly TileKind[] ObjectKinds =
    {
        TileKind.Empty,
        TileKind.Blocker01, TileKind.Blocker02, TileKind.Blocker03,
        TileKind.Breakable01, TileKind.Breakable02, TileKind.Breakable03,
        TileKind.Locked01, TileKind.Locked02, TileKind.Locked03,
        TileKind.Expanding01, TileKind.Expanding02, TileKind.Expanding03,
        TileKind.ColumnBomb, TileKind.RowBomb, TileKind.WrapBomb, TileKind.ColorBomb,
    };

    private static readonly string[] DefaultGoalValues =
    {
        "element_01", "element_02", "element_03", "element_04", "element_05",
        "blocker_01", "locker_01", "breakable_01", "LineBomb", "WrapBomb", "ColorBomb",
    };

    // ----- state -----
    private WorldManager worldManager;
    private Level level;                 // current level (an asset, or a draft that is not saved yet)
    private SerializedObject so;
    private int levelNumber = 1;
    private bool isDraft;
    private bool dirty;

    private int cols, rows;
    private int[] back;                  // background ids, top row first (like the TMX csv)
    private int[] obj;                   // TileKind or -1
    private int[] pre;                   // preloaded element index (0..4) or -1

    private Layer layer = Layer.Objects;
    private int selBack = DefaultBackId;
    private TileKind selKind = TileKind.Blocker01;
    private int selElem = 0;
    private bool erase;

    private int newCols, newRows;
    private Vector2 propsScroll;
    private Sprite[] backSprites;
    private readonly Dictionary<TileKind, Sprite> kindSprites = new Dictionary<TileKind, Sprite>();
    private Dictionary<string, Sprite> goalSprites = new Dictionary<string, Sprite>();
    private string[] goalValues = DefaultGoalValues;
    private List<float> curve = new List<float>();   // pressure of every saved level, index = level number - 1

    [MenuItem("Lost Match/Level Editor", priority = 10)]
    private static void Open()
    {
        var w = GetWindow<LevelEditorWindow>("Level Editor");
        w.minSize = new Vector2(980, 700);
    }

    // After Play mode ends go back to the scene that was open before "Play level"
    [InitializeOnLoadMethod]
    private static void HookPlayMode()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;

            EditorPrefs.DeleteKey(TestLevelKey);
            string scene = EditorPrefs.GetString(ReturnSceneKey, "");
            if (!string.IsNullOrEmpty(scene))
            {
                EditorPrefs.DeleteKey(ReturnSceneKey);
                if (File.Exists(scene)) EditorSceneManager.OpenScene(scene);
            }
        };
    }

    // ───────────────────────── lifecycle ─────────────────────────

    private void OnEnable()
    {
        backSprites = Resources.LoadAll<Sprite>("BackTiles/back_tile01");
        FindWorldManager();
        RefreshGoalCatalog();
        if (worldManager != null && level == null) LoadLevel(Mathf.Max(1, levelNumber));
    }

    private void FindWorldManager()
    {
        if (worldManager != null) return;
        string[] guids = AssetDatabase.FindAssets("t:WorldManager");
        if (guids.Length > 0)
            worldManager = AssetDatabase.LoadAssetAtPath<WorldManager>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private int TotalLevels => worldManager != null ? worldManager.GetTotalLevelsCount() : 0;

    // ───────────────────────── loading ─────────────────────────

    private bool ConfirmDiscard()
    {
        if (!dirty) return true;
        int r = EditorUtility.DisplayDialogComplex("Level Editor",
            $"Level {levelNumber} has unsaved changes.", "Save", "Discard", "Cancel");
        if (r == 2) return false;
        if (r == 0) return Save();
        return true;
    }

    private void LoadLevel(int number)
    {
        if (worldManager == null) return;

        Level found = worldManager.GetLevel(number, out World _);
        if (found == null)
        {
            Debug.LogWarning($"[LevelEditor] Level {number} not found.");
            return;
        }

        levelNumber = number;
        SetLevel(found, false);

        // background from the xml csv
        back = ParseBack(level.xmlLayoutFile, cols, rows);

        // board objects and preloaded elements (rowY 0 = bottom row)
        obj = Enumerable.Repeat(-1, cols * rows).ToArray();
        pre = Enumerable.Repeat(-1, cols * rows).ToArray();

        if (level.boardLayout != null)
            foreach (TileType t in level.boardLayout)
                if (InBounds(t.columnX, t.rowY)) obj[Idx(t.columnX, t.rowY)] = (int)t.tileKind;

        if (level.preloadBoardLayout != null)
            foreach (TileType t in level.preloadBoardLayout)
                if (InBounds(t.columnX, t.rowY)) pre[Idx(t.columnX, t.rowY)] = (int)t.tileKind;

        dirty = false;
    }

    private void SetLevel(Level l, bool draft)
    {
        level = l;
        so = new SerializedObject(level);
        isDraft = draft;
        cols = newCols = Mathf.Max(1, level.columns);
        rows = newRows = Mathf.Max(1, level.rows);
        selElem = 0;
        Repaint();
    }

    private static int[] ParseBack(TextAsset file, int c, int r)
    {
        int[] result = Enumerable.Repeat(0, c * r).ToArray();
        if (file == null) return result;

        try
        {
            XElement data = XDocument.Parse(file.text).Element("data");
            if (data == null) return result;

            string[] values = data.Value.Split(',');
            for (int i = 0; i < result.Length && i < values.Length; i++)
                int.TryParse(values[i].Trim(), out result[i]);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LevelEditor] Cannot read the background xml: " + e.Message);
        }
        return result;
    }

    private bool InBounds(int x, int rowY) => x >= 0 && x < cols && rowY >= 0 && rowY < rows;

    // grid index of a cell, rowY 0 = bottom row, the arrays are stored top row first
    private int Idx(int x, int rowY) => (rows - 1 - rowY) * cols + x;

    // ───────────────────────── new level ─────────────────────────

    private void NewLevel()
    {
        if (worldManager == null || !ConfirmDiscard()) return;

        int n = TotalLevels + 1;
        Level template = level;

        var draft = CreateInstance<Level>();
        draft.name = $"Level_{n:00}";
        draft.uID = $"lvl{n:00}";
        draft.columns = template != null ? template.columns : 8;
        draft.rows = template != null ? template.rows : 10;
        draft.levelDifficulty = Level.LevelDifficulty.Simple;
        draft.boardLayout = new TileType[0];
        draft.preloadBoardLayout = new TileType[0];
        draft.element = template != null && template.element != null && template.element.Length > 0
            ? (GameObject[])template.element.Clone()
            : DefaultElementPrefabs(3);
        draft.scoreGoals = new[] { 100, 200, 300 };
        draft.goalsDescription = "";
        draft.endGameRequrimentsForLevel = new EndGameRequriments
        {
            gameType = GameType.Moves,
            counterValue = template != null ? template.endGameRequrimentsForLevel.counterValue : 20,
        };
        draft.levelGoals = new BlankGoalClass[0];

        levelNumber = n;
        SetLevel(draft, true);
        back = Enumerable.Repeat(DefaultBackId, cols * rows).ToArray();
        obj = Enumerable.Repeat(-1, cols * rows).ToArray();
        pre = Enumerable.Repeat(-1, cols * rows).ToArray();
        dirty = true;
    }

    private static GameObject[] DefaultElementPrefabs(int count)
    {
        var list = new List<GameObject>();
        for (int i = 1; i <= count; i++)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{ElementPrefabsDir}/element{i:00}.prefab");
            if (p != null) list.Add(p);
        }
        return list.ToArray();
    }

    // ───────────────────────── save ─────────────────────────

    private bool Save()
    {
        if (level == null || so == null || worldManager == null) return false;

        if (cols < 1 || rows < 1)
        {
            EditorUtility.DisplayDialog("Level Editor", "Columns and rows must be at least 1.", "OK");
            return false;
        }

        string folder = LevelFolder(levelNumber);
        string assetPath = isDraft ? $"{folder}/Level_{levelNumber:00}.asset" : AssetDatabase.GetAssetPath(level);
        string xmlPath = level.xmlLayoutFile != null
            ? AssetDatabase.GetAssetPath(level.xmlLayoutFile)
            : $"{Path.GetDirectoryName(assetPath)?.Replace('\\', '/')}/Level_{levelNumber:00}_back.xml";

        // 1. the background xml
        File.WriteAllText(xmlPath, BuildBackXml(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(xmlPath);
        var xmlAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(xmlPath);

        // 2. the level data
        so.Update();
        so.FindProperty("columns").intValue = cols;
        so.FindProperty("rows").intValue = rows;
        so.FindProperty("xmlLayoutFile").objectReferenceValue = xmlAsset;
        WriteTiles(so.FindProperty("boardLayout"), obj);
        WriteTiles(so.FindProperty("preloadBoardLayout"), pre);

        SerializedProperty score = so.FindProperty("scoreGoals");
        if (score.arraySize < 3) score.arraySize = 3;

        // collected counters always start from 0
        SerializedProperty goals = so.FindProperty("levelGoals");
        for (int i = 0; i < goals.arraySize; i++)
            goals.GetArrayElementAtIndex(i).FindPropertyRelative("numberCollectedGoals").intValue = 0;

        so.ApplyModifiedProperties();

        // 3. a new level becomes an asset and is added to its world
        if (isDraft)
        {
            AssetDatabase.CreateAsset(level, assetPath);
            RegisterInWorld(level, levelNumber);
            isDraft = false;
            so = new SerializedObject(level);
        }

        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssets();
        dirty = false;
        RefreshGoalCatalog();
        Debug.Log($"[LevelEditor] Level {levelNumber} saved: {assetPath}");
        Repaint();
        return true;
    }

    private string BuildBackXml()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<data encoding=\"csv\">\n");
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                sb.Append(back[y * cols + x]);
                bool last = y == rows - 1 && x == cols - 1;
                if (!last) sb.Append(',');
            }
            sb.Append('\n');
        }
        sb.Append("</data>");
        return sb.ToString();
    }

    private void WriteTiles(SerializedProperty array, int[] cells)
    {
        var list = new List<(int x, int rowY, int kind)>();
        for (int rowY = 0; rowY < rows; rowY++)
            for (int x = 0; x < cols; x++)
            {
                int v = cells[Idx(x, rowY)];
                if (v >= 0) list.Add((x, rowY, v));
            }

        array.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("columnX").intValue = list[i].x;
            item.FindPropertyRelative("rowY").intValue = list[i].rowY;
            item.FindPropertyRelative("tileKind").intValue = list[i].kind;
        }
    }

    // Levels_1_10, Levels_11_20, ... (old folders are named Levels_51-60 too)
    private static string LevelFolder(int number)
    {
        int first = ((number - 1) / 10) * 10 + 1;
        int last = first + 9;

        foreach (string dir in AssetDatabase.GetSubFolders(LevelsRoot))
        {
            string name = Path.GetFileName(dir);
            if (name.StartsWith($"Levels_{first}_") || name.StartsWith($"Levels_{first}-"))
                return dir;
        }

        string guid = AssetDatabase.CreateFolder(LevelsRoot, $"Levels_{first}_{last}");
        return AssetDatabase.GUIDToAssetPath(guid);
    }

    private void RegisterInWorld(Level newLevel, int number)
    {
        int worldIndex = (number - 1) / 10;

        var wm = new SerializedObject(worldManager);
        SerializedProperty worlds = wm.FindProperty("worlds");

        // missing worlds are created
        while (worlds.arraySize <= worldIndex)
        {
            int num = worlds.arraySize + 1;
            var world = CreateInstance<World>();
            world.worldNumber = num;
            world.levels = new Level[0];

            if (!AssetDatabase.IsValidFolder(WorldsDir))
                AssetDatabase.CreateFolder(LevelsRoot, "Worlds");

            AssetDatabase.CreateAsset(world, $"{WorldsDir}/World{num:00}.asset");

            worlds.arraySize++;
            worlds.GetArrayElementAtIndex(worlds.arraySize - 1).objectReferenceValue = world;
        }
        wm.ApplyModifiedProperties();

        var targetWorld = (World)worlds.GetArrayElementAtIndex(worldIndex).objectReferenceValue;
        var w = new SerializedObject(targetWorld);
        SerializedProperty levels = w.FindProperty("levels");

        int slot = (number - 1) % 10;
        if (levels.arraySize <= slot) levels.arraySize = slot + 1;
        levels.GetArrayElementAtIndex(slot).objectReferenceValue = newLevel;
        w.ApplyModifiedProperties();

        EditorUtility.SetDirty(targetWorld);
        EditorUtility.SetDirty(worldManager);
    }

    // ───────────────────────── play ─────────────────────────

    private void PlayLevel()
    {
        if (EditorApplication.isPlaying) return;
        if (!Save()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorPrefs.SetInt(TestLevelKey, levelNumber);
        EditorPrefs.SetString(ReturnSceneKey, SceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene(GameScene);
        EditorApplication.isPlaying = true;
    }

    // ───────────────────────── sprites ─────────────────────────

    private static void DrawSprite(Rect r, Sprite s)
    {
        if (s == null || s.texture == null) return;
        Texture2D t = s.texture;
        Rect tr = s.textureRect;
        var uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
        GUI.DrawTextureWithTexCoords(r, t, uv, true);
    }

    private static Sprite SpriteOfPrefab(GameObject prefab)
    {
        if (prefab == null) return null;
        var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
        return sr != null ? sr.sprite : null;
    }

    private Sprite SpriteOfKind(TileKind kind)
    {
        if (kindSprites.TryGetValue(kind, out Sprite cached)) return cached;

        string path = null;
        switch (kind)
        {
            case TileKind.Blocker01: path = "Assets/Prefabs/Blockers/blocker_01.prefab"; break;
            case TileKind.Blocker02: path = "Assets/Prefabs/Blockers/blocker_02.prefab"; break;
            case TileKind.Blocker03: path = "Assets/Prefabs/Blockers/blocker_03.prefab"; break;
            case TileKind.Breakable01: path = "Assets/Prefabs/Blockers/breakable_01.prefab"; break;
            case TileKind.Breakable02: path = "Assets/Prefabs/Blockers/breakable_02.prefab"; break;
            case TileKind.Breakable03: path = "Assets/Prefabs/Blockers/breakable_03.prefab"; break;
            case TileKind.Locked01: path = "Assets/Prefabs/Lockers/locker_01.prefab"; break;
            case TileKind.Locked02: path = "Assets/Prefabs/Lockers/locker_02.prefab"; break;
            case TileKind.Locked03: path = "Assets/Prefabs/Lockers/locker_03.prefab"; break;
            case TileKind.Expanding01: path = "Assets/Prefabs/Expandable/expandable_01.prefab"; break;
            case TileKind.Expanding02: path = "Assets/Prefabs/Expandable/expandable_02.prefab"; break;
            case TileKind.Expanding03: path = "Assets/Prefabs/Expandable/expandable_03.prefab"; break;
        }

        Sprite sprite = path != null ? SpriteOfPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)) : null;
        kindSprites[kind] = sprite;
        return sprite;
    }

    private Sprite SpriteOfElement(int index)
    {
        if (level == null || level.element == null || index < 0 || index >= level.element.Length) return null;
        return SpriteOfPrefab(level.element[index]);
    }

    private static string KindLabel(TileKind k)
    {
        switch (k)
        {
            case TileKind.Empty: return "EMPTY";
            case TileKind.ColumnBomb: return "COL";
            case TileKind.RowBomb: return "ROW";
            case TileKind.WrapBomb: return "WRAP";
            case TileKind.ColorBomb: return "COLOR";
            default: return k.ToString().Replace("Blocker", "BL").Replace("Breakable", "BR")
                .Replace("Locked", "LK").Replace("Expanding", "EX");
        }
    }

    private static Color KindColor(TileKind k)
    {
        if (k == TileKind.Empty) return new Color(0.15f, 0.15f, 0.15f);
        if (k.ToString().StartsWith("Blocker")) return new Color(0.45f, 0.45f, 0.5f);
        if (k.ToString().StartsWith("Breakable")) return new Color(0.7f, 0.5f, 0.3f);
        if (k.ToString().StartsWith("Locked")) return new Color(0.6f, 0.6f, 0.2f);
        if (k.ToString().StartsWith("Expanding")) return new Color(0.3f, 0.6f, 0.3f);
        return new Color(0.7f, 0.3f, 0.5f); // bombs
    }

    // goal values and sprites already used by the existing levels
    private void RefreshGoalCatalog()
    {
        goalSprites = new Dictionary<string, Sprite>();
        FindWorldManager();
        if (worldManager == null) return;

        curve = new List<float>();
        int total = worldManager.GetTotalLevelsCount();
        for (int n = 1; n <= total; n++)
        {
            Level l = worldManager.GetLevel(n, out World _);
            curve.Add(l != null ? LevelTableExport.PressureOfAsset(l) : 0f);
            if (l == null || l.levelGoals == null) continue;

            foreach (BlankGoalClass g in l.levelGoals)
            {
                if (string.IsNullOrEmpty(g.matchValue)) continue;
                if (!goalSprites.ContainsKey(g.matchValue) || goalSprites[g.matchValue] == null)
                    goalSprites[g.matchValue] = g.goalSprite;
            }
        }

        goalValues = DefaultGoalValues.Concat(goalSprites.Keys).Distinct().ToArray();
    }

    // ───────────────────────── GUI ─────────────────────────

    private void OnGUI()
    {
        FindWorldManager();
        if (worldManager == null)
        {
            EditorGUILayout.HelpBox("WorldManager asset not found (Assets/ScriptableObjects/WorldManager.asset).", MessageType.Error);
            return;
        }

        if (level == null || so == null || so.targetObject == null)
        {
            LoadLevel(Mathf.Max(1, levelNumber));
            if (level == null) return;
        }

        DrawToolbar();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Max(cols * CellSize, 400) + 10));
        DrawPalette();
        GUILayout.Space(6);
        DrawGrid();
        GUILayout.Space(6);
        DrawStats();
        EditorGUILayout.EndVertical();

        propsScroll = EditorGUILayout.BeginScrollView(propsScroll);
        DrawProperties();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(26)) && levelNumber > 1 && ConfirmDiscard())
            LoadLevel(levelNumber - 1);

        string title = isDraft ? $"Level {levelNumber} (new)" : $"Level {levelNumber} / {TotalLevels}";
        GUILayout.Label(title + (dirty ? " *" : ""), GUILayout.Width(150));

        if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(26)) && !isDraft && levelNumber < TotalLevels && ConfirmDiscard())
            LoadLevel(levelNumber + 1);

        int go = EditorGUILayout.IntField(levelNumber, GUILayout.Width(40));
        if (GUILayout.Button("Go", EditorStyles.toolbarButton, GUILayout.Width(30)) && go >= 1 && go <= TotalLevels && go != levelNumber && ConfirmDiscard())
            LoadLevel(go);

        GUILayout.Space(10);
        if (GUILayout.Button("New level", EditorStyles.toolbarButton, GUILayout.Width(70))) NewLevel();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Export table", EditorStyles.toolbarButton, GUILayout.Width(85)))
            LevelTableExport.Export();

        if (!isDraft && GUILayout.Button("Select asset", EditorStyles.toolbarButton, GUILayout.Width(80)))
            Selection.activeObject = level;

        GUI.backgroundColor = dirty || isDraft ? new Color(0.6f, 1f, 0.6f) : Color.white;
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60))) Save();
        GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
        if (GUILayout.Button("Play level ▶", EditorStyles.toolbarButton, GUILayout.Width(90))) PlayLevel();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private bool IconButton(Rect r, bool selected, Sprite sprite, string label, Color bg)
    {
        if (selected) EditorGUI.DrawRect(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(1f, 0.8f, 0.1f));
        EditorGUI.DrawRect(r, bg);
        DrawSprite(r, sprite);
        if (sprite == null && !string.IsNullOrEmpty(label))
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 9 };
            style.normal.textColor = Color.white;
            GUI.Label(r, label, style);
        }
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    private void DrawPalette()
    {
        EditorGUILayout.BeginHorizontal();
        foreach (Layer l in Enum.GetValues(typeof(Layer)))
        {
            bool on = layer == l;
            if (GUILayout.Toggle(on, l.ToString(), EditorStyles.miniButton, GUILayout.Width(90)) && !on)
            {
                layer = l;
                erase = false;
            }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Left click paint, right click erase", EditorStyles.miniLabel, GUILayout.Width(210));
        EditorGUILayout.EndHorizontal();

        const int size = 36;
        float width = Mathf.Max(cols * CellSize, 400);
        int perRow = Mathf.Max(1, (int)(width / (size + 4)));

        int count;
        switch (layer)
        {
            case Layer.Background: count = backSprites.Length; break;
            case Layer.Objects: count = ObjectKinds.Length; break;
            default: count = level.element != null ? level.element.Length : 0; break;
        }

        int rowsNeeded = Mathf.Max(1, Mathf.CeilToInt(count / (float)perRow));
        Rect area = GUILayoutUtility.GetRect(width, rowsNeeded * (size + 4) + 4);

        for (int i = 0; i < count; i++)
        {
            var r = new Rect(area.x + 4 + (i % perRow) * (size + 4), area.y + 4 + (i / perRow) * (size + 4), size, size);

            if (layer == Layer.Background)
            {
                if (IconButton(r, selBack == BackFirstId + i, backSprites[i], (BackFirstId + i).ToString(), new Color(0.25f, 0.25f, 0.25f)))
                    selBack = BackFirstId + i;
            }
            else if (layer == Layer.Objects)
            {
                TileKind k = ObjectKinds[i];
                if (IconButton(r, selKind == k, SpriteOfKind(k), KindLabel(k), KindColor(k)))
                    selKind = k;
            }
            else
            {
                if (IconButton(r, selElem == i, SpriteOfElement(i), (i + 1).ToString(), new Color(0.2f, 0.3f, 0.4f)))
                    selElem = i;
            }
        }
    }

    private void DrawGrid()
    {
        Rect grid = GUILayoutUtility.GetRect(cols * CellSize, rows * CellSize);
        EditorGUI.DrawRect(new Rect(grid.x - 2, grid.y - 2, grid.width + 4, grid.height + 4), new Color(0.1f, 0.1f, 0.1f));

        var label = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 9 };
        label.normal.textColor = Color.white;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int i = y * cols + x;
                var r = new Rect(grid.x + x * CellSize, grid.y + y * CellSize, CellSize - 1, CellSize - 1);

                // background
                EditorGUI.DrawRect(r, new Color(0.22f, 0.22f, 0.22f));
                int b = back[i];
                int bi = b - BackFirstId;
                if (bi >= 0 && bi < backSprites.Length) DrawSprite(r, backSprites[bi]);

                // board object
                if (obj[i] >= 0)
                {
                    var kind = (TileKind)obj[i];
                    Sprite s = SpriteOfKind(kind);
                    if (s != null) DrawSprite(r, s);
                    else
                    {
                        EditorGUI.DrawRect(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), KindColor(kind));
                        GUI.Label(r, KindLabel(kind), label);
                    }
                }

                // preloaded element
                if (pre[i] >= 0)
                {
                    var er = new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 12);
                    Sprite s = SpriteOfElement(pre[i]);
                    EditorGUI.DrawRect(er, new Color(0, 0, 0, 0.35f));
                    if (s != null) DrawSprite(er, s);
                    GUI.Label(new Rect(r.x, r.y, 16, 14), (pre[i] + 1).ToString(), label);
                }
            }
        }

        Event e = Event.current;
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            && (e.button == 0 || e.button == 1) && grid.Contains(e.mousePosition))
        {
            int x = (int)((e.mousePosition.x - grid.x) / CellSize);
            int y = (int)((e.mousePosition.y - grid.y) / CellSize);
            if (x >= 0 && x < cols && y >= 0 && y < rows)
            {
                Paint(y * cols + x, e.button == 1);
                dirty = true;
                e.Use();
                Repaint();
            }
        }
    }

    private void Paint(int i, bool eraseCell)
    {
        switch (layer)
        {
            case Layer.Background:
                back[i] = eraseCell ? 0 : selBack;
                break;
            case Layer.Objects:
                obj[i] = eraseCell ? -1 : (int)selKind;
                break;
            case Layer.Elements:
                pre[i] = eraseCell ? -1 : selElem;
                break;
        }
    }

    private bool showWhatIf = true;

    // input of the pressure estimate for the level that is painted now (not saved yet)
    private LevelTableExport.PressureInput BuildPressureInput()
    {
        var count = new Dictionary<TileKind, int>();
        foreach (int v in obj)
            if (v >= 0) count[(TileKind)v] = count.TryGetValue((TileKind)v, out int c) ? c + 1 : 1;

        var goals = new List<(string match, int need)>();
        if (level.levelGoals != null)
            foreach (BlankGoalClass g in level.levelGoals)
                goals.Add((g.matchValue, g.numberGoalsNeeded));

        return new LevelTableExport.PressureInput
        {
            Colours = level.element != null ? level.element.Length : 0,
            Moves = level.endGameRequrimentsForLevel != null ? level.endGameRequrimentsForLevel.counterValue : 0,
            Goals = goals,
            Count = count,
            ElementTags = (level.element ?? new GameObject[0]).Where(e => e != null).Select(e => e.tag).ToList(),
        };
    }

    private void DrawStats()
    {
        int area = back.Count(v => (v >= 26 && v <= 33) || v == 38);
        int objects = obj.Count(v => v >= 0);
        int preloaded = pre.Count(v => v >= 0);
        int empty = obj.Count(v => v == (int)TileKind.Empty);
        EditorGUILayout.LabelField($"Playable cells: {area - empty}   (background {area}, empty {empty})   Objects: {objects}   Preloaded: {preloaded}", EditorStyles.miniLabel);

        // ----- live difficulty -----
        LevelTableExport.PressureInput input = BuildPressureInput();
        var notes = new List<string>();
        LevelTableExport.PressureResult result = LevelTableExport.EstimateDetailed(input, notes);

        float pressure = result.Pressure;
        float target = LevelTableExport.TargetPressure(levelNumber);
        float delta = pressure - target;

        // neighbours are the saved levels, this level is the one in the editor
        var around = new List<float>(curve);
        while (around.Count < levelNumber) around.Add(0f);
        around[levelNumber - 1] = pressure;
        float local = LevelTableExport.LocalMedian(around, levelNumber - 1);

        string role = pressure <= 0f ? "-"
            : pressure > local * LevelTableExport.PeakFactor ? "Peak"
            : pressure < local * LevelTableExport.BreatherFactor ? "Breather" : "Normal";

        var big = new GUIStyle(EditorStyles.boldLabel);
        big.normal.textColor = RatingColor(pressure);
        EditorGUILayout.LabelField($"Pressure {pressure:0.00} ({LevelTableExport.Rate(pressure)})   Role: {role}   Neighbours: {local:0.00}", big);

        string finale = levelNumber % 10 == 0 ? "   (world finale: aim for a peak)" : "";
        string check = Mathf.Abs(delta) > LevelTableExport.ReviewDelta ? (delta > 0 ? "  too hard for its place" : "  too easy for its place") : "  ok";
        EditorGUILayout.LabelField($"Target {target:0.00}   Delta {delta:+0.00;-0.00}{check}{finale}", EditorStyles.miniLabel);

        DrawLoadBar(result, input.Moves);
        DrawCurveChart(pressure);

        showWhatIf = EditorGUILayout.Foldout(showWhatIf, "What changes the difficulty (this level)", true);
        if (showWhatIf) DrawWhatIf(input, pressure);

        foreach (string note in notes)
            EditorGUILayout.HelpBox(note, MessageType.Warning);

        int elementCount = level.element != null ? level.element.Length : 0;
        if (pre.Any(v => v >= elementCount))
            EditorGUILayout.HelpBox("A preloaded element uses a type that is not in the element list.", MessageType.Warning);

        for (int i = 0; i < obj.Length; i++)
        {
            if (obj[i] == (int)TileKind.Empty && pre[i] >= 0)
            {
                EditorGUILayout.HelpBox("A preloaded element is on an Empty cell.", MessageType.Warning);
                break;
            }
        }
    }

    // moves needed for the goals against the moves available: blue = colour goals, orange = obstacles, purple = bombs
    private void DrawLoadBar(LevelTableExport.PressureResult result, int moves)
    {
        Rect r = GUILayoutUtility.GetRect(Mathf.Max(cols * CellSize, 400), 18);
        EditorGUI.DrawRect(r, new Color(0.14f, 0.14f, 0.14f));
        if (moves <= 0) return;

        float scale = r.width / (moves * 2f);     // the full bar = 200% of the moves
        float x = r.x;
        void Segment(float needed, Color c)
        {
            float w = needed * scale;
            if (w <= 0f) return;
            EditorGUI.DrawRect(new Rect(x, r.y, Mathf.Min(w, r.xMax - x), r.height), c);
            x += w;
        }

        Segment(result.ElementMoves, new Color(0.3f, 0.55f, 0.9f));
        Segment(result.ObstacleMoves, new Color(0.95f, 0.6f, 0.2f));
        Segment(result.BombMoves, new Color(0.7f, 0.4f, 0.85f));

        // 100% = all the moves
        EditorGUI.DrawRect(new Rect(r.x + moves * scale, r.y - 1, 2, r.height + 2), Color.white);

        var small = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9 };
        small.normal.textColor = Color.white;
        GUI.Label(r, $" colours {result.ElementMoves:0.0}  obstacles {result.ObstacleMoves:0.0}  bombs {result.BombMoves:0.0}  of {moves} moves", small);
    }

    // changes the level a little and shows how the pressure moves: this is what really affects the difficulty
    private void DrawWhatIf(LevelTableExport.PressureInput input, float pressure)
    {
        if (input.Moves <= 0)
        {
            EditorGUILayout.HelpBox("Set the number of moves to see the effects.", MessageType.Info);
            return;
        }

        void Row(string label, LevelTableExport.PressureInput changed)
        {
            float p = LevelTableExport.EstimateDetailed(changed, null).Pressure;
            float d = p - pressure;
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = d > 0.005f ? new Color(0.95f, 0.5f, 0.4f) : d < -0.005f ? new Color(0.5f, 0.85f, 0.5f) : Color.gray;
            EditorGUILayout.LabelField($"{label}:  {d:+0.00;-0.00;0.00}  ->  {p:0.00}", style);
        }

        LevelTableExport.PressureInput With(int moves, int colours, IList<(string match, int need)> goals)
        {
            var copy = input;
            copy.Moves = moves;
            copy.Colours = colours;
            copy.Goals = goals;
            return copy;
        }

        Row("+1 move", With(input.Moves + 1, input.Colours, input.Goals));
        Row("-1 move", With(Mathf.Max(1, input.Moves - 1), input.Colours, input.Goals));
        Row("+1 colour", With(input.Moves, input.Colours + 1, input.Goals));
        Row("-1 colour", With(input.Moves, Mathf.Max(1, input.Colours - 1), input.Goals));

        for (int i = 0; i < input.Goals.Count; i++)
        {
            var more = new List<(string match, int need)>(input.Goals);
            more[i] = (more[i].match, more[i].need + 5);
            Row($"goal {more[i].match} +5", With(input.Moves, input.Colours, more));
        }
    }

    private static Color RatingColor(float p)
    {
        if (p <= 0f) return Color.gray;
        if (p < 0.55f) return new Color(0.4f, 0.8f, 0.4f);
        if (p < 0.8f) return new Color(0.9f, 0.85f, 0.3f);
        if (p < 1.0f) return new Color(0.95f, 0.6f, 0.2f);
        return new Color(0.9f, 0.3f, 0.3f);
    }

    // bars = pressure of the levels around this one (this level is live), white dashes = the target curve
    private void DrawCurveChart(float livePressure)
    {
        const float height = 90f;
        const float maxValue = 2.5f;
        const int radius = 14;

        Rect r = GUILayoutUtility.GetRect(Mathf.Max(cols * CellSize, 400), height);
        EditorGUI.DrawRect(r, new Color(0.14f, 0.14f, 0.14f));

        int total = Mathf.Max(curve.Count, levelNumber);
        int first = Mathf.Max(1, levelNumber - radius);
        int last = Mathf.Min(total, levelNumber + radius);
        int count = last - first + 1;
        if (count <= 0) return;

        float barW = r.width / count;
        float usable = r.height - 12;

        // the line of 1.0
        float oneY = r.yMax - Mathf.Clamp01(1f / maxValue) * usable;
        EditorGUI.DrawRect(new Rect(r.x, oneY, r.width, 1), new Color(1f, 1f, 1f, 0.2f));

        var small = new GUIStyle(EditorStyles.miniLabel) { fontSize = 8, alignment = TextAnchor.UpperCenter };
        small.normal.textColor = new Color(1f, 1f, 1f, 0.6f);

        for (int n = first; n <= last; n++)
        {
            int i = n - first;
            float value = n == levelNumber ? livePressure : (n - 1 < curve.Count ? curve[n - 1] : 0f);
            float h = Mathf.Clamp01(value / maxValue) * usable;
            var bar = new Rect(r.x + i * barW + 1, r.yMax - h, barW - 2, h);

            EditorGUI.DrawRect(bar, RatingColor(value));

            if (n == levelNumber)
            {
                EditorGUI.DrawRect(new Rect(bar.x - 1, r.y, bar.width + 2, 1), Color.white);
                EditorGUI.DrawRect(new Rect(bar.x - 1, r.yMax - 1, bar.width + 2, 1), Color.white);
                EditorGUI.DrawRect(new Rect(bar.x - 1, r.y, 1, r.height), Color.white);
                EditorGUI.DrawRect(new Rect(bar.xMax, r.y, 1, r.height), Color.white);
            }

            // the target of this level
            float ty = r.yMax - Mathf.Clamp01(LevelTableExport.TargetPressure(n) / maxValue) * usable;
            EditorGUI.DrawRect(new Rect(r.x + i * barW, ty, barW, 2), Color.white);

            if (n % 5 == 0 || n == levelNumber)
                GUI.Label(new Rect(r.x + i * barW - 6, r.y, barW + 12, 12), n.ToString(), small);
        }
    }

    private void DrawProperties()
    {
        so.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("uID"), new GUIContent("UID"));
        EditorGUILayout.PropertyField(so.FindProperty("levelDifficulty"), new GUIContent("Difficulty"));

        // size
        EditorGUILayout.BeginHorizontal();
        newCols = EditorGUILayout.IntField("Columns", newCols);
        newRows = EditorGUILayout.IntField("Rows", newRows);
        using (new EditorGUI.DisabledScope(newCols == cols && newRows == rows))
        {
            if (GUILayout.Button("Resize", GUILayout.Width(60))) Resize(newCols, newRows);
        }
        EditorGUILayout.EndHorizontal();

        // end game
        GUILayout.Space(6);
        EditorGUILayout.LabelField("End of the game", EditorStyles.boldLabel);
        SerializedProperty end = so.FindProperty("endGameRequrimentsForLevel");
        EditorGUILayout.PropertyField(end.FindPropertyRelative("gameType"), new GUIContent("Type (Moves / Time)"));
        EditorGUILayout.PropertyField(end.FindPropertyRelative("counterValue"), new GUIContent("Counter (moves or seconds)"));

        // score goals (stars)
        SerializedProperty score = so.FindProperty("scoreGoals");
        if (score.arraySize < 3) score.arraySize = 3;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Score for 1 / 2 / 3 stars");
        for (int i = 0; i < 3; i++)
            score.GetArrayElementAtIndex(i).intValue = EditorGUILayout.IntField(score.GetArrayElementAtIndex(i).intValue);
        EditorGUILayout.EndHorizontal();

        // description
        GUILayout.Space(6);
        EditorGUILayout.LabelField("Goals description", EditorStyles.boldLabel);
        SerializedProperty desc = so.FindProperty("goalsDescription");
        desc.stringValue = EditorGUILayout.TextArea(desc.stringValue, GUILayout.MinHeight(40));

        DrawGoals();
        DrawElementTypes();

        if (EditorGUI.EndChangeCheck()) dirty = true;
        so.ApplyModifiedProperties();
    }

    private void DrawGoals()
    {
        GUILayout.Space(6);
        EditorGUILayout.LabelField("Level goals", EditorStyles.boldLabel);
        SerializedProperty goals = so.FindProperty("levelGoals");

        int remove = -1;
        for (int i = 0; i < goals.arraySize; i++)
        {
            SerializedProperty g = goals.GetArrayElementAtIndex(i);
            SerializedProperty need = g.FindPropertyRelative("numberGoalsNeeded");
            SerializedProperty match = g.FindPropertyRelative("matchValue");
            SerializedProperty sprite = g.FindPropertyRelative("goalSprite");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            int current = Array.IndexOf(goalValues, match.stringValue);
            int picked = EditorGUILayout.Popup(current, goalValues);
            if (picked >= 0 && picked != current)
            {
                match.stringValue = goalValues[picked];
                if (goalSprites.TryGetValue(match.stringValue, out Sprite s) && s != null)
                    sprite.objectReferenceValue = s;
            }
            EditorGUILayout.PropertyField(match, GUIContent.none);

            need.intValue = EditorGUILayout.IntField(need.intValue, GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(24))) remove = i;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(sprite, new GUIContent("Icon"));
            EditorGUILayout.EndVertical();
        }
        if (remove >= 0) goals.DeleteArrayElementAtIndex(remove);

        if (GUILayout.Button("+ Add goal", GUILayout.Width(100)))
        {
            goals.arraySize++;
            SerializedProperty g = goals.GetArrayElementAtIndex(goals.arraySize - 1);
            g.FindPropertyRelative("numberGoalsNeeded").intValue = 10;
            g.FindPropertyRelative("numberCollectedGoals").intValue = 0;
            g.FindPropertyRelative("matchValue").stringValue = goalValues[0];
            g.FindPropertyRelative("goalSprite").objectReferenceValue =
                goalSprites.TryGetValue(goalValues[0], out Sprite s0) ? s0 : null;
        }
    }

    private void DrawElementTypes()
    {
        GUILayout.Space(6);
        EditorGUILayout.LabelField("Element types (preload uses the numbers 1, 2, 3...)", EditorStyles.boldLabel);
        SerializedProperty list = so.FindProperty("element");

        int remove = -1;
        for (int i = 0; i < list.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent($"Element {i + 1}"));
            if (GUILayout.Button("X", GUILayout.Width(24))) remove = i;
            EditorGUILayout.EndHorizontal();
        }
        if (remove >= 0)
        {
            list.GetArrayElementAtIndex(remove).objectReferenceValue = null;
            list.DeleteArrayElementAtIndex(remove);
            for (int i = 0; i < pre.Length; i++)
                if (pre[i] == remove) pre[i] = -1;
                else if (pre[i] > remove) pre[i]--;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use elements 1-3")) SetElementTypes(3);
        if (GUILayout.Button("1-4")) SetElementTypes(4);
        if (GUILayout.Button("1-5")) SetElementTypes(5);
        EditorGUILayout.EndHorizontal();
    }

    private void SetElementTypes(int count)
    {
        SerializedProperty list = so.FindProperty("element");
        GameObject[] prefabs = DefaultElementPrefabs(count);
        list.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
    }

    // keeps the cells by (column, rowY): the bottom-left corner stays in place
    private void Resize(int c, int r)
    {
        c = Mathf.Clamp(c, 1, 20);
        r = Mathf.Clamp(r, 1, 20);

        int[] nb = Enumerable.Repeat(DefaultBackId, c * r).ToArray();
        int[] no = Enumerable.Repeat(-1, c * r).ToArray();
        int[] np = Enumerable.Repeat(-1, c * r).ToArray();

        for (int rowY = 0; rowY < r; rowY++)
        {
            for (int x = 0; x < c; x++)
            {
                int ni = (r - 1 - rowY) * c + x;
                if (x < cols && rowY < rows)
                {
                    int oi = Idx(x, rowY);
                    nb[ni] = back[oi];
                    no[ni] = obj[oi];
                    np[ni] = pre[oi];
                }
            }
        }

        cols = newCols = c;
        rows = newRows = r;
        back = nb;
        obj = no;
        pre = np;
        dirty = true;
    }

    private void OnDestroy()
    {
        if (dirty && level != null)
        {
            if (EditorUtility.DisplayDialog("Level Editor", $"Level {levelNumber} has unsaved changes. Save?", "Save", "Discard"))
                Save();
        }
    }
}
