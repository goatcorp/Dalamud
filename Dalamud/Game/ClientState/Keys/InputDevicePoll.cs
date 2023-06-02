using System;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Dalamud.Utility;

namespace Dalamud.Game.ClientState.Keys;

/// <summary>
/// This class provides events related to game inputs.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
internal sealed class InputDevicePoll : IDisposable, IServiceType
{
    private readonly Hook<UpdateInputDelegate> updateInputHook;

    /// <summary>
    /// Raised when the game is about to poll inputs.
    /// </summary>
    public event Action? OnBeforePoll;

    /// <summary>
    /// Raised when the game polled inputs.
    /// </summary>
    public event Action? OnAfterPoll;

    [ServiceManager.ServiceConstructor]
    public InputDevicePoll(SigScanner sigScanner)
    {
        // Client::System::Framework::Framework_TaskUpdateInputDevice (names from FFXIVClientStructs) calls this function
        // 48:83EC 48   | sub rsp,48 |
        // 48:895C24 50 | mov qword ptr ss: [rsp+50],rbx |
        // BA 1E000000  | mov edx,1E |
        // 48:897424 58 | mov qword ptr ss: [rsp+58],rsi |
        // 48:897C24 40 | mov qword ptr ss: [rsp+40],rdi |
        // 48:8BF9      | mov rdi,rcx |
        // 48:81C1 60040000 | add rcx,460 |
        // 0F297424 30  | movaps xmmword ptr ss: [rsp+30],xmm6 |
        // 0F28F1       | movaps xmm6,xmm1 |
        // E8 0158FDFF  | call ffxiv_dx11.7FF6CDD89970 |
        var updateInputAddr =
            sigScanner.ScanText(
                "48?????? 48???????? BA ????0000 48???????? 48???????? 48???? 4881C1????0000 0F???????? 0F???? E8");
        this.updateInputHook = Hook<UpdateInputDelegate>.FromAddress(updateInputAddr, this.OnUpdateInput);
        this.updateInputHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint UpdateInputDelegate(nint a1, nint a2);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.updateInputHook.Dispose();
    }

    private nint OnUpdateInput(nint a1, nint a2)
    {
        this.OnBeforePoll?.InvokeSafely();
        var ret = this.updateInputHook.Original(a1, a2);
        this.OnAfterPoll?.InvokeSafely();

        return ret;
    }
}
