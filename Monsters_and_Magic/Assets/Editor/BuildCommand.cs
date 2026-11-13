using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;

public static class BuildCommand
{
    public static void BuildAndroid()
    {
        var output = GetArgument("-buildOutput",
            "Builds/Android/Monsters_and_Magic.apk");

        Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");

        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARM64;

        EditorUserBuildSettings.buildAppBundle =
            output.EndsWith(".aab", StringComparison.OrdinalIgnoreCase);

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Android build failed");
    }

    private static string GetArgument(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, name);

        return index >= 0 && index + 1 < args.Length
            ? args[index + 1]
            : fallback;
    }
}
