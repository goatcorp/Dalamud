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

    /// <summary> Get the value of a specific property by a custom integral ID generically to avoid boxing of value types. </summary>
    /// <typeparam name="T"> The type the value should have. This needs to be a type known to Dalamud again. </typeparam>
    /// <param name="propertyId"> The custom integral ID of the property. </param>
    /// <returns> The value. </returns>
    /// <remarks>
    ///   Should throw an exception if the requested property does not exist, or the return type is wrong. <br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public T? GetValue<T>(int propertyId)
        => throw new NotImplementedException();

    /// <summary> Set the value of a specific property by its name generically to avoid boxing of value types. </summary>
    /// <typeparam name="T"> The type the value should have. This needs to be a type known to Dalamud again. </typeparam>
    /// <param name="propertyName"> The name of the property. </param>
    /// <param name="newValue"> The intended new value of the property. </param>
    /// <returns> True if the property was changed, false otherwise. </returns>
    /// <remarks>
    ///   Should either throw an exception if the requested property does not exist, or the value type is wrong, or just return false.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public bool SetValue<T>(string propertyName, T newValue)
        => throw new NotImplementedException();

    /// <summary> Set the value of a specific property by a custom integral ID generically to avoid boxing of value types. </summary>
    /// <typeparam name="T"> The type the value should have. This needs to be a type known to Dalamud again. </typeparam>
    /// <param name="propertyId"> The custom integral ID of the property. </param>
    /// <param name="newValue"> The intended new value of the property. </param>
    /// <returns> True if the property was changed, false otherwise. </returns>
    /// <remarks>
    ///   Should either throw an exception if the requested property does not exist, or the value type is wrong, or just return false.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public bool SetValue<T>(int propertyId, T newValue)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke(string methodName)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1>(string methodName, T1 argument1)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2>(string methodName, T1 argument1, T2 argument2)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3>(string methodName, T1 argument1, T2 argument2, T3 argument3)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8)
        => throw new NotImplementedException();

    /// <summary> Try to invoke an action by its name using generic arguments. </summary>
    /// <typeparam name="T1"> The first parameter type. </typeparam>
    /// <typeparam name="T2"> The second parameter type. </typeparam>
    /// <typeparam name="T3"> The third parameter type. </typeparam>
    /// <typeparam name="T4"> The fourth parameter type. </typeparam>
    /// <typeparam name="T5"> The fifth parameter type. </typeparam>
    /// <typeparam name="T6"> The sixth parameter type. </typeparam>
    /// <typeparam name="T7"> The seventh parameter type. </typeparam>
    /// <typeparam name="T8"> The eight parameter type. </typeparam>
    /// <typeparam name="T9"> The ninth parameter type. </typeparam>
    /// <param name="methodName"> The name of the method to invoke. </param>
    /// <param name="argument1"> The first argument. </param>
    /// <param name="argument2"> The second argument. </param>
    /// <param name="argument3"> The third argument. </param>
    /// <param name="argument4"> The fourth argument. </param>
    /// <param name="argument5"> The fifth argument. </param>
    /// <param name="argument6"> The sixth argument. </param>
    /// <param name="argument7"> The seventh argument. </param>
    /// <param name="argument8"> The eighth argument. </param>
    /// <param name="argument9"> The ninth argument. </param>
    /// <returns> True if the invocation was successful, false otherwise. </returns>
    /// <remarks>
    ///   Should either throw an exception if the requested method does not exist, or any of the parameter types is wrong, or just return false.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<TRet>(string methodName, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, TRet>(string methodName, T1 argument1, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, TRet>(string methodName, T1 argument1, T2 argument2, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, out TRet? ret)
        => throw new NotImplementedException();

    /// <summary> Try to invoke a function by its name using generic arguments. </summary>
    /// <typeparam name="T1"> The first parameter type. </typeparam>
    /// <typeparam name="T2"> The second parameter type. </typeparam>
    /// <typeparam name="T3"> The third parameter type. </typeparam>
    /// <typeparam name="T4"> The fourth parameter type. </typeparam>
    /// <typeparam name="T5"> The fifth parameter type. </typeparam>
    /// <typeparam name="T6"> The sixth parameter type. </typeparam>
    /// <typeparam name="T7"> The seventh parameter type. </typeparam>
    /// <typeparam name="T8"> The eight parameter type. </typeparam>
    /// <typeparam name="T9"> The ninth parameter type. </typeparam>
    /// <typeparam name="TRet"> The type of the returned value. </typeparam>
    /// <param name="methodName"> The name of the method to invoke. </param>
    /// <param name="argument1"> The first argument. </param>
    /// <param name="argument2"> The second argument. </param>
    /// <param name="argument3"> The third argument. </param>
    /// <param name="argument4"> The fourth argument. </param>
    /// <param name="argument5"> The fifth argument. </param>
    /// <param name="argument6"> The sixth argument. </param>
    /// <param name="argument7"> The seventh argument. </param>
    /// <param name="argument8"> The eighth argument. </param>
    /// <param name="argument9"> The ninth argument. </param>
    /// <param name="ret"> The returned value on success, undefined on failure. </param>
    /// <returns> True if the invocation was successful, false otherwise. </returns>
    /// <remarks>
    ///   Should either throw an exception if the requested method does not exist, or any of the parameter types or the return type is wrong, or just return false.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke(int methodId)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1>(int methodId, T1 argument1)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2>(int methodId, T1 argument1, T2 argument2)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3>(int methodId, T1 argument1, T2 argument2, T3 argument3)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8)
        => throw new NotImplementedException();

    /// <summary> Try to invoke an action by a custom integral ID using generic arguments. </summary>
    /// <typeparam name="T1"> The first parameter type. </typeparam>
    /// <typeparam name="T2"> The second parameter type. </typeparam>
    /// <typeparam name="T3"> The third parameter type. </typeparam>
    /// <typeparam name="T4"> The fourth parameter type. </typeparam>
    /// <typeparam name="T5"> The fifth parameter type. </typeparam>
    /// <typeparam name="T6"> The sixth parameter type. </typeparam>
    /// <typeparam name="T7"> The seventh parameter type. </typeparam>
    /// <typeparam name="T8"> The eight parameter type. </typeparam>
    /// <typeparam name="T9"> The ninth parameter type. </typeparam>
    /// <param name="methodId"> The custom integral ID of the method. </param>
    /// <param name="argument1"> The first argument. </param>
    /// <param name="argument2"> The second argument. </param>
    /// <param name="argument3"> The third argument. </param>
    /// <param name="argument4"> The fourth argument. </param>
    /// <param name="argument5"> The fifth argument. </param>
    /// <param name="argument6"> The sixth argument. </param>
    /// <param name="argument7"> The seventh argument. </param>
    /// <param name="argument8"> The eighth argument. </param>
    /// <param name="argument9"> The ninth argument. </param>
    /// <returns> True if the invocation was successful, false otherwise. </returns>
    /// <remarks>
    ///   Should either throw an exception if the requested method does not exist, or any of the parameter types is wrong, or just return false.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<TRet>(int methodId, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, TRet>(int methodId, T1 argument1, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, TRet>(int methodId, T1 argument1, T2 argument2, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, out TRet? ret)
        => throw new NotImplementedException();

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, out TRet? ret)
        => throw new NotImplementedException();

    /// <summary> Try to invoke a function by a custom integral ID using generic arguments. </summary>
    /// <typeparam name="T1"> The first parameter type. </typeparam>
    /// <typeparam name="T2"> The second parameter type. </typeparam>
    /// <typeparam name="T3"> The third parameter type. </typeparam>
    /// <typeparam name="T4"> The fourth parameter type. </typeparam>
    /// <typeparam name="T5"> The fifth parameter type. </typeparam>
    /// <typeparam name="T6"> The sixth parameter type. </typeparam>
    /// <typeparam name="T7"> The seventh parameter type. </typeparam>
    /// <typeparam name="T8"> The eight parameter type. </typeparam>
    /// <typeparam name="T9"> The ninth parameter type. </typeparam>
    /// <typeparam name="TRet"> The type of the returned value. </typeparam>
    /// <param name="methodId"> The name of the method to invoke. </param>
    /// <param name="argument1"> The first argument. </param>
    /// <param name="argument2"> The second argument. </param>
    /// <param name="argument3"> The third argument. </param>
    /// <param name="argument4"> The fourth argument. </param>
    /// <param name="argument5"> The fifth argument. </param>
    /// <param name="argument6"> The sixth argument. </param>
    /// <param name="argument7"> The seventh argument. </param>
    /// <param name="argument8"> The eighth argument. </param>
    /// <param name="argument9"> The ninth argument. </param>
    /// <param name="ret"> The returned value on success, undefined on failure. </param>
    /// <returns> True if the invocation was successful, false otherwise. </returns>
    /// <remarks>
    ///   Should either throw an exception if the requested method does not exist, or any of the parameter types or the return type is wrong, or just return false.<br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    public bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8, T9 argument9, out TRet? ret)
        => throw new NotImplementedException();
}
