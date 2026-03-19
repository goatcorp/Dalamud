using System.Collections.Generic;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

public readonly record struct VCProjMSBuildExec : IVCProjExec
{
    public IReadOnlyCollection<Output> Build(AbsolutePath path, Configuration config)
    {
        return MSBuildTasks.MSBuild(s => s
            .SetTargetPath(path)
            .SetConfiguration(config)
            .SetTargetPlatform(MSBuildTargetPlatform.x64));
    }

    public IReadOnlyCollection<Output> Clean(AbsolutePath path, Configuration config)
    {
        return MSBuildTasks.MSBuild(s => s
            .SetProjectFile(path)
            .SetConfiguration(config)
            .SetTargets("Clean"));
    }
}
