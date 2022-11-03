namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test that waits N frames.
/// </summary>
internal class WaitFramesAgingStep : IAgingStep
{
    private readonly int frames;
    private int cFrames;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitFramesAgingStep"/> class.
    /// </summary>
    /// <param name="frames">Amount of frames to wait.</param>
    public WaitFramesAgingStep(int frames)
    {
        this.frames = frames;
        this.cFrames = frames;
    }

    /// <inheritdoc/>
    public string Name => $"Wait {this.cFrames} frames";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        this.cFrames--;

        return this.cFrames <= 0 ? SelfTestStepResult.Pass : SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        this.cFrames = this.frames;
    }
}
