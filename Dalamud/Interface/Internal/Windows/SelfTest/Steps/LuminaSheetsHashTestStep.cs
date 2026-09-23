using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Plugin.SelfTest;

using Lumina.Data.Files.Excel;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Lumina.Misc;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for Lumina.Excel.Sheets.
/// </summary>
internal class LuminaSheetsHashTestStep : ISelfTestStep
{
    private Dictionary<string, Type>? sheetTypes;
    private ConcurrentDictionary<string, (uint, uint?)>? hashMismatches;
    private ParallelLoopResult? task;
    private bool initialized;

    /// <inheritdoc/>
    public string Name => "Test Sheet Hashes";

    /// <inheritdoc/>
    public void CleanUp()
    {
        this.sheetTypes = null;
        this.hashMismatches = null;
        this.task = null;
        this.initialized = false;
    }

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        this.Initialize();

        if (!this.task.HasValue || !this.task.Value.IsCompleted || this.hashMismatches == null)
            return SelfTestStepResult.Waiting;

        if (this.hashMismatches.IsEmpty)
            return SelfTestStepResult.Pass;

        var count = this.hashMismatches.Keys.Count;
        ImGui.Text($"{count} sheet{(count == 1 ? string.Empty : "s")} failed the hash check:");

        foreach (var (sheetName, (hasHash, wantHash)) in this.hashMismatches)
        {
            if (!wantHash.HasValue)
            {
                ImGui.Text($"[{sheetName}] Header file not found.");
            }
            else
            {
                ImGui.Text($"[{sheetName}] Expected {wantHash.Value:X}, got {hasHash:X}");
            }
        }

        if (ImGui.Button("Continue"u8))
        {
            return SelfTestStepResult.Fail;
        }

        return SelfTestStepResult.Waiting;
    }

    // copied from https://github.com/NotAdam/Lumina.Excel/blob/f3cc118d/src/Lumina.Excel.Generator/ColumnDefinitions.cs#L48
    private static uint? GetColumnsHash(DataManager dataManager, string sheetName)
    {
        var exh = dataManager.GetFile<ExcelHeaderFile>($"exd/{sheetName}.exh");
        if (exh == null)
            return null;

        var data = MemoryMarshal.Cast<ExcelColumnDefinition, ushort>(exh.ColumnDefinitions);

        // Column hashes are based on the file data, so we need to ensure the endianness matches
        if (BitConverter.IsLittleEndian)
        {
            var temp = data.ToArray();
            foreach (ref var el in temp.AsSpan())
                el = BinaryPrimitives.ReverseEndianness(el);
            data = temp.AsSpan();
        }

        return Crc32.Get(MemoryMarshal.Cast<ushort, byte>(data));
    }

    private void Initialize()
    {
        if (this.initialized)
            return;

        var dataManager = Service<DataManager>.Get();
        this.hashMismatches = [];

        var sheetsType = typeof(Lumina.Excel.Sheets.Achievement);
        this.sheetTypes = sheetsType.Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == sheetsType.Namespace && Attribute.IsDefined(type, typeof(SheetAttribute)) && !string.IsNullOrEmpty(type.GetCustomAttribute<SheetAttribute>().Name))
            .ToDictionary(type => type.GetCustomAttribute<SheetAttribute>().Name, StringComparer.Ordinal);

        this.task = Parallel.ForEach(this.sheetTypes, kv =>
        {
            var (sheetName, sheetType) = kv;

            var haveHash = sheetType.GetCustomAttribute<SheetAttribute>().ColumnHash;
            if (!haveHash.HasValue)
                return;

            var wantHash = GetColumnsHash(dataManager, sheetName);
            if (!wantHash.HasValue || haveHash.Value != wantHash.Value)
            {
                this.hashMismatches.TryAdd(sheetName, (haveHash.Value, wantHash));
            }
        });

        this.initialized = true;
    }
}
