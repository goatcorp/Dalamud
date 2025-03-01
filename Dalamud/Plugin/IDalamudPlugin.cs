using JetBrains.Annotations;

namespace Dalamud.Plugin;

/// <summary>
/// This interface represents a basic Dalamud plugin. All plugins have to implement this interface.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithInheritors)]
public interface IDalamudPlugin : IDisposable
{
}
