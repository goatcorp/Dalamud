namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Interface for test implementations.
/// </summary>
internal interface IAgingStep
{
    /// <summary>
    /// Gets the name of the test.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Run the test step, once per frame it is active.
    /// </summary>
    /// <returns>The result of this frame, test is discarded once a result other than <see cref="SelfTestStepResult.Waiting"/> is returned.</returns>
    public SelfTestStepResult RunStep();

    /// <summary>
    /// Clean up this test.
    /// </summary>
    public void CleanUp();
}
