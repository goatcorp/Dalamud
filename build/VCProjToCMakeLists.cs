using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Namotion.Reflection;
using Nuke.Common.IO;
using Nuke.Common.Utilities;

/// <summary>
/// Bare minimum .vcxproj to CMakeLists generator.
/// </summary>
public sealed class VCProjToCMakeLists
{
    const string CMakeVersion = "3.31";
    const string StandardCLatest = "23";
    const string StandardCXXLatest = "23";

    readonly ICMakePaths Paths;

    readonly Project VCProj;

    readonly string Name;

    readonly string TargetName;

    readonly ConfigurationType Type;

    readonly string SubSystem;

    readonly string CMakeLists;

    readonly List<Compile> Compiles = [];

    readonly List<string> LinkDeps = [];

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
            Compile compile = item.ItemType switch
            {
                "ClCompile" => new ClCompile(item),
                "ResourceCompile" => new ResourceCompile(item),
                _ => null
            };

            if (compile is not null)
            {
                Compiles.Add(compile);
            }
        }

        if (VCProj.ItemDefinitions.TryGetValue("Link", out var link))
        {
            SubSystem = link.GetMetadataValue("SubSystem");

            foreach (var dep in link.GetMetadataValue("AdditionalDependencies").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                LinkDeps.Add(dep.TrimEnd(".lib"));
            }
        }

        if (Type == ConfigurationType.Application && string.IsNullOrEmpty(SubSystem))
        {
            SubSystem = "Console";
        }
    }

    public VCProjToCMakeLists(ICMakePaths paths, AbsolutePath path, Configuration config)
        : this(paths, ParseMSBuildProject(paths, path, config))
    {
    }

    public AbsolutePath Gen()
    {
        #region Header and global props
        var lists = new StringBuilder($@"# Auto-generated, changes will be overwritten by NUKE
cmake_minimum_required(VERSION {CMakeVersion})
project({Name})

include({Paths.CMakeToolchain.ToString().DoubleQuoteIfNeeded()})

");

        if (VCProj.GetPropertyValue("CharacterSet").EqualsOrdinalIgnoreCase("Unicode"))
        {
            lists.AppendLine("add_compile_definitions(_UNICODE UNICODE)");
            lists.AppendLine();
        }

        #endregion

        #region Source -> Objects
        foreach (var compile in Compiles)
        {
            lists.AppendLine($"add_library({compile.Key} OBJECT {compile.Path.ToString().DoubleQuoteIfNeeded()})");

            var text = compile.Gen();
            if (!string.IsNullOrEmpty(text))
            {
                lists.Append(text);
            }

            lists.AppendLine();
        }
        #endregion

        #region Objects -> Target
        lists.AppendLine(Type switch
        {
            ConfigurationType.Application => $"add_executable({TargetName}",
            ConfigurationType.DynamicLibrary => $"add_library({TargetName} SHARED",
            ConfigurationType.StaticLibrary => $"add_library({TargetName} STATIC",
            _ => throw new NotSupportedException(),
        });

        foreach (var compile in Compiles)
        {
            lists.AppendLine($"$<TARGET_OBJECTS:{compile.Key}>");
        }

        lists.AppendLine(")");
        lists.AppendLine();
        #endregion

        #region Target linker props
        if (Type == ConfigurationType.Application)
        {
            lists.AppendLine($"target_link_options({TargetName} PRIVATE -m{SubSystem.ToLowerInvariant()})");
            lists.AppendLine();
        }

        if (LinkDeps.Count != 0)
        {
            lists.AppendLine($"target_link_libraries({TargetName}");

            foreach (var dep in LinkDeps)
            {
                lists.AppendLine($"PRIVATE {dep}");
            }

            lists.AppendLine(")");
            lists.AppendLine();
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
                    { "VCTargetsPath", paths.VCStubDirectory },
                    { "CoreLibraryDependencies", "kernel32.lib;user32.lib;gdi32.lib;winspool.lib;comdlg32.lib;advapi32.lib;shell32.lib;ole32.lib;oleaut32.lib;uuid.lib;odbc32.lib;odbccp32.lib" }
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

    enum ConfigurationType
    {
        Application,
        DynamicLibrary,
        StaticLibrary
    }

    abstract class Compile
    {
        public readonly AbsolutePath Path;

        public readonly string Key;

        public Compile(ProjectItem item)
        {
            Path = ResolvePath(item.Project, item.EvaluatedInclude);
            Key = Path.NameWithoutExtension + "_" + item.UnevaluatedInclude.ToString().GetSHA256Hash();
        }

        public virtual string Gen()
            => "";
    }

    sealed class ClCompile(ProjectItem item) : Compile(item)
    {
        public readonly string Definitions = item.GetMetadataValue("PreprocessorDefinitions");

        public readonly string StandardC =
            item.GetMetadataValue("LanguageStandard_C")
            .TrimStart("stdc")
            .Replace("latest", StandardCLatest);

        public readonly string StandardCXX =
            item.GetMetadataValue("LanguageStandard")
            .TrimStart("stdcpp")
            .Replace("latest", StandardCXXLatest);

        public override string Gen()
            => $@"
set_target_properties({Key} PROPERTIES
    COMPILE_DEFINITIONS {Definitions}
    C_STANDARD {StandardC}
    CXX_STANDARD {StandardCXX}
)
";
    }

    sealed class ResourceCompile(ProjectItem item) : Compile(item)
    {
        public readonly string Definitions = item.GetMetadataValue("PreprocessorDefinitions");
        public readonly string StandardC = item.GetMetadataValue("LanguageStandard_C");
        public readonly string StandardCXX = item.GetMetadataValue("LanguageStandard");
    }
}
