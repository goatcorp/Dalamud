using Dalamud.Memory;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Hooking;

/// <summary>
/// Helpers related to hooks.
/// </summary>
internal class HookUtility
{
    /// <summary>
    /// Write a message to the log containing information about a failed hooking attempt, including the address space around it.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="address">The address to describe/log around.</param>
    internal static void LogHookingErrorWithAddressSpaceInformation(Exception ex, IntPtr address)
    {
        try
        {
            Log.Error(
                ex,
                "Could not set up a hook at {Address}\n{Report}",
                Util.DescribeAddress(address),
                AddressSpaceAnalysis.BuildReport(address));
        }
        catch (Exception reportEx)
        {
            Log.Error(reportEx, "Could not build an address space report for the failed hook");
        }
    }
}
