namespace Dalamud.IoC.Internal;

/// <summary>
/// Mark a class as being dependent on a service, without actually injecting it.
/// </summary>
/// <typeparam name="T">The service to be dependent upon.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal class InherentDependencyAttribute<T> : InherentDependencyAttribute where T : IServiceType
{
}

/// <summary>
/// Helper class used for matching. Use the generic version.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal class InherentDependencyAttribute : Attribute
{
}
