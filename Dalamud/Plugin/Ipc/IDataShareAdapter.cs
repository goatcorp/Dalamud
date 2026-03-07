namespace Dalamud.Plugin.Ipc;

/// <summary> An interface to provide live IPC adapters that can provide arbitrary values without the runtime overhead of IPC queries. </summary>
/// <remarks> Implement only whichever type of methods suits you best. This can then be used to create a wrapper encapsulating the actually available properties, either by the provider library, or on the consumer side. </remarks>
public interface IDataShareAdapter : IDisposable
{
    /// <summary> Get the value of a specific property by its name generically to avoid boxing of value types. </summary>
    /// <typeparam name="T"> The type the value should have. This needs to be a type known to Dalamud again. </typeparam>
    /// <param name="propertyName"> The name of the property. </param>
    /// <returns> The value. </returns>
    /// <remarks>
    ///   Should throw an exception if the requested property does not exist, or the return type is wrong.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public T? GetValue<T>(string propertyName)
        => throw new NotImplementedException();

    /// <summary> Get the value of a specific property by a custom integral ID generically to avoid boxing of value typey. </summary>
    /// <typeparam name="T"> The type the value should have. This needs to be a type known to Dalamud again. </typeparam>
    /// <param name="propertyId"> The custom integral ID of the property. </param>
    /// <returns> The value. </returns>
    /// <remarks>
    ///   Should throw an exception if the requested property does not exist, or the return type is wrong. <br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public T? GetValue<T>(int propertyId)
        => throw new NotImplementedException();

    /// <summary> Get the value of a specific property by its name as an object. </summary>
    /// <param name="propertyName"> The name of the property. </param>
    /// <returns> The value. </returns>
    /// <remarks>
    ///   Should throw an exception if the requested property does not exist.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public object? GetValue(string propertyName)
        => throw new NotImplementedException();

    /// <summary> Get the value of a specific property by a custom integral ID as an object. </summary>
    /// <param name="propertyId"> The custom integral ID of the property. </param>
    /// <returns> The value. </returns>
    /// <remarks>
    ///   Should throw an exception if the requested property does not exist.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public object? GetValue(int propertyId)
        => throw new NotImplementedException();
}
