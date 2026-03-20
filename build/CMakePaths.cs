using Nuke.Common.IO;
using Nuke.Common.Tooling;

public interface ICMakePaths
{
    Tool CMake { get; }

    AbsolutePath RootDirectory { get; }

    AbsolutePath BuildToolDirectory { get; }

    AbsolutePath CMakeToolchain { get; }

    AbsolutePath VCStubDirectory { get; }

    AbsolutePath ProjectsDirectory { get; }

    AbsolutePath JWasmSrcDirectory { get; }

    AbsolutePath JWasmBuildDirectory { get; }

    AbsolutePath JWasmTool { get; }
}

public sealed record DefaultCMakePaths(AbsolutePath RootDirectory, Tool CMake) : ICMakePaths
{
    public AbsolutePath BuildToolDirectory => RootDirectory / "build";

    public AbsolutePath CMakeToolchain => BuildToolDirectory / "CMakeMinGW.cmake";

    public AbsolutePath VCStubDirectory => BuildToolDirectory / "vcstub";

    public AbsolutePath ProjectsDirectory => BuildToolDirectory / "cmake";

    public AbsolutePath JWasmSrcDirectory => RootDirectory / "lib" / "JWasm";

    public AbsolutePath JWasmBuildDirectory => JWasmSrcDirectory / "build";

    public AbsolutePath JWasmTool => JWasmBuildDirectory / "jwasm";
}
