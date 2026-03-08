using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class GameBuilder
{
    // ─── Server Scenes (no WelcomeScreen) ────────────────────────
    private static readonly string[] ServerScenes = new string[]
    {
        "Assets/Scenes/LobbyScene.unity",
        "Assets/Scenes/SampleScene.unity"
    };

    // ─── Client Scenes (all three) ──────────────────────────────
    private static readonly string[] ClientScenes = new string[]
    {
        "Assets/Scenes/WelcomeScreen.unity",
        "Assets/Scenes/LobbyScene.unity",
        "Assets/Scenes/SampleScene.unity"
    };

    // ═════════════════════════════════════════════════════════════
    //  DEDICATED SERVER BUILDS
    // ═════════════════════════════════════════════════════════════

    [MenuItem("Build/Build Dedicated Server (Windows)")]
    public static void BuildWindowsServer()
    {
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = ServerScenes;
        options.locationPathName = "Builds/Server_Windows/MultiplayerAuthServer.exe";
        options.target = BuildTarget.StandaloneWindows64;
        options.subtarget = (int)StandaloneBuildSubtarget.Server;
        options.options = BuildOptions.None;

        // IL2CPP for server performance
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Release);

        RunBuild(options, "Windows Server");
    }

    [MenuItem("Build/Build Dedicated Server (Linux)")]
    public static void BuildLinuxServer()
    {
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = ServerScenes;
        options.locationPathName = "Builds/Server_Linux/MultiplayerAuthServer";
        options.target = BuildTarget.StandaloneLinux64;
        options.subtarget = (int)StandaloneBuildSubtarget.Server;
        options.options = BuildOptions.None;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Release);

        RunBuild(options, "Linux Server");
    }

    // ═════════════════════════════════════════════════════════════
    //  CLIENT BUILDS
    // ═════════════════════════════════════════════════════════════

    [MenuItem("Build/Build Windows Client (Development)")]
    public static void BuildWindowsDev()
    {
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = ClientScenes;
        options.locationPathName = "Builds/Windows/MultiplayerAuth.exe";
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.Development;

        RunBuild(options, "Windows Client (Dev)");
    }

    [MenuItem("Build/Build Windows Client (Release)")]
    public static void BuildWindowsRelease()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Release);

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = ClientScenes;
        options.locationPathName = "Builds/Windows_Release/MultiplayerAuth.exe";
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        RunBuild(options, "Windows Client (Release)");
    }

    // ═════════════════════════════════════════════════════════════
    //  SHARED
    // ═════════════════════════════════════════════════════════════

    private static void RunBuild(BuildPlayerOptions options, string label)
    {
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"<color=green>[Build]</color> {label} succeeded — {summary.totalSize / (1024 * 1024)} MB");
        else
            Debug.LogError($"<color=red>[Build]</color> {label} failed!");
    }
}
