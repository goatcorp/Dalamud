using System;
using System.Collections.Generic;

namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// A helper struct for reference-counted, type-safe shared access across plugin boundaries.
/// </summary>
internal readonly struct DataCache
{
    /// <summary> The assembly name of the initial creator. </summary>
    internal readonly string CreatorAssemblyName;

    /// <summary> A not-necessarily distinct list of current users. </summary>
    internal readonly List<string> UserAssemblyNames;

    /// <summary> The type the data was registered as. </summary>
    internal readonly Type Type;

    /// <summary> A reference to data. </summary>
    internal readonly object? Data;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataCache"/> struct.
    /// </summary>
    /// <param name="creatorAssemblyName">The assembly name of the initial creator.</param>
    /// <param name="data">A reference to data.</param>
    /// <param name="type">The type of the data.</param>
    public DataCache(string creatorAssemblyName, object? data, Type type)
    {
        this.CreatorAssemblyName = creatorAssemblyName;
        this.UserAssemblyNames = new List<string> { creatorAssemblyName };
        this.Data = data;
        this.Type = type;
    }
}
