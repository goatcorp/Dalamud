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

    readonly string OutputName;

    readonly ConfigurationType Type;

    readonly AbsolutePath OutDir;

    readonly string PostBuild;

    readonly string SubSystem;

    readonly string LinkOptions;

    readonly AbsolutePath ModuleDef = null;

    readonly ClCompile PCH;

    readonly List<Compile> Compiles = [];

    readonly List<string> LinkDeps = [];

    AbsolutePath OwnDirectory => Paths.ProjectsDirectory / Name;

    AbsolutePath CMakeListsPath => OwnDirectory / "CMakeLists.txt";

    public VCProjToCMakeLists(ICMakePaths paths, Project proj)
    {
        Paths = paths;
        VCProj = proj;

        Name = VCProj.GetPropertyValue("TargetName");
        Type = Enum.Parse<ConfigurationType>(VCProj.GetPropertyValue("ConfigurationType"));
        OutDir = ResolvePath(VCProj, VCProj.GetPropertyValue("OutDir"));
        PostBuild = VCProj.GetPropertyValue("CMakePostBuild");

        OutputName = Type switch
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
                "Content" => new Content(this, item),
                "MASM" => new MASM(this, item),
                "ResourceCompile" => new ResourceCompile(this, item),
                _ => null
            };

            if (compile is null || compile.IsExcluded)
            {
                continue;
            }

            if (compile as ClCompile is { PCH: PrecompiledHeader.Create } pch)
            {
                Assert.Equals(PCH, null);
                PCH = pch;
                Compiles.Insert(0, pch);
                continue;
            }

            Compiles.Add(compile);
        }

        if (VCProj.ItemDefinitions.TryGetValue("Link", out var link))
        {
            SubSystem = link.GetMetadataValue("SubSystem");
            LinkOptions = link.GetMetadataValue("AdditionalOptions");
            ModuleDef = ResolvePath(VCProj, link.GetMetadataValue("ModuleDefinitionFile"));

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
include({Paths.CMakeToolchain.ToString().SingleQuoteIfNeeded()})
project({Name} C CXX ASM_MASM RC)

# Target Windows 10
add_compile_definitions(_WIN32_WINNT=0x0A00 WINVER=0x0A00)

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
            compile.GenBeforeMain(lists);
        }
        #endregion

        #region Objects -> Target
        lists.AppendLine(Type switch
        {
            ConfigurationType.Application => $"add_executable({Name}",
            ConfigurationType.DynamicLibrary => $"add_library({Name} SHARED",
            ConfigurationType.StaticLibrary => $"add_library({Name} STATIC",
            _ => throw new NotSupportedException(),
        });

        foreach (var compile in Compiles)
        {
            compile.GenMainDep(lists);
        }

        lists.AppendLine(")");
        lists.AppendLine();
        #endregion

        #region Non-.o Items
        foreach (var compile in Compiles)
        {
            compile.GenAfterMain(lists);
        }
        #endregion

        #region Target props
        lists.AppendLine($@"
set_target_properties({Name} PROPERTIES
    OUTPUT_NAME {OutputName}
    PREFIX """"
    SUFFIX """"
)

");

        switch (Type)
        {
            case ConfigurationType.Application:
                lists.AppendLine($"target_link_options({Name} PRIVATE -m{SubSystem.ToLowerInvariant()})");
                lists.AppendLine();
                break;
        }

        if (!string.IsNullOrEmpty(LinkOptions))
        {
            lists.AppendLine($"target_link_options({Name} PRIVATE {LinkOptions})");
            lists.AppendLine();
        }

        if (ModuleDef is not null)
        {
            lists.AppendLine($"target_link_options({Name} PRIVATE {ModuleDef.ToString().SingleQuoteIfNeeded()})");
        }

        if (LinkDeps.Count != 0)
        {
            lists.AppendLine($"target_link_libraries({Name} PRIVATE");

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
    include({Paths.CMakeToolchain.ToString().SingleQuoteIfNeeded()})

    set(PROJECTDIR {ResolvePath(VCProj, ".").ToString().SingleQuoteIfNeeded()})

    file(INSTALL
        DESTINATION {OutDir.ToString().SingleQuoteIfNeeded()}
        FILES ""$<TARGET_FILE:{Name}>""
    )

    file(GET_RUNTIME_DEPENDENCIES
        EXECUTABLES ""$<TARGET_FILE:{Name}>""
        RESOLVED_DEPENDENCIES_VAR DEPS_RESOLVED
        UNRESOLVED_DEPENDENCIES_VAR DEPS_UNRESOLVED
        DIRECTORIES ${{MINGW_DLL_DIRS}}
    )

    message(STATUS ""Resolved: ${{DEPS_RESOLVED}}"")
    file(INSTALL
        DESTINATION {OutDir.ToString().SingleQuoteIfNeeded()}
        FOLLOW_SYMLINK_CHAIN
        FILES ${{DEPS_RESOLVED}}
    )

    message(STATUS ""Unresolved: ${{DEPS_UNRESOLVED}}"")

");

        if (!string.IsNullOrEmpty(PostBuild))
        {
            lists.AppendLine(PostBuild);
        }

        lists.AppendLine(@"
]])");
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
                    { "IsMinGW", GetCompilerId(paths) },
                    { "VCTargetsPath", paths.VCStubDirectory },
                    { "CoreLibraryDependencies", "kernel32.lib;user32.lib;gdi32.lib;winspool.lib;comdlg32.lib;advapi32.lib;shell32.lib;ole32.lib;oleaut32.lib;uuid.lib;odbc32.lib;odbccp32.lib" }
                },
                ToolsVersion = collection.DefaultToolsVersion,
                ProjectCollection = collection
            });
    }

    private static string GetCompilerId(ICMakePaths paths)
    {
        AbsolutePath checkTmp = Path.GetTempFileName();
        File.Delete(checkTmp);
        Directory.CreateDirectory(checkTmp);

        File.WriteAllText(checkTmp / "CMakeLists.txt", $@"
cmake_minimum_required(VERSION {CMakeVersion})
include({paths.CMakeToolchain.ToString().SingleQuoteIfNeeded()})
project(GetCompilerId CXX)
message(STATUS ""CMAKE_CXX_COMPILER_ID=${{CMAKE_CXX_COMPILER_ID}}"")
");

        var outputs = paths.CMake(
            $"-S{checkTmp.ToString().SingleQuoteIfNeeded()} " +
            $"-B{checkTmp.ToString().SingleQuoteIfNeeded()}");

        Directory.Delete(checkTmp, true);

        var line = outputs
            .Select(l => (l.Text, Index: l.Text.IndexOf("CMAKE_CXX_COMPILER_ID=")))
            .First(l => l.Index != -1);

        return line.Text[(line.Index + "CMAKE_CXX_COMPILER_ID=".Length)..].Trim();
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

    enum CopyToOutputDirectory
    {
        Never,
        Always,
        PreserveNewest,
        IfDifferent
    }

    #region Items

    abstract class Compile
    {
        protected readonly VCProjToCMakeLists Ctx;

        public readonly AbsolutePath Path;

        public readonly string Key;

        public readonly bool IsExcluded;

        public Compile(VCProjToCMakeLists ctx, ProjectItem item)
        {
            Ctx = ctx;
            Path = ResolvePath(item.Project, item.EvaluatedInclude);
            Key = Path.NameWithoutExtension + "." + item.UnevaluatedInclude.ToString().GetSHA256Hash()[..4] + ".o";

            if (bool.TryParse(item.GetMetadataValue("ExcludedFromBuild"), out var excluded))
            {
                IsExcluded = excluded;
            }
        }

        public virtual void GenBeforeMain(StringBuilder lists)
        {
        }

        public virtual void GenMainDep(StringBuilder lists)
        {
        }

        public virtual void GenAfterMain(StringBuilder lists)
        {
        }
    }

    abstract class CompileObj(VCProjToCMakeLists ctx, ProjectItem item) : Compile(ctx, item)
    {
        public override void GenBeforeMain(StringBuilder lists)
        {
            lists.AppendLine($"add_library({Key} OBJECT {Path.ToString().SingleQuoteIfNeeded()})");
            lists.AppendLine();
        }

        public override void GenMainDep(StringBuilder lists)
        {
            lists.AppendLine($"$<TARGET_OBJECTS:{Key}>");
        }
    }

    sealed class ClCompile(VCProjToCMakeLists ctx, ProjectItem item) : CompileObj(ctx, item)
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

        public readonly string AdditionalIncludeDirectories = item.GetMetadataValue("AdditionalIncludeDirectories");

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

        public override void GenBeforeMain(StringBuilder lists)
        {
            base.GenBeforeMain(lists);

            lists.AppendLine($@"
set_target_properties({Key} PROPERTIES
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

            if (!string.IsNullOrEmpty(Definitions))
            {
                lists.AppendLine($"target_compile_definitions({Key} PRIVATE {Definitions})");
            }

            if (!string.IsNullOrEmpty(AdditionalOptions))
            {
                lists.AppendLine($"target_compile_options({Key} PRIVATE {AdditionalOptions})");
            }

            if (!string.IsNullOrEmpty(AdditionalIncludeDirectories))
            {
                lists.AppendLine($"target_include_directories({Key} PRIVATE");
                foreach (var dir in AdditionalIncludeDirectories.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    lists.AppendLine(ResolvePath(Ctx.VCProj, dir).ToString().SingleQuoteIfNeeded());
                }
                lists.AppendLine(")");
            }

            switch (PCH)
            {
                case PrecompiledHeader.Create:
                    Assert.NotNull(PCHFile);
                    lists.AppendLine($"target_precompile_headers({Key} PRIVATE {PCHFile.ToString().SingleQuoteIfNeeded()})");
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

    sealed class MASM(VCProjToCMakeLists ctx, ProjectItem item) : CompileObj(ctx, item)
    {
        public readonly string AdditionalOptions = item.GetMetadataValue("AdditionalOptions");

        public override void GenBeforeMain(StringBuilder lists)
        {
            base.GenBeforeMain(lists);

            lists.AppendLine($"set_target_properties({Key} PROPERTIES LANGUAGE ASM_MASM)");

            if (!string.IsNullOrEmpty(AdditionalOptions))
            {
                lists.AppendLine($"target_compile_options({Key} PRIVATE {AdditionalOptions})");
            }

            lists.AppendLine();
        }
    }

    sealed class ResourceCompile(VCProjToCMakeLists ctx, ProjectItem item) : CompileObj(ctx, item)
    {
    }

    sealed class Content(VCProjToCMakeLists ctx, ProjectItem item) : Compile(ctx, item)
    {
        public readonly string Link = item.GetMetadataValue("Link");

        // TODO: Not that critical to implement, but if anyone wants to dig deeper... -jade
        public readonly CopyToOutputDirectory Copy =
            Enum.TryParse(item.GetMetadataValue("CopyToOutputDirectory"), out CopyToOutputDirectory value)
                ? value : CopyToOutputDirectory.Never;

        string TargetPath => $"${{CMAKE_CURRENT_BINARY_DIR}}/{Link}";

        public override void GenBeforeMain(StringBuilder lists)
        {
            if (Copy == CopyToOutputDirectory.Never)
            {
                return;
            }

            lists.AppendLine($@"
add_custom_command(
    OUTPUT {TargetPath}
    COMMAND ${{CMAKE_COMMAND}} -E copy
        {Path.ToString().SingleQuoteIfNeeded()}
        {TargetPath}
    DEPENDS {Path.ToString().SingleQuoteIfNeeded()}
)
");
        }

        public override void GenMainDep(StringBuilder lists)
        {
            if (Copy == CopyToOutputDirectory.Never)
            {
                return;
            }

            lists.AppendLine(TargetPath);
        }
    }
    #endregion
}
