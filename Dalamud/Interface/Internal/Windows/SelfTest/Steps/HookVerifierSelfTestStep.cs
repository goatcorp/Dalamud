using Dalamud.Hooking.Internal.Verification;
using Dalamud.Plugin.SelfTest;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Serilog;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Class implementing a self-test for the <see cref="HookVerifier"/>.
/// It tests both a valid and an invalid method to ensure the verifier is working correctly.
/// </summary>
internal unsafe class HookVerifierSelfTestStep : ISelfTestStep
{
    private delegate void HideHeadgearGood(DrawDataContainer* thisPtr, uint unk, bool hide);

    private delegate void HideHeadgearBad(DrawDataContainer* thisPtr, byte unk, bool hide);

    /// <inheritdoc/>
    public string Name => "Hook Verifier";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var resultGood = HookVerifier.TryVerify<HideHeadgearGood>(
            DrawDataContainer.Addresses.HideHeadgear.Value,
            typeof(HookVerifierSelfTestStep).Assembly,
            out var exceptionsGood);

        if (!resultGood || exceptionsGood.Length != 0)
        {
            Log.Error("HookVerifier failed to verify a valid method. Exceptions: {@Exceptions}", exceptionsGood);
            return SelfTestStepResult.Fail;
        }

        var resultBad = HookVerifier.TryVerify<HideHeadgearBad>(
            DrawDataContainer.Addresses.HideHeadgear.Value,
            typeof(HookVerifierSelfTestStep).Assembly,
            out var exceptionsBad);

        if (resultBad || exceptionsBad.Length == 0)
        {
            Log.Error("HookVerifier failed to verify an invalid method.");
            return SelfTestStepResult.Fail;
        }

        return SelfTestStepResult.Pass;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // Ignored
    }
}
