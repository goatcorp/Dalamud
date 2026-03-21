using System;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities;
using Serilog;

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

    private static Dictionary<string, string> EnvironmentVariables => new(EnvironmentInfo.Variables);

    private static string ConsoleTemplate => "{Message:l}{NewLine}{Exception}";
    private static bool IsCIBuild => Environment.GetEnvironmentVariable("CI") == "true";

    [PathVariable]
    readonly Tool CMake;

    DefaultCMakePaths CMakePaths => new(RootDirectory, CMake);

    IVCProjExec MSBuild => EnvironmentInfo.IsWin ? new VCProjMSBuildExec() : new VCProjToCMakeExec(CMakePaths);

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target CompileCImGui => _ => _
        .Executes(() =>
        {
            // Not necessary
            if (IsDocsBuild)
                return;
            
            MSBuild.Build(CImGuiProjectFile, Configuration);
        });

    Target CompileCImPlot => _ => _
        .Executes(() =>
        {
            // Not necessary
            if (IsDocsBuild)
                return;
            
            MSBuild.Build(CImPlotProjectFile, Configuration);
        });

    Target CompileCImGuizmo => _ => _
        .Executes(() =>
        {
            // Not necessary
            if (IsDocsBuild)
                return;
            
            MSBuild.Build(CImGuizmoProjectFile, Configuration);
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
                       .EnableNoRestore();
                if (IsCIBuild)
                {
                    s = s
                        .SetProcessAdditionalArguments("/clp:NoSummary"); // Disable MSBuild summary on CI builds
                }
                // We need to emit compiler generated files for the docs build, since docfx can't run generators directly
                // TODO: This fails every build after this because of redefinitions...

                // if (IsDocsBuild)
                // { 
                //     Log.Warning("Building for documentation, emitting compiler generated files. This can cause issues on Windows due to path-length limitations");
                //     s = s
                //         .SetProperty("IsDocsBuild", "true");
                // }
                return s;
            });
        });

    Target CompileDalamudBoot => _ => _
        .DependsOn(PrepareJWasm)
        .Executes(() =>
        {
            MSBuild.Build(DalamudBootProjectFile, Configuration);
        });
    
    Target CompileDalamudCrashHandler => _ => _
        .Executes(() =>
        {
            MSBuild.Build(DalamudCrashHandlerProjectFile, Configuration);
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

    Target SetCILogging => _ => _
        .DependentFor(Compile)
        .OnlyWhenStatic(() => IsCIBuild)
        .Executes(() =>
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: ConsoleTemplate)
                .CreateLogger();
        });

    Target Compile => _ => _
    .DependsOn(CompileDalamud)
    .DependsOn(CompileDalamudBoot)
    .DependsOn(CompileDalamudCrashHandler)
    .DependsOn(CompileInjector)
    ;

    Target CI => _ => _
        .DependsOn(Compile)
        .Triggers(Test);

    Target Test => _ => _
        .DependsOn(Compile)
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
            MSBuild.Clean(CImGuiProjectFile, Configuration);

            MSBuild.Clean(CImPlotProjectFile, Configuration);

            MSBuild.Clean(CImGuizmoProjectFile, Configuration);

            DotNetTasks.DotNetClean(s => s
                .SetProject(DalamudProjectFile)
                .SetConfiguration(Configuration));

            MSBuild.Clean(DalamudBootProjectFile, Configuration);
            
            MSBuild.Clean(DalamudCrashHandlerProjectFile, Configuration);

            DotNetTasks.DotNetClean(s => s
                .SetProject(InjectorProjectFile)
                .SetConfiguration(Configuration));

            ArtifactsDirectory.CreateOrCleanDirectory();

            CMakePaths.JWasmBuildDirectory.DeleteDirectory();
        });

    Target PrepareJWasm => _ => _
        .Executes(() =>
        {
            if (EnvironmentInfo.IsWin)
                return [];

            var buildDir = CMakePaths.JWasmBuildDirectory;
            buildDir.CreateDirectory();

            List<Output> outputs = [];

            outputs.AddRange(CMake(
                $"-S{CMakePaths.JWasmSrcDirectory.ToString().SingleQuoteIfNeeded()} " +
                $"-B{CMakePaths.JWasmBuildDirectory.ToString().SingleQuoteIfNeeded()} " +
                // Fails to build with C++23 onwards.
                "-DCMAKE_C_STANDARD=17 -DCMAKE_CXX_STANDARD=17 " +
                // The min ver is ancient and NUKE picks up CMake's warnings about that as errors.
                "-Wno-deprecated"));

            outputs.AddRange(CMake(
                $"--build {CMakePaths.JWasmBuildDirectory.ToString().SingleQuoteIfNeeded()}"));

            Assert.FileExists(CMakePaths.JWasmTool);

            return outputs;
        });
}
