using System.Collections.Generic;
using System.IO;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

[UnsetVisualStudioEnvironmentVariables]
public class DalamudBuild : NukeBuild
{
    /// Support plugins are available for:
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<DalamudBuild>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] Solution Solution;
    [GitRepository] GitRepository GitRepository;

    AbsolutePath DalamudProjectDir => RootDirectory / "Dalamud";
    AbsolutePath DalamudProjectFile => DalamudProjectDir / "Dalamud.csproj";

    AbsolutePath DalamudBootProjectDir => RootDirectory / "Dalamud.Boot";
    AbsolutePath DalamudBootProjectFile => DalamudBootProjectDir / "Dalamud.Boot.vcxproj";
    
    AbsolutePath DalamudCrashHandlerProjectDir => RootDirectory / "DalamudCrashHandler";
    AbsolutePath DalamudCrashHandlerProjectFile => DalamudCrashHandlerProjectDir / "DalamudCrashHandler.vcxproj";

    AbsolutePath InjectorProjectDir => RootDirectory / "Dalamud.Injector";
    AbsolutePath InjectorProjectFile => InjectorProjectDir / "Dalamud.Injector.csproj";

    AbsolutePath InjectorBootProjectDir => RootDirectory / "Dalamud.Injector.Boot";
    AbsolutePath InjectorBootProjectFile => InjectorBootProjectDir / "Dalamud.Injector.Boot.vcxproj";

    AbsolutePath TestProjectDir => RootDirectory / "Dalamud.Test";
    AbsolutePath TestProjectFile => TestProjectDir / "Dalamud.Test.csproj";

    AbsolutePath ArtifactsDirectory => RootDirectory / "bin" / Configuration;

    private static AbsolutePath LibraryDirectory => RootDirectory / "lib";

    private static Dictionary<string, string> EnvironmentVariables => new(EnvironmentInfo.Variables);

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target CompileDalamud => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(DalamudProjectFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target CompileDalamudBoot => _ => _
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(DalamudBootProjectFile)
                .SetConfiguration(Configuration));
        });
    
    Target CompileDalamudCrashHandler => _ => _
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                                      .SetTargetPath(DalamudCrashHandlerProjectFile)
                                      .SetConfiguration(Configuration));
        });

    Target CompileInjector => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(InjectorProjectFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target CompileInjectorBoot => _ => _
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(InjectorBootProjectFile)
                .SetConfiguration(Configuration));
        });

    Target Compile => _ => _
        .DependsOn(CompileDalamud)
        .DependsOn(CompileDalamudBoot)
        .DependsOn(CompileDalamudCrashHandler)
        .DependsOn(CompileInjector)
        .DependsOn(CompileInjectorBoot);

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(TestProjectFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(s => s
                .SetProject(DalamudProjectFile)
                .SetConfiguration(Configuration));

            MSBuildTasks.MSBuild(s => s
                .SetProjectFile(DalamudBootProjectFile)
                .SetConfiguration(Configuration)
                .SetTargets("Clean"));
            
            MSBuildTasks.MSBuild(s => s
                .SetProjectFile(DalamudCrashHandlerProjectFile)
                .SetConfiguration(Configuration)
                .SetTargets("Clean"));

            DotNetTasks.DotNetClean(s => s
                .SetProject(InjectorProjectFile)
                .SetConfiguration(Configuration));

            MSBuildTasks.MSBuild(s => s
                .SetProjectFile(InjectorBootProjectFile)
                .SetConfiguration(Configuration)
                .SetTargets("Clean"));

            FileSystemTasks.DeleteDirectory(ArtifactsDirectory);
            Directory.CreateDirectory(ArtifactsDirectory);
        });
}
