using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// This class represents a callsite hook. Only the specific address's instructions are replaced with this hook.
/// This is a destructive operation, no other callsite hooks can coexist at the same address.
///
/// There's no .Original for this hook type.
/// This is only intended for be for functions where the parameters provided allow you to invoke the original call.
///
/// This class was specifically added for hooking virtual function callsites.
/// Only the specific callsite hooked is modified, if the game calls the virtual function from other locations this hook will not be triggered.
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
