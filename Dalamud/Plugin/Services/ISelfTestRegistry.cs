using System.Collections.Generic;

using Dalamud.Plugin.SelfTest;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Interface for registering and unregistering self-test steps from plugins.
/// </summary>
/// <example>
/// Registering custom self-test steps for your plugin:
/// <code>
/// [PluginService]
/// public ISelfTestRegistry SelfTestRegistry { get; init; }
///
/// // In your plugin initialization
/// this.SelfTestRegistry.RegisterTestSteps([
///     new MyCustomSelfTestStep(),
///     new AnotherSelfTestStep()
/// ]);
/// </code>
///
/// Creating a custom self-test step:
/// <code>
/// public class MyCustomSelfTestStep : ISelfTestStep
/// {
///     public string Name => "My Custom Test";
///
///     public SelfTestStepResult RunStep()
///     {
///         // Your test logic here
///         if (/* test condition passes */)
///             return SelfTestStepResult.Pass;
///
///         if (/* test condition fails */)
///             return SelfTestStepResult.Fail;
///
///         // Still waiting for test to complete
///         return SelfTestStepResult.Waiting;
///     }
///
///     public void CleanUp()
///     {
///         // Clean up any resources used by the test
///     }
/// }
/// </code>
/// </example>
public interface ISelfTestRegistry : IDalamudService
{
    /// <summary>
    /// Registers the self-test steps for this plugin.
    /// </summary>
    /// <param name="steps">The test steps to register.</param>
    public void RegisterTestSteps(IEnumerable<ISelfTestStep> steps);
}
