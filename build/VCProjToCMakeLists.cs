using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Nuke.Common;
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

    readonly AbsolutePath OutDir;

    readonly string SubSystem;

    readonly string LinkOptions;

    readonly ClCompile PCH;

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
        OutDir = ResolvePath(VCProj, VCProj.GetPropertyValue("OutDir"));

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
                "ClCompile" => new ClCompile(this, item),
                "ResourceCompile" => new ResourceCompile(this, item),
                _ => null
            };

            if (compile is null || compile.Excluded)
            {
                continue;
            }

            if (compile as ClCompile is { PCH: PrecompiledHeader.Create } pch)
            {
                Assert.Equals(PCH, null);
                PCH = pch;
                continue;
            }

            Compiles.Add(compile);
        }

        if (VCProj.ItemDefinitions.TryGetValue("Link", out var link))
        {
            SubSystem = link.GetMetadataValue("SubSystem");
            LinkOptions = link.GetMetadataValue("AdditionalOptions");

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
        foreach (var compile in new Compile[] { PCH }.Concat(Compiles))
        {
            compile?.Gen(lists);
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

        #region Target props
        if (Type == ConfigurationType.Application)
        {
            lists.AppendLine($"target_link_options({TargetName} PRIVATE -m{SubSystem.ToLowerInvariant()})");
            lists.AppendLine();
        }

        if (!string.IsNullOrEmpty(LinkOptions))
        {
            lists.AppendLine($"target_link_options({TargetName} PRIVATE {LinkOptions})");
            lists.AppendLine();
        }

        if (LinkDeps.Count != 0)
        {
            lists.AppendLine($"target_link_libraries({TargetName} PRIVATE");

            foreach (var dep in LinkDeps)
            {
                lists.AppendLine(dep);
            }

            lists.AppendLine(")");
            lists.AppendLine();
        }
        #endregion

        #region Installation
        lists.AppendLine(@$"
install(CODE [[
    include({Paths.CMakeToolchain.ToString().DoubleQuoteIfNeeded()})

    file(INSTALL
        DESTINATION {OutDir.ToString().DoubleQuoteIfNeeded()}
        TYPE EXECUTABLE
        FILES ""$<TARGET_FILE:{TargetName}>""
    )

    file(GET_RUNTIME_DEPENDENCIES
        EXECUTABLES ""$<TARGET_FILE:{TargetName}>""
        RESOLVED_DEPENDENCIES_VAR DEPS_RESOLVED
        UNRESOLVED_DEPENDENCIES_VAR DEPS_UNRESOLVED
        DIRECTORIES ${{MINGW_DLL_DIRS}}
    )

    message(STATUS ""Resolved: ${{DEPS_RESOLVED}}"")
    file(INSTALL
        DESTINATION {OutDir.ToString().DoubleQuoteIfNeeded()}
        TYPE SHARED_LIBRARY
        FOLLOW_SYMLINK_CHAIN
        FILES ${{DEPS_RESOLVED}}
    )

    message(STATUS ""Unresolved: ${{DEPS_UNRESOLVED}}"")
]])
");
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
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

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

    enum PrecompiledHeader
    {
        NotUsing,
        Create,
        Use
    }

    abstract class Compile
    {
        protected readonly VCProjToCMakeLists Ctx;

        public readonly AbsolutePath Path;

        public readonly string Key;

        public virtual bool Excluded { get; }

        public Compile(VCProjToCMakeLists ctx, ProjectItem item)
        {
            Ctx = ctx;
            Path = ResolvePath(item.Project, item.EvaluatedInclude);
            Key = Path.NameWithoutExtension + "." + item.UnevaluatedInclude.ToString().GetSHA256Hash()[..4] + ".o";

            if (bool.TryParse(item.GetMetadataValue("ExcludedFromBuild"), out var excluded))
            {
                Excluded = excluded;
            }
        }

        public virtual void Gen(StringBuilder lists)
        {
            lists.AppendLine($"add_library({Key} OBJECT {Path.ToString().DoubleQuoteIfNeeded()})");
            lists.AppendLine();
        }
    }

    sealed class ClCompile(VCProjToCMakeLists ctx, ProjectItem item) : Compile(ctx, item)
    {
        public readonly string Definitions = item.GetMetadataValue("PreprocessorDefinitions");

        public readonly string StandardC =
            item.GetMetadataValue("LanguageStandard_C")
            .TrimStart("stdc")
            .Replace("latest", StandardCLatest)
            .ToNullIfEmpty() ?? StandardCLatest;

        public readonly string StandardCXX =
            item.GetMetadataValue("LanguageStandard")
            .TrimStart("stdcpp")
            .Replace("latest", StandardCXXLatest)
            .ToNullIfEmpty() ?? StandardCXXLatest;

        public readonly bool ExtensionsC = !item.GetMetadataValue("LanguageStandard_C").StartsWith("stdc");

        public readonly bool ExtensionsCXX = !item.GetMetadataValue("LanguageStandard").StartsWith("stdcpp");

        public readonly string AdditionalOptions = item.GetMetadataValue("AdditionalOptions");

        public readonly string Language =
            item.GetMetadataValue("CompileAs")
            .ToUpperInvariant()
            .TrimStart("COMPILEAS")
            .Replace("CPP", "CXX")
            .Replace("Default", "");

        public readonly PrecompiledHeader PCH =
            Enum.TryParse(item.GetMetadataValue("PrecompiledHeader"), out PrecompiledHeader value)
                ? value : PrecompiledHeader.NotUsing;

        public readonly AbsolutePath PCHFile = ResolvePath(item.Project, item.GetMetadataValue("PrecompiledHeaderFile"));

        public override bool Excluded => base.Excluded;

        public override void Gen(StringBuilder lists)
        {
            base.Gen(lists);

            lists.AppendLine($@"
set_target_properties({Key} PROPERTIES
    COMPILE_DEFINITIONS ""{Definitions}""
    C_STANDARD {StandardC}
    C_EXTENSIONS {(ExtensionsC ? "ON" : "OFF")}
    CXX_STANDARD {StandardCXX}
    CXX_EXTENSIONS {(ExtensionsCXX ? "ON" : "OFF")}
)
");

            var lang = Language;

            // For PCH files, the header extension might not match the source file extension...
            if (string.IsNullOrEmpty(lang) && PCH == PrecompiledHeader.Create)
            {
                // Based on GCC suffix list, which might not match MSVC behavior, but might be close enough.
                var cxx = Path.Name.EndsWithAny([
                    ".C", ".cc", ".cpp", ".CPP", ".c++", ".cp", ".cxx"
                ]);
                lang = cxx ? "CXX" : "C";
            }

            if (!string.IsNullOrEmpty(lang))
            {
                lists.AppendLine($"set_target_properties({Key} PROPERTIES LANGUAGE {lang})");
            }

            if (!string.IsNullOrEmpty(AdditionalOptions))
            {
                lists.AppendLine($"target_compile_options({Key} PRIVATE {AdditionalOptions})");
            }

            switch (PCH)
            {
                case PrecompiledHeader.Create:
                    Assert.NotNull(PCHFile);
                    lists.AppendLine($"target_precompile_headers({Key} PRIVATE {PCHFile.ToString().DoubleQuoteIfNeeded()})");
                    break;

                case PrecompiledHeader.Use:
                    var pch = Assert.NotNull(Ctx.PCH);
                    Assert.Equals(PCHFile, pch.PCHFile);
                    lists.AppendLine($"target_precompile_headers({Key} REUSE_FROM {pch.Key})");
                    break;
            }

            lists.AppendLine();
        }
    }

    sealed class ResourceCompile(VCProjToCMakeLists ctx, ProjectItem item) : Compile(ctx, item)
    {
    }
}
