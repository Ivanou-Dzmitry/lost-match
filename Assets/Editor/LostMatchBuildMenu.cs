using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Top menu "Lost Match": version bump + Google Play AAB build, and a one-off
/// fix for EventSystems after switching to the Input System package.
/// Keystore passwords are NOT stored in the project: type them once per editor
/// session in Player Settings > Publishing Settings, or set the environment
/// variables LOSTMATCH_KEYSTORE_PASS and LOSTMATCH_KEYALIAS_PASS.
/// </summary>
public static class LostMatchBuildMenu
{
    const string Root = "Lost Match/";
    const string OutputDir = "Builds";
    const string FilePrefix = "LostMatch";

    // Matches the "Version 0.0.17" part of the version labels shown in the UI.
    static readonly Regex VersionLabel = new Regex(@"(Version\s+)\d+(\.\d+)+");

    // ───────────────────────── menu items ─────────────────────────

    [MenuItem(Root + "Build/Bump Version + Build AAB (Google Play)", priority = 0)]
    static void BumpAndBuild() => Run(bump: true, build: true);

    [MenuItem(Root + "Build/Build AAB (keep current version)", priority = 1)]
    static void BuildOnly() => Run(bump: false, build: true);

    [MenuItem(Root + "Build/Bump Version Only", priority = 2)]
    static void BumpOnly() => Run(bump: true, build: false);

    [MenuItem(Root + "Build/Open Builds Folder", priority = 20)]
    static void OpenBuilds()
    {
        Directory.CreateDirectory(OutputDir);
        EditorUtility.RevealInFinder(Path.GetFullPath(OutputDir));
    }

    [MenuItem(Root + "Tools/Fix EventSystems for Input System (all build scenes)", priority = 40)]
    static void FixEventSystems()
    {
        if (!CanTouchScenes()) return;

        string previous = SceneManager.GetActiveScene().path;
        int fixedCount = 0;
        try
        {
            foreach (string path in BuildScenePaths())
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;

                foreach (var es in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    foreach (var old in es.GetComponents<StandaloneInputModule>())
                    {
                        UnityEngine.Object.DestroyImmediate(old, true);
                        changed = true;
                    }

                    if (es.GetComponent<InputSystemUIInputModule>() == null)
                    {
                        var module = ObjectFactory.AddComponent<InputSystemUIInputModule>(es.gameObject);
                        if (module.actionsAsset == null) module.AssignDefaultActions();
                        changed = true;
                    }
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    fixedCount++;
                }
            }
        }
        finally
        {
            ReopenScene(previous);
        }

        EditorUtility.DisplayDialog("Lost Match", $"EventSystems fixed in {fixedCount} scene(s).", "OK");
    }

    [MenuItem(Root + "Tools/Remove Missing Scripts (all build scenes)", priority = 41)]
    static void RemoveMissingScripts()
    {
        if (!CanTouchScenes()) return;

        string previous = SceneManager.GetActiveScene().path;
        int removed = 0;
        try
        {
            foreach (string path in BuildScenePaths())
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int inScene = 0;

                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        inScene += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

                if (inScene > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    removed += inScene;
                    Debug.Log($"[LostMatch] {path}: removed {inScene} missing script(s).");
                }
            }
        }
        finally
        {
            ReopenScene(previous);
        }

        EditorUtility.DisplayDialog("Lost Match", $"Removed {removed} missing script(s).", "OK");
    }

    // ───────────────────────── main flow ─────────────────────────

    static void Run(bool bump, bool build)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Lost Match", "Exit Play Mode first.", "OK");
            return;
        }

        if (build && !CheckBuildPrerequisites()) return;
        if (bump && !CanTouchScenes()) return;

        string oldVersion = PlayerSettings.bundleVersion;
        int oldCode = PlayerSettings.Android.bundleVersionCode;
        string newVersion = oldVersion;
        int newCode = oldCode;

        if (bump)
        {
            if (!TryNextVersion(oldVersion, out newVersion))
            {
                EditorUtility.DisplayDialog("Lost Match",
                    $"Cannot parse version '{oldVersion}'. Expected something like 0.0.17.", "OK");
                return;
            }
            newCode = oldCode + 1;
        }

        string what = build ? "Build AAB for Google Play" : "Bump version";
        if (!EditorUtility.DisplayDialog("Lost Match",
                $"{what}\n\nVersion: {oldVersion} → {newVersion}\nVersion code: {oldCode} → {newCode}",
                "Go", "Cancel"))
            return;

        if (bump)
        {
            PlayerSettings.bundleVersion = newVersion;
            PlayerSettings.Android.bundleVersionCode = newCode;
            int labels = UpdateVersionLabels(oldVersion, newVersion);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LostMatch] Version {oldVersion} → {newVersion}, code {oldCode} → {newCode}, labels updated in {labels} asset(s).");
        }

        if (build) BuildAab();
    }

    static bool CheckBuildPrerequisites()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUtility.DisplayDialog("Lost Match",
                "Active platform is not Android.\nSwitch it in File > Build Profiles first.", "OK");
            return false;
        }

        if (!PlayerSettings.Android.useCustomKeystore)
        {
            EditorUtility.DisplayDialog("Lost Match",
                "Custom keystore is off. Enable it in Player Settings > Publishing Settings.", "OK");
            return false;
        }

        if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass))
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("LOSTMATCH_KEYSTORE_PASS") ?? "";
        if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("LOSTMATCH_KEYALIAS_PASS") ?? "";

        if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) ||
            string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
        {
            EditorUtility.DisplayDialog("Lost Match",
                "Keystore passwords are empty.\n\nEnter them in Player Settings > Publishing Settings " +
                "(Keystore and Key passwords), then run the build again.\n\n" +
                "Nothing was changed.", "OK");
            return false;
        }

        if (BuildScenePaths().Length == 0)
        {
            EditorUtility.DisplayDialog("Lost Match", "No enabled scenes in Build Settings.", "OK");
            return false;
        }

        return true;
    }

    static void BuildAab()
    {
        EditorUserBuildSettings.buildAppBundle = true;
        Directory.CreateDirectory(OutputDir);

        string version = PlayerSettings.bundleVersion;
        int code = PlayerSettings.Android.bundleVersionCode;
        string path = $"{OutputDir}/{FilePrefix}_{version}_code{code}.aab";

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = BuildScenePaths(),
            locationPathName = path,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        });

        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[LostMatch] Build OK: {path} ({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime:hh\\:mm\\:ss})");
            EditorUtility.RevealInFinder(Path.GetFullPath(path));
            EditorUtility.DisplayDialog("Lost Match",
                $"Build succeeded.\n\n{path}\n\nUpload this .aab to Play Console.", "OK");
        }
        else
        {
            Debug.LogError($"[LostMatch] Build {summary.result}: {summary.totalErrors} error(s). See the Console.");
            EditorUtility.DisplayDialog("Lost Match",
                $"Build {summary.result} ({summary.totalErrors} error(s)).\nSee the Console.\n\n" +
                "The version was already bumped; that is fine, the next build will use the next number.", "OK");
        }
    }

    // ───────────────────────── helpers ─────────────────────────

    static string[] BuildScenePaths() =>
        EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

    static bool CanTouchScenes() => EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

    static void ReopenScene(string path)
    {
        if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    /// <summary>0.0.17 → 0.0.18 (last numeric component +1).</summary>
    static bool TryNextVersion(string current, out string next)
    {
        next = current;
        var parts = (current ?? "").Trim().Split('.');
        if (parts.Length < 2 || !int.TryParse(parts[parts.Length - 1], out int last)) return false;
        if (parts.Any(p => !int.TryParse(p, out _))) return false;

        // keep the width of the last component ("017" stays "018")
        string old = parts[parts.Length - 1];
        parts[parts.Length - 1] = (last + 1).ToString().PadLeft(old.Length, '0');
        next = string.Join(".", parts);
        return true;
    }

    /// <summary>Updates "Version x.y.z" texts in build scenes and prefabs. Returns how many assets changed.</summary>
    static int UpdateVersionLabels(string oldVersion, string newVersion)
    {
        string needle = "Version " + oldVersion;
        int changedAssets = 0;
        string previous = SceneManager.GetActiveScene().path;

        try
        {
            // Scenes: cheap text pre-check, then edit through the Unity API.
            foreach (string path in BuildScenePaths().Where(p => File.ReadAllText(p).Contains(needle)))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (var root in scene.GetRootGameObjects())
                    changed |= ReplaceLabels(root, oldVersion, newVersion);

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changedAssets++;
                }
            }

            // Prefabs
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!File.ReadAllText(path).Contains(needle)) continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (ReplaceLabels(root, oldVersion, newVersion))
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changedAssets++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }
        finally
        {
            ReopenScene(previous);
        }

        return changedAssets;
    }

    static bool ReplaceLabels(GameObject root, string oldVersion, string newVersion)
    {
        bool changed = false;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (string.IsNullOrEmpty(text.text)) continue;

            string updated = VersionLabel.Replace(text.text, m =>
                m.Value.EndsWith(oldVersion) ? m.Groups[1].Value + newVersion : m.Value);

            if (updated != text.text)
            {
                text.text = updated;
                EditorUtility.SetDirty(text);
                changed = true;
            }
        }
        return changed;
    }
}
