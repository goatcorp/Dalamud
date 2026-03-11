namespace Dalamud.Plugin.Ipc;

/// <summary> An interface to provide live IPC adapters that can invoke methods directly using custom IDs without the runtime overhead of IPC queries. </summary>
/// <remarks> Implement only methods you actually need. This can then be used to create a wrapper encapsulating the actually available methods or properties, either by the provider library, or on the consumer side. </remarks>
public interface IIdDataShareAdapter : IDisposable
{
    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke(int methodId)
        => throw new AdapterMethodMissingException(methodId, 0, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1>(int methodId, T1 argument1)
        where T1 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 1, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2>(int methodId, T1 argument1, T2 argument2)
        where T1 : allows ref struct
        where T2 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 2, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3>(int methodId, T1 argument1, T2 argument2, T3 argument3)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 3, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 4, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4, T5>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 5, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4, T5, T6>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 6, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4, T5, T6, T7>(
        int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 7, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(
        int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 8, false);

    /// <summary> Try to invoke an action by a custom integral ID using generic arguments. </summary>
    /// <typeparam name="T1"> The first parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T2"> The second parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T3"> The third parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T4"> The fourth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T5"> The fifth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T6"> The sixth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T7"> The seventh parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T8"> The eight parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T9"> The ninth parameter type. This needs to be a type known to Dalamud again. </typeparam>
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
    /// <remarks>
    ///   Should throw <see cref="AdapterMethodMissingException"/> if the requested method does not exist. <br/>
    ///   Should throw <see cref="AdapterTypeMismatchException"/> if the requested method can not handle a provided argument. <br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where T9 : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 9, false);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<TRet>(int methodId, out TRet? ret)
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 0, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, TRet>(int methodId, T1 argument1, out TRet? ret)
        where T1 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 1, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, TRet>(int methodId, T1 argument1, T2 argument2, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 2, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 3, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 4, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, T5, TRet>(int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 5, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, T5, T6, TRet>(
        int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 6, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, TRet>(
        int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 7, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(int,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(
        int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 8, true);

    /// <summary> Try to invoke a function by a custom integral ID using generic arguments. </summary>
    /// <typeparam name="T1"> The first parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T2"> The second parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T3"> The third parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T4"> The fourth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T5"> The fifth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T6"> The sixth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T7"> The seventh parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T8"> The eight parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T9"> The ninth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="TRet"> The type of the returned value. This needs to be a type known to Dalamud again. </typeparam>
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
    ///   Should throw <see cref="AdapterMethodMissingException"/> if the requested method does not exist. <br/>
    ///   Should throw <see cref="AdapterTypeMismatchException"/> if the requested method can not handle a provided argument or the return value. <br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(
        int methodId, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where T9 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodId, 9, true);
}

/// <summary> An interface to provide live IPC adapters that can invoke methods directly using names without the runtime overhead of IPC queries. </summary>
/// <remarks> Implement only whichever type of methods suits you best. This can then be used to create a wrapper encapsulating the actually available methods or properties, either by the provider library, or on the consumer side. </remarks>
public interface INameDataShareAdapter : IDisposable
{
    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke(string methodName)
        => throw new AdapterMethodMissingException(methodName, 0, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1>(string methodName, T1 argument1)
        where T1 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 1, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2>(string methodName, T1 argument1, T2 argument2)
        where T1 : allows ref struct
        where T2 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 2, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3>(string methodName, T1 argument1, T2 argument2, T3 argument3)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 3, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 4, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4, T5>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 5, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4, T5, T6>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 6, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4, T5, T6, T7>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 7, false);

    /// <inheritdoc cref="Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
    void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 8, false);

    /// <summary> Try to invoke an action by its name using generic arguments. </summary>
    /// <typeparam name="T1"> The first parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T2"> The second parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T3"> The third parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T4"> The fourth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T5"> The fifth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T6"> The sixth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T7"> The seventh parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T8"> The eight parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T9"> The ninth parameter type. This needs to be a type known to Dalamud again. </typeparam>
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
    /// <remarks>
    ///   Should throw <see cref="AdapterMethodMissingException"/> if the requested method does not exist. <br/>
    ///   Should throw <see cref="AdapterTypeMismatchException"/> if the requested method can not handle a provided argument. <br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where T9 : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 9, false);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<TRet>(string methodName, out TRet? ret)
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 0, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, TRet>(string methodName, T1 argument1, out TRet? ret)
        where T1 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 1, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, TRet>(string methodName, T1 argument1, T2 argument2, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 2, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 3, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, TRet>(string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 4, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, T5, TRet>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 5, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, T5, T6, TRet>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 6, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, TRet>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 7, true);

    /// <inheritdoc cref="TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}(string,T1,T2,T3,T4,T5,T6,T7,T8,T9,out TRet)"/>
    bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 8, true);

    /// <summary> Try to invoke a function by its name using generic arguments. </summary>
    /// <typeparam name="T1"> The first parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T2"> The second parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T3"> The third parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T4"> The fourth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T5"> The fifth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T6"> The sixth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T7"> The seventh parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T8"> The eight parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="T9"> The ninth parameter type. This needs to be a type known to Dalamud again. </typeparam>
    /// <typeparam name="TRet"> The type of the returned value. This needs to be a type known to Dalamud again. </typeparam>
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
    ///   Should throw <see cref="AdapterMethodMissingException"/> if the requested method does not exist. <br/>
    ///   Should throw <see cref="AdapterTypeMismatchException"/> if the requested method can not handle a provided argument or the return value. <br/>
    ///   The method should generally be implemented as a switch-statement.
    /// </remarks>
    bool TryInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(
        string methodName, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8,
        T9 argument9, out TRet? ret)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where T9 : allows ref struct
        where TRet : allows ref struct
        => throw new AdapterMethodMissingException(methodName, 9, true);
}
