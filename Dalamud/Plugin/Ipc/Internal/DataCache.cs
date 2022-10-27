using System;
using System.Collections.Generic;

namespace Dalamud.Plugin.Ipc.Internal;

internal class DataCache
{
    internal readonly string       CreatorAssemblyName;
    internal readonly List<string> UserAssemblyNames;
    internal readonly Type         Type;
    internal readonly object?      Data;

    internal DataCache(string creatorAssemblyName, object? data, Type type)
    {
        this.CreatorAssemblyName = creatorAssemblyName;
        this.UserAssemblyNames   = new List<string>{ creatorAssemblyName };
        this.Data                = data;
        this.Type                = type;
    }
}
