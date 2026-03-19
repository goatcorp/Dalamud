using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Nuke.Common.IO;
using Nuke.Common.Utilities;

/// <summary>
/// Bare minimum .vcxproj to CMakeLists generator.
/// </summary>
public sealed class VCProjToCMakeLists
{
    readonly ICMakePaths Paths;

    readonly Project VCProj;

    readonly string Name;

    readonly string TargetName;

    readonly ConfigurationType Type;

    readonly string CMakeLists;

    readonly List<ClCompile> ClCompiles = [];

    AbsolutePath OwnDirectory => Paths.ProjectsDirectory / Name;

    AbsolutePath CMakeListsPath => OwnDirectory / "CMakeLists.txt";

    public VCProjToCMakeLists(ICMakePaths paths, Project proj)
    {
        Paths = paths;
        VCProj = proj;

        Name = VCProj.GetPropertyValue("MSBuildProjectName");
        Type = Enum.Parse<ConfigurationType>(VCProj.GetPropertyValue("ConfigurationType"));

        TargetName = Type switch
        {
            ConfigurationType.Application => $"{Name}.exe",
            ConfigurationType.DynamicLibrary => $"{Name}.dll",
            ConfigurationType.StaticLibrary => $"{Name}.lib",
            _ => throw new NotSupportedException(),
        };

        foreach (var item in VCProj.Items)
        {
            switch (item.ItemType)
            {
                case "ClCompile":
                    ClCompiles.Add(new ClCompile(item));
                break;
            }
        }
    }

    public VCProjToCMakeLists(ICMakePaths paths, AbsolutePath path, Configuration config)
        : this(paths, ParseMSBuildProject(paths, path, config))
    {
    }

    public AbsolutePath Gen()
    {
        #region Header
        var lists = new StringBuilder($@"# Auto-generated, changes will be overwritten by NUKE
cmake_minimum_required(VERSION 3.31)
project({Name})

include({Paths.CMakeToolchain.ToString().DoubleQuoteIfNeeded()})

");
        #endregion

        #region add_
        lists.AppendLine(Type switch
        {
            ConfigurationType.Application => $"add_executable({TargetName}",
            ConfigurationType.DynamicLibrary => $"add_library({TargetName} SHARED",
            ConfigurationType.StaticLibrary => $"add_library({TargetName} STATIC",
            _ => throw new NotSupportedException(),
        });

        foreach (var item in ClCompiles)
        {
            lists.AppendLine(item.Path.DoubleQuoteIfNeeded());
        }

        lists.AppendLine(")");
        #endregion

        #region set_property
        foreach (var item in ClCompiles)
        {
            lists.AppendLine($"set_property(SOURCE {item.Path.DoubleQuoteIfNeeded()} PROPERTY");
            lists.AppendLine($"COMPILE_DEFINITIONS {item.Definitions}");
            lists.AppendLine($"C_STANDARD {item.StandardC}");
            lists.AppendLine($"CXX_STANDARD {item.StandardCXX}");
            lists.AppendLine(")");
        }
        #endregion

        OwnDirectory.CreateDirectory();
        CMakeListsPath.WriteAllText(lists.ToString());

        return OwnDirectory;
    }

    public void Clean()
    {
        OwnDirectory.DeleteDirectory();
    }

    private static Project ParseMSBuildProject(ICMakePaths paths, AbsolutePath path, Configuration config)
    {
        var collection = new ProjectCollection();
        return Project.FromProjectRootElement(
            ProjectRootElement.Open(path, collection, preserveFormatting: true),
            new ProjectOptions
            {
                GlobalProperties = new Dictionary<string, string>()
                {
                    { "Configuration", config },
                    { "Platform", "x64" },
                    { "IsMinGW", "true" },
                    { "VCTargetsPath", paths.VCStubDirectory }
                },
                ToolsVersion = collection.DefaultToolsVersion,
                ProjectCollection = collection
            });
    }

    private static AbsolutePath ResolvePath(Project proj, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        AbsolutePath projPath = proj.FullPath;
        return projPath.Parent / path;
    }

    class ClCompile(ProjectItem item)
    {
        public readonly string Path = ResolvePath(item.Project, item.EvaluatedInclude);
        public readonly string Definitions = item.GetMetadataValue("PreprocessorDefinitions");
        public readonly string StandardC = item.GetMetadataValue("LanguageStandard_C");
        public readonly string StandardCXX = item.GetMetadataValue("LanguageStandard");
    }

    enum ConfigurationType
    {
        Application,
        DynamicLibrary,
        StaticLibrary
    }
}
