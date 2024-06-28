using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// This class represents a callsite hook used to replace the address of the OnSetup function in r9.
/// </summary>
/// <typeparam name="T">Delegate signature for this hook.</typeparam>
internal class AddonSetupHook<T> : IDisposable where T : Delegate
{
    private readonly Reloaded.Hooks.AsmHook asmHook;
    
    private T? detour;
    private bool activated;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonSetupHook{T}"/> class.
    /// </summary>
    /// <param name="address">Address of the instruction to replace.</param>
    /// <param name="detour">Delegate to invoke.</param>
    internal AddonSetupHook(nint address, T detour)
    {
        this.detour = detour;

        var detourPtr = Marshal.GetFunctionPointerForDelegate(this.detour);
        var code = new[]
        {
            "use64",
            $"mov r9, 0x{detourPtr:X8}",
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
