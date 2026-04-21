#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PacManBuild
{
    const string ProductName = "Pac-Man Settings";
    const string ApplicationId = "com.couleslaw.pacmansettings";
    const string WindowsOutput = "Builds/Windows/PacManSettings/PacManSettings.exe";
    const string AndroidOutput = "Builds/Android/PacManSettings.apk";

    [MenuItem("Build/Pac-Man Settings/Build Windows 64-bit")]
    public static void BuildWindows()
    {
        PreparePlayerSettings();
        BuildPlayer(BuildTarget.StandaloneWindows64, WindowsOutput);
    }

    [MenuItem("Build/Pac-Man Settings/Build Android APK")]
    public static void BuildAndroidApk()
    {
        PreparePlayerSettings();
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        BuildPlayer(BuildTarget.Android, AndroidOutput);
    }

    [MenuItem("Build/Pac-Man Settings/Build Windows And APK")]
    public static void BuildAll()
    {
        BuildWindows();
        BuildAndroidApk();
    }

    static void PreparePlayerSettings()
    {
        PlayerSettings.companyName = "Couleslaw";
        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = "1.1.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, ApplicationId);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, ApplicationId);
        PlayerSettings.Android.bundleVersionCode = 2;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        EditorBuildSettings.scenes = BuildScenes();
    }

    static EditorBuildSettingsScene[] BuildScenes()
    {
        return new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/StartScreen.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };
    }

    static string[] EnabledScenePaths()
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        if (scenes.Count == 0)
            throw new InvalidOperationException("No enabled scenes were found for the build.");

        return scenes.ToArray();
    }

    static void BuildPlayer(BuildTarget target, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EnabledScenePaths(),
            target = target,
            locationPathName = outputPath,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception(target + " build failed: " + report.summary.result);

        EnsureWindowsLauncher(target, outputPath);
    }

    static void EnsureWindowsLauncher(BuildTarget target, string outputPath)
    {
        if (target != BuildTarget.StandaloneWindows64 || File.Exists(outputPath))
            return;

        string playerTemplate = Path.Combine(
            EditorApplication.applicationContentsPath,
            "PlaybackEngines",
            "windowsstandalonesupport",
            "Variations",
            "win64_player_nondevelopment_mono",
            "WindowsPlayer.exe");

        if (!File.Exists(playerTemplate))
            throw new FileNotFoundException("Windows player launcher was not found after build.", playerTemplate);

        File.Copy(playerTemplate, outputPath, overwrite: true);
        Debug.Log("Restored missing Windows launcher: " + outputPath);
    }
}
#endif
