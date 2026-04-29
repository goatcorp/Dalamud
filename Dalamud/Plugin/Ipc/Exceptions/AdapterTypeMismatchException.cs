namespace Dalamud.Plugin.Ipc;

/// <summary> Exception thrown if the method requested in a <see cref="IIdDataShareAdapter"/> or a <see cref="INameDataShareAdapter"/> can not handle a specified argument type. </summary>
public sealed class AdapterTypeMismatchException : Exception
{
    /// <summary> Initializes a new instance of the <see cref="AdapterTypeMismatchException"/> class using a named method. </summary>
    /// <param name="methodName"> The name of the method. </param>
    /// <param name="numArguments"> The number of arguments with which the method was requested. </param>
    /// <param name="func"> Whether the method was requested as a function or an action. </param>
    /// <param name="argumentIndex"> The index of the failing argument, or -1 if it is the return type. </param>
    /// <param name="passedType"> The passed type that was incompatible. </param>
    public AdapterTypeMismatchException(string methodName, int numArguments, bool func, int argumentIndex, Type passedType)
        : base(
            argumentIndex is -1
                ? $"The {(func ? "function" : "action")} with the name {methodName} and {numArguments} arguments can not return a value of type {passedType}."
                : $"The {(func ? "function" : "action")} with the name {methodName} and {numArguments} arguments can not be invoked with an argument of type {passedType} as argument {argumentIndex}.")
    {
    }

    /// <summary> Initializes a new instance of the <see cref="AdapterTypeMismatchException"/> class using a custom method ID. </summary>
    /// <param name="methodId"> The custom ID of the method. </param>
    /// <param name="numArguments"> The number of arguments with which the method was requested. </param>
    /// <param name="func"> Whether the method was requested as a function or an action. </param>
    /// <param name="argumentIndex"> The index of the failing argument, or -1 if it is the return type. </param>
    /// <param name="passedType"> The passed type that was incompatible. </param>
    public AdapterTypeMismatchException(int methodId, int numArguments, bool func, int argumentIndex, Type passedType)
        : base(
            argumentIndex is -1
                ? $"The {(func ? "function" : "action")} with the ID {methodId} and {numArguments} arguments can not return a value of type {passedType}."
                : $"The {(func ? "function" : "action")} with the ID {methodId} and {numArguments} arguments can not be invoked with an argument of type {passedType} as argument {argumentIndex}.")
    {
    }
}
