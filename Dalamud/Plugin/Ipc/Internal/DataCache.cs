using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

    /// <summary> The creating plugin ID of this DataCache entry. </summary>
    internal readonly DataCachePluginId CreatorPluginId;

    /// <summary> A distinct list of plugin IDs that are using this data. </summary>
    /// <remarks> Also used as a reference count tracker. </remarks>
    internal readonly List<DataCachePluginId> UserPluginIds;

    /// <summary> The type the data was registered as. </summary>
    internal readonly Type Type;

    /// <summary> A reference to data. </summary>
    internal readonly object? Data;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataCache"/> struct.
    /// </summary>
    /// <param name="tag">Name of the data.</param>
    /// <param name="creatorPluginId">The internal name and effective working ID of the creating plugin.</param>
    /// <param name="data">A reference to data.</param>
    /// <param name="type">The type of the data.</param>
    public DataCache(string tag, DataCachePluginId creatorPluginId, object? data, Type type)
    {
        this.Tag = tag;
        this.CreatorPluginId = creatorPluginId;
        this.UserPluginIds = [];
        this.Data = data;
        this.Type = type;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DataCache"/> struct, using the given data generator function.
    /// </summary>
    /// <param name="tag">The name for the data cache.</param>
    /// <param name="creatorPluginId">The internal name and effective working ID of the creating plugin.</param>
    /// <param name="dataGenerator">The function that generates the data if it does not already exist.</param>
    /// <typeparam name="T">The type of the stored data - needs to be a reference type that is shared through Dalamud itself, not loaded by the plugin.</typeparam>
    /// <returns>The new instance of <see cref="DataCache"/>.</returns>
    public static DataCache From<T>(string tag, DataCachePluginId creatorPluginId, Func<T> dataGenerator)
        where T : class
    {
        try
        {
            var result = new DataCache(tag, creatorPluginId, dataGenerator.Invoke(), typeof(T));
            Log.Verbose(
                "[{who}] Created new data for [{Tag:l}] for creator {Creator:l}.",
                nameof(DataShare),
                tag,
                creatorPluginId);
            return result;
        }
        catch (Exception e)
        {
            throw ExceptionDispatchInfo.SetCurrentStackTrace(
                new DataCacheCreationError(tag, creatorPluginId, typeof(T), e));
        }
    }

    /// <summary>
    /// Attempts to fetch the data.
    /// </summary>
    /// <param name="callingPluginId">The calling plugin ID.</param>
    /// <param name="value">The value, if succeeded.</param>
    /// <param name="ex">The exception, if failed.</param>
    /// <typeparam name="T">Desired type of the data.</typeparam>
    /// <returns><c>true</c> on success.</returns>
    public bool TryGetData<T>(
        DataCachePluginId callingPluginId,
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

                // Register the access history. The effective working ID is unique per plugin and persists between reloads, so only add it once.
                lock (this.UserPluginIds)
                {
                    if (this.UserPluginIds.All(c => c.EffectiveWorkingId != callingPluginId.EffectiveWorkingId))
                    {
                        this.UserPluginIds.Add(callingPluginId);
                    }
                }

                return true;

            default:
                value = null;
                ex = ExceptionDispatchInfo.SetCurrentStackTrace(
                    new DataCacheTypeMismatchError(this.Tag, this.CreatorPluginId, typeof(T), this.Type));
                return false;
        }
    }
}
