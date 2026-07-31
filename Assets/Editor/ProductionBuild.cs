using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class ProductionBuild
{
    private const string DefaultOutputPath = "Builds/Windows/SimpleSummon.exe";

    [MenuItem("Build/Production/Windows x64")]
    public static void BuildWindowsFromMenu()
    {
        BuildWindows();
    }

    public static void BuildWindows()
    {
        string outputPath = GetOutputPath();
        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in Editor Build Settings.");
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Production build failed: {summary.result}, errors: {summary.totalErrors}.");
        }

        Console.WriteLine(
            $"Production build succeeded: {Path.GetFullPath(outputPath)} ({summary.totalSize} bytes).");
    }

    private static string GetOutputPath()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], "-buildOutput", StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return DefaultOutputPath;
    }
}
