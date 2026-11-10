using System.Runtime.InteropServices;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// Bindings for the safetyhook inline hook wrapper exported by Dalamud.Boot.
/// See Dalamud.Boot/safetyhook_api.h for the native side.
/// </summary>
internal static partial class SafetyHookNative
{
    /// <summary>
    /// Kinds of failures the wrapper can report. Must match boot_safetyhook_error_type.
    /// </summary>
    internal enum ErrorType
    {
        /// <summary>No error occurred.</summary>
        None = 0,

        /// <summary>An error occurred when allocating memory for the trampoline.</summary>
        BadAllocation = 1,

        /// <summary>An instruction in the target function could not be decoded.</summary>
        FailedToDecodeInstruction = 2,

        /// <summary>The bytes that would be moved into the trampoline contain a short jump.</summary>
        ShortJumpInTrampoline = 3,

        /// <summary>An IP-relative instruction could not be relocated into the trampoline.</summary>
        IpRelativeInstructionOutOfRange = 4,

        /// <summary>An instruction that cannot be relocated into the trampoline was found.</summary>
        UnsupportedInstructionInTrampoline = 5,

        /// <summary>The target memory could not be made writable.</summary>
        FailedToUnprotect = 6,

        /// <summary>The target function is too short to place a jump in.</summary>
        NotEnoughSpace = 7,

        /// <summary>A null handle or address was passed to the wrapper.</summary>
        InvalidHandle = 100,

        /// <summary>An exception escaped safetyhook.</summary>
        Exception = 101,
    }

    /// <summary>
    /// Creates an inline hook, initially disabled.
    /// </summary>
    /// <param name="target">The function to hook.</param>
    /// <param name="destination">The detour to redirect to.</param>
    /// <param name="error">Receives failure details.</param>
    /// <returns>An opaque handle, or 0 on failure.</returns>
    [LibraryImport("Dalamud.Boot.dll", EntryPoint = "BootSafetyHookInlineCreate")]
    internal static partial nint Create(nint target, nint destination, out Error error);

    /// <summary>
    /// Restores the target function and releases the handle, invalidating its trampoline.
    /// </summary>
    /// <param name="handle">The handle to release.</param>
    [LibraryImport("Dalamud.Boot.dll", EntryPoint = "BootSafetyHookInlineDestroy")]
    internal static partial void Destroy(nint handle);

    /// <summary>
    /// Enables the hook.
    /// </summary>
    /// <param name="handle">The handle to enable.</param>
    /// <param name="error">Receives failure details.</param>
    /// <returns>Non-zero on success.</returns>
    [LibraryImport("Dalamud.Boot.dll", EntryPoint = "BootSafetyHookInlineEnable")]
    internal static partial int Enable(nint handle, out Error error);

    /// <summary>
    /// Disables the hook.
    /// </summary>
    /// <param name="handle">The handle to disable.</param>
    /// <param name="error">Receives failure details.</param>
    /// <returns>Non-zero on success.</returns>
    [LibraryImport("Dalamud.Boot.dll", EntryPoint = "BootSafetyHookInlineDisable")]
    internal static partial int Disable(nint handle, out Error error);

    /// <summary>
    /// Checks whether the hook is currently enabled.
    /// </summary>
    /// <param name="handle">The handle to query.</param>
    /// <returns>Non-zero if enabled.</returns>
    [LibraryImport("Dalamud.Boot.dll", EntryPoint = "BootSafetyHookInlineIsEnabled")]
    internal static partial int IsEnabled(nint handle);

    /// <summary>
    /// Gets the trampoline that calls the target as if it were not hooked.
    /// </summary>
    /// <param name="handle">The handle to query.</param>
    /// <returns>The trampoline address, or 0.</returns>
    [LibraryImport("Dalamud.Boot.dll", EntryPoint = "BootSafetyHookInlineGetTrampoline")]
    internal static partial nint GetTrampoline(nint handle);

    /// <summary>
    /// Failure details written back by the wrapper. Must match boot_safetyhook_error.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Error
    {
        /// <summary>The kind of failure.</summary>
        public ErrorType Type;

        /// <summary>The safetyhook allocator error, if <see cref="Type"/> is <see cref="ErrorType.BadAllocation"/>.</summary>
        public int AllocatorError;

        /// <summary>The offending instruction, for the error kinds that report one.</summary>
        public nint Ip;

        /// <summary>
        /// Formats this error for use in a log message or an exception.
        /// </summary>
        /// <returns>A human-readable description.</returns>
        public readonly string Describe() => this.Type switch
        {
            ErrorType.BadAllocation => $"{this.Type} (allocator error {this.AllocatorError})",
            ErrorType.None or ErrorType.InvalidHandle or ErrorType.Exception => this.Type.ToString(),
            _ => $"{this.Type} (at 0x{this.Ip:X})",
        };
    }
}
