using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for Framework task scheduling.
/// </summary>
internal class FrameworkTaskSchedulerSelfTestStep : ISelfTestStep
{
    private bool passed = false;
    private Task? task;

    /// <inheritdoc/>
    public string Name => "Test Framework Task Scheduler";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var framework = Service<Framework>.Get();

        this.task ??= Task.Run(async () =>
        {
            ThreadSafety.AssertNotMainThread();

            await framework.RunOnTick(async () =>
            {
                ThreadSafety.AssertMainThread();
                await Task.Delay(100);

                // TODO: Should we be scheduled back to the framework thread here?
                // ThreadSafety.AssertMainThread();
                ThreadSafety.AssertNotMainThread();
            }).ConfigureAwait(false);

            ThreadSafety.AssertNotMainThread();

            await framework.RunOnTick(() =>
            {
                ThreadSafety.AssertMainThread();
            });

            ThreadSafety.AssertMainThread();

            this.passed = true;
        });

        if (this.task is { IsFaulted: true } or { IsCanceled: true })
        {
            return SelfTestStepResult.Fail;
        }

        return this.passed ? SelfTestStepResult.Pass : SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        this.passed = false;
        this.task = null;
    }
}
