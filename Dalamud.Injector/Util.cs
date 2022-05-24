using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Iced.Intel;
using PeNet;
using PeNet.Header.Pe;

namespace Dalamud.Injector;

/// <summary>
/// Utility methods.
/// </summary>
internal static class Util
{
    /// <summary>
    /// Get a process module with the given name.
    /// </summary>
    /// <param name="process">Target process.</param>
    /// <param name="moduleName">Module name.</param>
    /// <returns>The requested process module.</returns>
    public static ProcessModule GetProcessModule(Process process, string moduleName)
    {
        var modules = process.Modules;
        for (var i = 0; i < modules.Count; i++)
        {
            var module = modules[i];
            if (module.ModuleName.Equals(moduleName, StringComparison.InvariantCultureIgnoreCase))
            {
                return module;
            }
        }

        throw new Exception($"Failed to find {moduleName} in target process' modules");
    }

    /// <summary>
    /// Get the exported functions from a file.
    /// </summary>
    /// <param name="filename">Target file.</param>
    /// <returns>The exported functions.</returns>
    public static ExportFunction[] GetExportedFunctions(string filename)
        => new PeFile(filename).ExportedFunctions;

    /// <summary>
    /// Get the exported function offset by name.
    /// </summary>
    /// <param name="exportFunctions">The exported functions for a given DLL.</param>
    /// <param name="functionName">The name of the exported function.</param>
    /// <returns>The exported function offset.</returns>
    public static uint GetExportedFunctionOffset(ExportFunction[] exportFunctions, string functionName)
    {
        var exportFunction = exportFunctions.FirstOrDefault(func => func.Name == functionName);

        if (exportFunction == default)
            throw new Exception($"Failed to find exported function {functionName} in target module's exports");

        return exportFunction.Address;
    }

    /// <summary>
    /// Get the exported function address by name.
    /// </summary>
    /// <param name="module">Process module.</param>
    /// <param name="exportFunctions">The exported functions for a given DLL.</param>
    /// <param name="functionName">The name of the exported function.</param>
    /// <returns>The exported function offset.</returns>
    public static IntPtr GetExportedFunctionAddress(ProcessModule module, ExportFunction[] exportFunctions, string functionName)
    {
        var offset = GetExportedFunctionOffset(exportFunctions, functionName);
        return module.BaseAddress + (int)offset;
    }

    /// <summary>
    /// Assemble the instructions in the assembler into a sequence of bytes.
    /// </summary>
    /// <param name="assembler">Assembler.</param>
    /// <returns>Assembly bytes.</returns>
    public static byte[] AssembleBytes(this Assembler assembler)
    {
        using var stream = new MemoryStream();
        assembler.Assemble(new StreamCodeWriter(stream), 0);

        stream.Position = 0;
        var reader = new StreamCodeReader(stream);

        int next;
        var bytes = new byte[stream.Length];
        while ((next = reader.ReadByte()) >= 0)
        {
            bytes[stream.Position - 1] = (byte)next;
        }

        return bytes;
    }
}
