namespace Dalamud.Plugin.SelfTest;

/// <summary>
/// Interface for test implementations.
/// </summary>
public interface ISelfTestStep
{
    /// <summary>
    /// Gets the name of the test.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Run the test step, once per frame it is active.
    /// </summary>
    /// <returns>The result of this frame, test is discarded once a result other than <see cref="SelfTestStepResult.Waiting"/> is returned.</returns>
    SelfTestStepResult RunStep();

    /// <summary>
    /// Clean up this test.
    /// </summary>
    void CleanUp();
}
