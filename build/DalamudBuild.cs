using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Flurl;
using Flurl.Http;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Logger;
using System.Threading.Tasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class DalamudBuild : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<DalamudBuild>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath OutputDirectory => RootDirectory / "output";

    AbsolutePath DefaultInstallDirectory => (AbsolutePath)Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) / "Dalamud" / "bin" / GitVersion.SemVer;

    AbsolutePath CoreHookBinaryDirectory => RootDirectory / "lib" / "CoreHookBinary";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });

    Target DownloadCoreHookBinary => _ => _
        .Executes(() =>
        {
            // Download pre-compiled binary of CoreHook.Hooking and CoreLoad (these are prerequisite)
            var corehookTask = ExtractZip($"https://github.com/unknownv2/CoreHook.Hooking/releases/download/1.0.9/corehook-{Configuration}-x64.zip", CoreHookBinaryDirectory);
            var coreloadTask = ExtractZip($"https://github.com/unknownv2/CoreHook.Host/releases/download/2.0.5/coreload-{Configuration}-x64.zip", CoreHookBinaryDirectory);

            Task.WaitAll(corehookTask, coreloadTask);
        });

    Target Install => _ => _
        .DependsOn(DownloadCoreHookBinary) // prolly can be skipped if prerequisites are already there?
        .DependsOn(Compile)
        .Executes(() =>
        {
            Info($"Installing Dalamud to {DefaultInstallDirectory}");
            // TODO
        });

    async Task ExtractZip(string uri, string directory)
    {
        EnsureExistingDirectory(directory);

        Info($"Downloading {uri}");

        var tempDir = Path.GetTempPath();
        var zipFilePath = await uri.DownloadFileAsync(tempDir);

        Info($"Extracting {zipFilePath}");
        ZipFile.ExtractToDirectory(zipFilePath, directory);
    }
}
