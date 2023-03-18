using System;

namespace Dalamud.Fools;

public class FoolsPluginMetadata
{
    public string Name { get; }

    public string InternalName { get; }

    public string Description { get; }

    public string Author { get; }

    public Type Type { get; }

    public FoolsPluginMetadata(string name, string internalName, string description, string author, Type type)
    {
        this.Name = name;
        this.InternalName = internalName;
        this.Description = description;
        this.Author = author;
        this.Type = type;
    }
}
