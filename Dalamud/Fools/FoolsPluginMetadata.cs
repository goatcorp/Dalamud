using System;

namespace Dalamud.Fools;

public class FoolsPluginMetadata
{
    public string Name { get; init; }

    public string InternalName { get; init; }

    public string Description { get; init; }

    public string Author { get; init; }

    public string RealAuthor { get; init; }

    public Type Type { get; init; }
}
