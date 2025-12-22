using System.Linq;

namespace Dalamud.Hooking.Internal.Verification;

/// <summary>
/// Exception thrown when a provided delegate for a hook does not match a known delegate.
/// </summary>
public class HookVerificationException : Exception
{
    private HookVerificationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Create a new <see cref="HookVerificationException"/> exception.
    /// </summary>
    /// <param name="address">The address of the function that is being hooked.</param>
    /// <param name="passed">The delegate passed by the user.</param>
    /// <param name="enforced">The delegate we think is correct.</param>
    /// <param name="message">Additional context to show to the user.</param>
    /// <returns>The created exception.</returns>
    internal static HookVerificationException Create(IntPtr address, Type passed, Type enforced, string message)
    {
        return new HookVerificationException(
            $"Hook verification failed for address 0x{address.ToInt64():X}\n\n" +
            $"Why:               {message}\n" +
            $"Passed Delegate:   {GetSignature(passed)}\n" +
            $"Correct Delegate:  {GetSignature(enforced)}\n\n" +
            "The hook delegate must exactly match the provided signature to prevent memory corruption and wrong data passed to originals.");
    }

    private static string GetSignature(Type delegateType)
    {
        var method = delegateType.GetMethod("Invoke");
        if (method == null) return delegateType.Name;

        var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"{method.ReturnType.Name} ({parameters})";
    }
}
