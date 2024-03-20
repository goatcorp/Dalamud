using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

using Dalamud.Plugin.Ipc.Exceptions;

using Serilog;

namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// A helper struct for reference-counted, type-safe shared access across plugin boundaries.
/// </summary>
internal readonly struct DataCache
{
    /// <summary> Name of the data. </summary>
    internal readonly string Tag;

    /// <summary> The assembly name of the initial creator. </summary>
    internal readonly string CreatorAssemblyName;

    /// <summary> A not-necessarily distinct list of current users. </summary>
    /// <remarks> Also used as a reference count tracker. </remarks>
    internal readonly List<string> UserAssemblyNames;

    /// <summary> The type the data was registered as. </summary>
    internal readonly Type Type;

    /// <summary> A reference to data. </summary>
    internal readonly object? Data;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataCache"/> struct.
    /// </summary>
    /// <param name="tag">Name of the data.</param>
    /// <param name="creatorAssemblyName">The assembly name of the initial creator.</param>
    /// <param name="data">A reference to data.</param>
    /// <param name="type">The type of the data.</param>
    public DataCache(string tag, string creatorAssemblyName, object? data, Type type)
    {
        this.Tag = tag;
        this.CreatorAssemblyName = creatorAssemblyName;
        this.UserAssemblyNames = new();
        this.Data = data;
        this.Type = type;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DataCache"/> struct, using the given data generator function.
    /// </summary>
    /// <param name="tag">The name for the data cache.</param>
    /// <param name="creatorAssemblyName">The assembly name of the initial creator.</param>
    /// <param name="dataGenerator">The function that generates the data if it does not already exist.</param>
    /// <typeparam name="T">The type of the stored data - needs to be a reference type that is shared through Dalamud itself, not loaded by the plugin.</typeparam>
    /// <returns>The new instance of <see cref="DataCache"/>.</returns>
    public static DataCache From<T>(string tag, string creatorAssemblyName, Func<T> dataGenerator)
        where T : class
    {
        try
        {
            var result = new DataCache(tag, creatorAssemblyName, dataGenerator.Invoke(), typeof(T));
            Log.Verbose(
                "[{who}] Created new data for [{Tag:l}] for creator {Creator:l}.",
                nameof(DataShare),
                tag,
                creatorAssemblyName);
            return result;
        }
        catch (Exception e)
        {
            throw ExceptionDispatchInfo.SetCurrentStackTrace(
                new DataCacheCreationError(tag, creatorAssemblyName, typeof(T), e));
        }
    }

    /// <summary>
    /// Attempts to fetch the data.
    /// </summary>
    /// <param name="callerName">The name of the caller assembly.</param>
    /// <param name="value">The value, if succeeded.</param>
    /// <param name="ex">The exception, if failed.</param>
    /// <typeparam name="T">Desired type of the data.</typeparam>
    /// <returns><c>true</c> on success.</returns>
    public bool TryGetData<T>(
        string callerName,
        [NotNullWhen(true)] out T? value,
        [NotNullWhen(false)] out Exception? ex)
        where T : class
    {
        switch (this.Data)
        {
            case null:
                value = null;
                ex = ExceptionDispatchInfo.SetCurrentStackTrace(new DataCacheValueNullError(this.Tag, this.Type));
                return false;

            case T data:
                value = data;
                ex = null;

                // Register the access history
                lock (this.UserAssemblyNames)
                    this.UserAssemblyNames.Add(callerName);

                return true;

            default:
                value = null;
                ex = ExceptionDispatchInfo.SetCurrentStackTrace(
                    new DataCacheTypeMismatchError(this.Tag, this.CreatorAssemblyName, typeof(T), this.Type));
                return false;
        }
    }
}
