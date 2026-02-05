using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class GameBuilder
{
    [MenuItem("Build/Build Windows Client (Development)")]
    public static void BuildWindowsDev()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[]
        {
            "Assets/Scenes/WelcomeScreen.unity",
            "Assets/Scenes/LobbyScene.unity",
            "Assets/Scenes/SampleScene.unity"
        };
        buildPlayerOptions.locationPathName = "Builds/Windows/MultiplayerAuth.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.Development;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }

    [MenuItem("Build/Build Windows Client (Release)")]
    public static void BuildWindowsRelease()
    {
        // Switch to IL2CPP for Release (Cleaner, Faster, Secure)
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        
        // Optional: Compiler Configuration to Master (optimized)
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Release);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[]
        {
            "Assets/Scenes/WelcomeScreen.unity",
            "Assets/Scenes/LobbyScene.unity",
            "Assets/Scenes/SampleScene.unity"
        };
        buildPlayerOptions.locationPathName = "Builds/Windows_Release/MultiplayerAuth.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }
}
