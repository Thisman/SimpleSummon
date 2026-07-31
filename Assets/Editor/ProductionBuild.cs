using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

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
        string buildVersion = GetArgument("-buildVersion");
        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
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
            options = BuildOptions.CompressWithLz4HC | BuildOptions.CleanBuildCache
        };

        bool usedDefaultGraphicsApis =
            PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
        GraphicsDeviceType[] graphicsApis =
            PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
        string originalVersion = PlayerSettings.bundleVersion;
        BuildReport report;
        try
        {
            if (!string.IsNullOrWhiteSpace(buildVersion))
            {
                PlayerSettings.bundleVersion = buildVersion;
            }

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11 });
            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            PlayerSettings.bundleVersion = originalVersion;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, graphicsApis);
            PlayerSettings.SetUseDefaultGraphicsAPIs(
                BuildTarget.StandaloneWindows64,
                usedDefaultGraphicsApis);
        }
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
        string value = GetArgument("-buildOutput");
        return string.IsNullOrWhiteSpace(value) ? DefaultOutputPath : value;
    }

    private static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
