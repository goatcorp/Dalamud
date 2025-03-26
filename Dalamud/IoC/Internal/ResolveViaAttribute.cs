namespace Dalamud.IoC.Internal;

/// <summary>
/// Indicates that an interface a service can implement can be used to resolve that service.
/// Take care: only one service can implement an interface with this attribute at a time.
/// </summary>
/// <typeparam name="T">The interface that can be used to resolve the service.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal class ResolveViaAttribute<T> : ResolveViaAttribute
{
}

/// <summary>
/// Helper class used for matching. Use the generic version.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal class ResolveViaAttribute : Attribute
{
}
