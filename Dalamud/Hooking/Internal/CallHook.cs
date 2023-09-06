using System;
using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Hooking class for callsite hooking. This hook does not have capabilities of calling the original function.
/// The intended use is replacing virtual function calls where you are able to manually invoke the original call using the delegate arguments.
/// </summary>
/// <typeparam name="T">Delegate signature for this hook.</typeparam>
internal class CallHook<T> : IDisposable where T : Delegate
{
    private readonly Reloaded.Hooks.AsmHook asmHook;
    
    private T? detour;
    private bool activated;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallHook{T}"/> class.
    /// </summary>
    /// <param name="address">Address of the instruction to replace.</param>
    /// <param name="detour">Delegate to invoke.</param>
    internal CallHook(nint address, T detour)
    {
        this.detour = detour;

        var detourPtr = Marshal.GetFunctionPointerForDelegate(this.detour);
        var code = new[]
        {
            "use64",
            $"mov rax, 0x{detourPtr:X8}",
            "call rax",
        };
        
        var opt = new AsmHookOptions
        {
            PreferRelativeJump = true,
            Behaviour = Reloaded.Hooks.Definitions.Enums.AsmHookBehaviour.DoNotExecuteOriginal,
            MaxOpcodeSize = 5,
        };
        
        this.asmHook = new Reloaded.Hooks.AsmHook(code, (nuint)address, opt);
    }

    /// <summary>
    /// Gets a value indicating whether or not the hook is enabled.
    /// </summary>
    public bool IsEnabled => this.asmHook.IsEnabled;
    
    /// <summary>
    /// Starts intercepting a call to the function.
    /// </summary>
    public void Enable()
    {
        if (!this.activated)
        {
            this.activated = true;
            this.asmHook.Activate();
            return;
        }
        
        this.asmHook.Enable();
    }

    /// <summary>
    /// Stops intercepting a call to the function.
    /// </summary>
    public void Disable()
    {
        this.asmHook.Disable();
    }

    /// <summary>
    /// Remove a hook from the current process.
    /// </summary>
    public void Dispose()
    {
        this.asmHook.Disable();
        this.detour = null;
    }
}
