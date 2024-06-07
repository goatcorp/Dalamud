using JetBrains.Annotations;

namespace Dalamud.IoC;

/// <summary>
/// This attribute indicates whether an applicable service should be injected into the plugin.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
[MeansImplicitUse(ImplicitUseKindFlags.Assign)]
public class PluginServiceAttribute : Attribute
{
}
