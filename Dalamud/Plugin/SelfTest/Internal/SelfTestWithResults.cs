using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Plugin.SelfTest.Internal;

/// <summary>
/// A self test step with result tracking.
/// </summary>
internal class SelfTestWithResults
{
    private static readonly ModuleLog Log = new("SelfTest");

    /// <summary>
    /// Initializes a new instance of the <see cref="SelfTestWithResults"/> class.
    /// </summary>
    /// <param name="plugin">The plugin providing this test.</param>
    /// <param name="group">The test group name.</param>
    /// <param name="step">The test step.</param>
    public SelfTestWithResults(LocalPlugin plugin, string group, ISelfTestStep step)
    {
        this.Plugin = plugin;
        this.Group = group;
        this.Step = step;
    }

    /// <summary>
    /// Gets the test group name.
    /// </summary>
    public string Group { get; private set; }

    /// <summary>
    /// Gets the plugin that defined these tests. <c>null</c> for Dalamud tests.
    /// </summary>
    public LocalPlugin? Plugin { get; private set; }

    /// <summary>
    /// Gets the test name.
    /// </summary>
    public string Name { get => this.Step.Name; }

    /// <summary>
    /// Gets a value indicating whether the test has run and finished.
    /// </summary>
    public bool Finished => this.Result == SelfTestStepResult.Fail || this.Result == SelfTestStepResult.Pass;

    /// <summary>
    /// Gets a value indicating whether the plugin that provided this test has been unloaded.
    /// </summary>
    public bool Unloaded => this.Step == null;

    /// <summary>
    /// Gets the most recent result of running this test.
    /// </summary>
    public SelfTestStepResult Result { get; private set; } = SelfTestStepResult.NotRan;

    /// <summary>
    /// Gets the last time this test was started.
    /// </summary>
    public DateTimeOffset? StartTime { get; private set; } = null;

    /// <summary>
    /// Gets how long it took (or is taking) for this test to execute.
    /// </summary>
    public TimeSpan? Duration { get; private set; } = null;

    /// <summary>
    /// Gets or sets the Step that our results are for.
    ///
    /// If <c>null</c> it means the Plugin that provided this test has been unloaded and we can't use this test anymore.
    /// </summary>
    private ISelfTestStep? Step { get; set; }

    /// <summary>
    /// Creates a SelfTestWithResults from a Dalamud step.
    /// </summary>
    /// <param name="step">The step to wrap.</param>
    /// <returns>A new SelfTestWithResults instance.</returns>
    public static SelfTestWithResults FromDalamudStep(ISelfTestStep step)
    {
        return new SelfTestWithResults(plugin: null, group: "Dalamud", step: step);
    }

    /// <summary>
    /// Creates a SelfTestWithResults from a plugin step.
    /// </summary>
    /// <param name="plugin">The plugin providing the step.</param>
    /// <param name="step">The step to wrap.</param>
    /// <returns>A new SelfTestWithResults instance.</returns>
    public static SelfTestWithResults FromPluginStep(LocalPlugin plugin, ISelfTestStep step)
    {
        return new SelfTestWithResults(plugin: plugin, group: plugin.InternalName, step: step);
    }

    /// <summary>
    /// Reset the test.
    /// </summary>
    public void Reset()
    {
        this.Result = SelfTestStepResult.NotRan;
        this.StartTime = null;
        this.Duration = null;
    }

    /// <summary>
    /// Finish the currently running test and clean up any state. This preserves test run results.
    /// </summary>
    public void Finish()
    {
        if (this.Step == null)
        {
            return;
        }

        if (this.Result == SelfTestStepResult.NotRan)
        {
            return;
        }

        this.Step.CleanUp();
    }

    /// <summary>
    /// Steps the state of this Self Test. This should be called every frame that we care about the results of this test.
    /// </summary>
    public void DrawAndStep()
    {
        // If we've been unloaded then there's nothing to do.
        if (this.Step == null)
        {
            return;
        }

        // If we have already finished then there's nothing to do
        if (this.Finished)
        {
            return;
        }

        // Otherwise, we assume that calling this functions means we are running the test.
        if (this.Result == SelfTestStepResult.NotRan)
        {
            this.StartTime = DateTimeOffset.Now;
            this.Result = SelfTestStepResult.Waiting;
        }

        try
        {
            this.Result = this.Step.RunStep();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Step failed: {this.Name} ({this.Group})");
            this.Result = SelfTestStepResult.Fail;
        }

        this.Duration = DateTimeOffset.Now - this.StartTime;

        // If we ran and finished we need to clean up
        if (this.Finished)
        {
            this.Finish();
        }
    }

    /// <summary>
    /// Unloads the test and cleans up.
    /// </summary>
    public void Unload()
    {
        this.Finish();
        this.Step = null;
    }
}
