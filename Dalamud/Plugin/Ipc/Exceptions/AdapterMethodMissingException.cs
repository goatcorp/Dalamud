namespace Dalamud.Plugin.Ipc;

/// <summary> Exception thrown if the method requested in a <see cref="IIdDataShareAdapter"/> or a <see cref="INameDataShareAdapter"/> does not exist. </summary>
public sealed class AdapterMethodMissingException : Exception
{
    /// <summary> Initializes a new instance of the <see cref="AdapterMethodMissingException"/> class using a named method. </summary>
    /// <param name="methodName"> The name of the method. </param>
    /// <param name="numArguments"> The number of arguments with which the method was requested. </param>
    /// <param name="func"> Whether the method was requested as a function or an action. </param>
    public AdapterMethodMissingException(string methodName, int numArguments, bool func)
        : base($"No {(func ? "function" : "action")} with the name {methodName} and {numArguments} arguments is available.")
    {
    }

    /// <summary> Initializes a new instance of the <see cref="AdapterMethodMissingException"/> class using a custom method ID. </summary>
    /// <param name="methodId"> The custom ID of the method. </param>
    /// <param name="numArguments"> The number of arguments with which the method was requested. </param>
    /// <param name="func"> Whether the method was requested as a function or an action. </param>
    public AdapterMethodMissingException(int methodId, int numArguments, bool func)
        : base($"No {(func ? "function" : "action")} with the ID {methodId} and {numArguments} arguments is available.")
    {
    }
}
