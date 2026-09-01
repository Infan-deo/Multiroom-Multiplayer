using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MansionEscape3D.Editor
{
    /// <summary>
    /// Custom buttons for Unity 6.3+ Main Toolbar.
    /// This uses Unity's supported MainToolbarElement API instead of
    /// injecting VisualElements into UnityEditor.Toolbar through reflection.
    /// </summary>
    public static class EditorCustomToolbar
    {
        private const string ClearSaveDataId = "MansionEscape3D/ClearSaveData";
        private const string OpenProjectFolderId = "MansionEscape3D/OpenProjectFolder";
        private const string OpenVersionControlId = "MansionEscape3D/OpenVersionControl";
        private const string OpenScriptIDEId = "MansionEscape3D/OpenScriptIDE";
        private const string OpenSceneId = "MansionEscape3D/OpenScene";
        private const string PlayFromStartId = "MansionEscape3D/PlayFromStart";

        private static bool _isCustomPlayMode;

        // --------------------------------------------------------------------
        // LEFT
        // --------------------------------------------------------------------

        [MainToolbarElement(
            ClearSaveDataId,
            defaultDockPosition = MainToolbarDockPosition.Left,
            defaultDockIndex = 0)]
        public static MainToolbarElement ClearSaveDataButton()
        {
            Texture2D icon = GetIcon("Cancel");
            var content = new MainToolbarContent(icon, "Reset Save Data");

            return new MainToolbarButton(content, ClearSaveData);
        }

        [MainToolbarElement(
            OpenProjectFolderId,
            defaultDockPosition = MainToolbarDockPosition.Left,
            defaultDockIndex = 1)]
        public static MainToolbarElement OpenProjectFolderButton()
        {
            Texture2D icon = GetIcon("Folder Icon");
            var content = new MainToolbarContent(icon, "Open Project Folder");

            return new MainToolbarButton(content, OpenProjectFolder);
        }

        [MainToolbarElement(
            OpenVersionControlId,
            defaultDockPosition = MainToolbarDockPosition.Left,
            defaultDockIndex = 2)]
        public static MainToolbarElement OpenVersionControlButton()
        {
            Texture2D icon = GetIcon("UnityEditor.VersionControl");
            var content = new MainToolbarContent(icon, "Open Version Control");

            return new MainToolbarButton(content, OpenVersionControl);
        }

        [MainToolbarElement(
            OpenScriptIDEId,
            defaultDockPosition = MainToolbarDockPosition.Left,
            defaultDockIndex = 3)]
        public static MainToolbarElement OpenScriptIDEButton()
        {
            Texture2D icon = GetIcon("cs Script Icon");
            var content = new MainToolbarContent(icon, "Open C# Project");

            return new MainToolbarButton(content, OpenScriptIDE);
        }

        // --------------------------------------------------------------------
        // MIDDLE
        // --------------------------------------------------------------------

        [MainToolbarElement(
            OpenSceneId,
            defaultDockPosition = MainToolbarDockPosition.Middle,
            defaultDockIndex = 0)]
        public static MainToolbarElement OpenSceneButton()
        {
            Texture2D icon = GetIcon("Scene");
            var content = new MainToolbarContent(icon, "Open Scene");

            return new MainToolbarDropdown(content, ShowSceneMenu);
        }

        // --------------------------------------------------------------------
        // RIGHT
        // --------------------------------------------------------------------

        [MainToolbarElement(
            PlayFromStartId,
            defaultDockPosition = MainToolbarDockPosition.Right,
            defaultDockIndex = 0)]
        public static MainToolbarElement PlayFromStartButton()
        {
            Texture2D icon = GetIcon("Animation Icon");
            var content = new MainToolbarContent(icon, "Play From Start");

            return new MainToolbarButton(content, PlayFromStart);
        }

        // --------------------------------------------------------------------
        // Button actions
        // --------------------------------------------------------------------

        private static void ClearSaveData()
        {
            // SaveManager.DeleteSaveDataFiles();
            // SaveGame.DeleteAll();
        }

        private static void OpenProjectFolder()
        {
            EditorUtility.RevealInFinder(Application.dataPath);
        }

        private static void OpenVersionControl()
        {
            string githubPath =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                @"\GitHubDesktop\GitHubDesktop.exe";

            if (File.Exists(githubPath))
            {
                Process.Start(githubPath);
            }
        }

        private static void OpenScriptIDE()
        {
            EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        }

        private static void PlayFromStart()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (EditorBuildSettings.scenes.Length == 0)
            {
                Debug.LogWarning("Play From Start: no scenes are configured in Build Settings.");
                return;
            }

            _isCustomPlayMode = true;
            EditorApplication.EnterPlaymode();
        }

        // --------------------------------------------------------------------
        // Scene menu
        // --------------------------------------------------------------------

        private static void ShowSceneMenu(Rect dropdownRect)
        {
            var menu = new GenericMenu();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // First: scenes from Build Settings.
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(buildScene.path))
                    continue;

                AddSceneMenuItem(menu, buildScene.path, addedPaths);
            }

            // Then: every .unity scene under Assets/Scenes.
            const string scenesRoot = "Assets/Scenes";

            if (Directory.Exists(scenesRoot))
            {
                string[] scenePaths =
                    Directory.GetFiles(scenesRoot, "*.unity", SearchOption.AllDirectories);

                Array.Sort(scenePaths, StringComparer.OrdinalIgnoreCase);

                foreach (string scenePath in scenePaths)
                {
                    AddSceneMenuItem(menu, scenePath.Replace('\\', '/'), addedPaths);
                }
            }

            if (addedPaths.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes found"));
            }

            // MainToolbarDropdown gives us the actual toolbar button rectangle.
            menu.DropDown(dropdownRect);
        }

        private static void AddSceneMenuItem(
            GenericMenu menu,
            string scenePath,
            HashSet<string> addedPaths)
        {
            if (!addedPaths.Add(scenePath))
                return;

            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            menu.AddItem(
                new GUIContent(sceneName),
                false,
                () => OpenScene(scenePath));
        }

        private static void OpenScene(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Debug.Log($"Opening scene: {scenePath}");
            EditorSceneManager.OpenScene(scenePath);
        }

        // --------------------------------------------------------------------
        // Play Mode
        // --------------------------------------------------------------------

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    if (_isCustomPlayMode &&
                        EditorBuildSettings.scenes.Length > 0)
                    {
                        string firstScenePath = EditorBuildSettings.scenes[0].path;

                        EditorSceneManager.playModeStartScene =
                            AssetDatabase.LoadAssetAtPath<SceneAsset>(firstScenePath);
                    }
                    else
                    {
                        EditorSceneManager.playModeStartScene = null;
                    }

                    break;

                case PlayModeStateChange.EnteredEditMode:
                    _isCustomPlayMode = false;
                    EditorSceneManager.playModeStartScene = null;
                    break;
            }

            // MainToolbarButton does not expose VisualElement.SetEnabled().
            // The click handler itself prevents Play From Start during Play Mode.
        }

        private static Texture2D GetIcon(string iconName)
        {
            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }
    }
}
