using System.Collections.Generic;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

public sealed class VCProjToCMakeExec(ICMakePaths paths) : IVCProjExec
{
    readonly ICMakePaths Paths = paths;

    public IReadOnlyCollection<Output> Build(AbsolutePath path, Configuration config)
    {
        var cmakelists = new VCProjToCMakeLists(Paths, path, config);
        var dir = cmakelists.Gen();

        List<Output> outputs = [];

        outputs.AddRange(Paths.CMake(
            $"-S{dir.ToString().SingleQuoteIfNeeded()} " +
            $"-B{dir.ToString().SingleQuoteIfNeeded()}"));

        outputs.AddRange(Paths.CMake(
            $"--build {dir.ToString().SingleQuoteIfNeeded()}"));

        outputs.AddRange(Paths.CMake(
            $"--install {dir.ToString().SingleQuoteIfNeeded()}"));

        return outputs;
    }

    public IReadOnlyCollection<Output> Clean(AbsolutePath path, Configuration config)
    {
        var cmakelists = new VCProjToCMakeLists(Paths, path, config);
        cmakelists.Clean();

        return [];
    }
}
