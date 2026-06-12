using System.Collections.Generic;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

public interface IVCProjExec
{
    IReadOnlyCollection<Output> Build(AbsolutePath path, Configuration config);

    IReadOnlyCollection<Output> Clean(AbsolutePath path, Configuration config);
}
