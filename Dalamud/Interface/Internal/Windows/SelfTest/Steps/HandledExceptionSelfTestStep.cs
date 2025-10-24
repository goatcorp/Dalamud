using System.Runtime.InteropServices;

using Dalamud.Plugin.SelfTest;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test dedicated to handling of Access Violations.
/// </summary>
internal class HandledExceptionSelfTestStep : ISelfTestStep
{
    /// <inheritdoc/>
    public string Name => "Test Handled Exception";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        try
        {
            Marshal.ReadByte(IntPtr.Zero);
        }
        catch (AccessViolationException)
        {
            return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Fail;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
