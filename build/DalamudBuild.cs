using System.Collections.Generic;
using System.IO;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Serilog;
using Serilog.Events;

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

    [Parameter("Whether we are building for documentation - emits generated files")]
    readonly bool IsDocsBuild = false;

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

    AbsolutePath ExternalsDir => RootDirectory / "external";
    AbsolutePath CImGuiDir => ExternalsDir / "cimgui";
    AbsolutePath CImGuiProjectFile => CImGuiDir / "cimgui.vcxproj";
    AbsolutePath CImPlotDir => ExternalsDir / "cimplot";
    AbsolutePath CImPlotProjectFile => CImPlotDir / "cimplot.vcxproj";
    AbsolutePath CImGuizmoDir => ExternalsDir / "cimguizmo";
    AbsolutePath CImGuizmoProjectFile => CImGuizmoDir / "cimguizmo.vcxproj";

    AbsolutePath ArtifactsDirectory => RootDirectory / "bin" / Configuration;

    private static AbsolutePath LibraryDirectory => RootDirectory / "lib";

    private static Dictionary<string, string> EnvironmentVariables => new(EnvironmentInfo.Variables);

    private static string ConsoleTemplate => "{Message:l}{NewLine}{Exception}";

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target CompileCImGui => _ => _
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(CImGuiProjectFile)
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.x64));
        });

    Target CompileCImPlot => _ => _
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(CImPlotProjectFile)
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.x64));
        });

    Target CompileCImGuizmo => _ => _
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(CImGuizmoProjectFile)
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.x64));
        });

    Target CompileImGuiNatives => _ => _
        .DependsOn(CompileCImGui)
        .DependsOn(CompileCImPlot)
        .DependsOn(CompileCImGuizmo);

    Target CompileDalamud => _ => _
        .DependsOn(Restore)
        .DependsOn(CompileImGuiNatives)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s =>
            {
                s = s
                       .SetProjectFile(DalamudProjectFile)
                       .SetConfiguration(Configuration)
                       .EnableNoRestore()
                       .SetProcessArgumentConfigurator(a => a.Add("/clp:NoSummary"));

                // We need to emit compiler generated files for the docs build, since docfx can't run generators directly
                // TODO: This fails every build after this because of redefinitions...
                if (IsDocsBuild)
                { 
                    Log.Warning("Building for documentation, emitting compiler generated files. This can cause issues on Windows due to path-length limitations");
                    s = s
                        .SetProperty("IsDocsBuild", "true");
                }

                return s;
            });
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

    Target SetCustomLogging => _ => _
    .Executes(() =>
    {
        var config = new LoggerConfiguration().WriteTo.Console(outputTemplate: ConsoleTemplate, restrictedToMinimumLevel: LogEventLevel.Debug).ConfigureFiles(this);
        Log.Logger = config.CreateLogger();
    });

    Target Compile => _ => _
        .DependsOn(SetCustomLogging)
        .DependsOn(CompileDalamud)
        .DependsOn(CompileDalamudBoot)
        .DependsOn(CompileDalamudCrashHandler)
        .DependsOn(CompileInjector)
        .DependsOn(CompileInjectorBoot);

    Target Test => _ => _
        .TriggeredBy(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(TestProjectFile)
                .SetConfiguration(Configuration)
                .AddProperty("WarningLevel", "0")
                .EnableNoRestore());
        });

    Target Clean => _ => _
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetProjectFile(CImGuiProjectFile)
                .SetConfiguration(Configuration)
                .SetTargets("Clean"));

            MSBuildTasks.MSBuild(s => s
                .SetProjectFile(CImPlotProjectFile)
                .SetConfiguration(Configuration)
                .SetTargets("Clean"));

            MSBuildTasks.MSBuild(s => s
                .SetProjectFile(CImGuizmoProjectFile)
                .SetConfiguration(Configuration)
                .SetTargets("Clean"));

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
