using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging.Internal;
using Dalamud.Networking.Http;
using ImGuiNET;
using Microsoft.Win32;
using Serilog;

namespace Dalamud.Utility;

/// <summary>
/// Class providing various helper methods for use in Dalamud and plugins.
/// </summary>
public static class Util
{
    private static string? gitHashInternal;
    private static int? gitCommitCountInternal;
    private static string? gitHashClientStructsInternal;

    private static ulong moduleStartAddr;
    private static ulong moduleEndAddr;

    /// <summary>
    /// Gets an httpclient for usage.
    /// Do NOT await this.
    /// </summary>
    [Obsolete($"Use Service<{nameof(HappyHttpClient)}> instead.")]
    public static HttpClient HttpClient { get; } = Service<HappyHttpClient>.Get().SharedHttpClient;

    /// <summary>
    /// Gets the assembly version of Dalamud.
    /// </summary>
    public static string AssemblyVersion { get; } = Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();

    /// <summary>
    /// Check two byte arrays for equality.
    /// </summary>
    /// <param name="a1">The first byte array.</param>
    /// <param name="a2">The second byte array.</param>
    /// <returns>Whether or not the byte arrays are equal.</returns>
    public static unsafe bool FastByteArrayCompare(byte[]? a1, byte[]? a2)
    {
        // Copyright (c) 2008-2013 Hafthor Stefansson
        // Distributed under the MIT/X11 software license
        // Ref: http://www.opensource.org/licenses/mit-license.php.
        // https://stackoverflow.com/a/8808245

        if (a1 == a2) return true;
        if (a1 == null || a2 == null || a1.Length != a2.Length)
            return false;
        fixed (byte* p1 = a1, p2 = a2)
        {
            byte* x1 = p1, x2 = p2;
            var l = a1.Length;
            for (var i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
            {
                if (*((long*)x1) != *((long*)x2))
                    return false;
            }

            if ((l & 4) != 0)
            {
                if (*((int*)x1) != *((int*)x2))
                    return false;
                x1 += 4;
                x2 += 4;
            }

            if ((l & 2) != 0)
            {
                if (*((short*)x1) != *((short*)x2))
                    return false;
                x1 += 2;
                x2 += 2;
            }

            if ((l & 1) != 0)
            {
                if (*((byte*)x1) != *((byte*)x2))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Gets the git hash value from the assembly
    /// or null if it cannot be found.
    /// </summary>
    /// <returns>The git hash of the assembly.</returns>
    public static string GetGitHash()
    {
        if (gitHashInternal != null)
            return gitHashInternal;

        var asm = typeof(Util).Assembly;
        var attrs = asm.GetCustomAttributes<AssemblyMetadataAttribute>();

        gitHashInternal = attrs.First(a => a.Key == "GitHash").Value;

        return gitHashInternal;
    }

    /// <summary>
    /// Gets the amount of commits in the current branch, or null if undetermined.
    /// </summary>
    /// <returns>The amount of commits in the current branch.</returns>
    public static int? GetGitCommitCount()
    {
        if (gitCommitCountInternal != null)
            return gitCommitCountInternal.Value;

        var asm = typeof(Util).Assembly;
        var attrs = asm.GetCustomAttributes<AssemblyMetadataAttribute>();

        var value = attrs.First(a => a.Key == "GitCommitCount").Value;
        if (value == null)
            return null;

        gitCommitCountInternal = int.Parse(value);
        return gitCommitCountInternal.Value;
    }

    /// <summary>
    /// Gets the git hash value from the assembly
    /// or null if it cannot be found.
    /// </summary>
    /// <returns>The git hash of the assembly.</returns>
    public static string GetGitHashClientStructs()
    {
        if (gitHashClientStructsInternal != null)
            return gitHashClientStructsInternal;

        var asm = typeof(Util).Assembly;
        var attrs = asm.GetCustomAttributes<AssemblyMetadataAttribute>();

        gitHashClientStructsInternal = attrs.First(a => a.Key == "GitHashClientStructs").Value;

        return gitHashClientStructsInternal;
    }

    /// <summary>
    /// Read memory from an offset and hexdump them via Serilog.
    /// </summary>
    /// <param name="offset">The offset to read from.</param>
    /// <param name="len">The length to read.</param>
    public static void DumpMemory(IntPtr offset, int len = 512)
    {
        try
        {
            SafeMemory.ReadBytes(offset, len, out var data);
            Log.Information(ByteArrayToHex(data));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Read failed");
        }
    }

    /// <summary>
    /// Create a hexdump of the provided bytes.
    /// </summary>
    /// <param name="bytes">The bytes to hexdump.</param>
    /// <param name="offset">The offset in the byte array to start at.</param>
    /// <param name="bytesPerLine">The amount of bytes to display per line.</param>
    /// <returns>The generated hexdump in string form.</returns>
    public static string ByteArrayToHex(byte[] bytes, int offset = 0, int bytesPerLine = 16)
    {
        if (bytes == null) return string.Empty;

        var hexChars = "0123456789ABCDEF".ToCharArray();

        var offsetBlock = 8 + 3;
        var byteBlock = offsetBlock + (bytesPerLine * 3) + ((bytesPerLine - 1) / 8) + 2;
        var lineLength = byteBlock + bytesPerLine + Environment.NewLine.Length;

        var line = (new string(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
        var numLines = (bytes.Length + bytesPerLine - 1) / bytesPerLine;

        var sb = new StringBuilder(numLines * lineLength);

        for (var i = 0; i < bytes.Length; i += bytesPerLine)
        {
            var h = i + offset;

            line[0] = hexChars[(h >> 28) & 0xF];
            line[1] = hexChars[(h >> 24) & 0xF];
            line[2] = hexChars[(h >> 20) & 0xF];
            line[3] = hexChars[(h >> 16) & 0xF];
            line[4] = hexChars[(h >> 12) & 0xF];
            line[5] = hexChars[(h >> 8) & 0xF];
            line[6] = hexChars[(h >> 4) & 0xF];
            line[7] = hexChars[(h >> 0) & 0xF];

            var hexColumn = offsetBlock;
            var charColumn = byteBlock;

            for (var j = 0; j < bytesPerLine; j++)
            {
                if (j > 0 && (j & 7) == 0) hexColumn++;

                if (i + j >= bytes.Length)
                {
                    line[hexColumn] = ' ';
                    line[hexColumn + 1] = ' ';
                    line[charColumn] = ' ';
                }
                else
                {
                    var by = bytes[i + j];
                    line[hexColumn] = hexChars[(by >> 4) & 0xF];
                    line[hexColumn + 1] = hexChars[by & 0xF];
                    line[charColumn] = by < 32 ? '.' : (char)by;
                }

                hexColumn += 3;
                charColumn++;
            }

            sb.Append(line);
        }

        return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
    }

    /// <summary>
    /// Show a structure in an ImGui context.
    /// </summary>
    /// <param name="obj">The structure to show.</param>
    /// <param name="addr">The address to the structure.</param>
    /// <param name="autoExpand">Whether or not this structure should start out expanded.</param>
    /// <param name="path">The already followed path.</param>
    public static void ShowStruct(object obj, ulong addr, bool autoExpand = false, IEnumerable<string>? path = null)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3, 2));
        path ??= new List<string>();

        if (moduleEndAddr == 0 && moduleStartAddr == 0)
        {
            try
            {
                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule != null)
                {
                    moduleStartAddr = (ulong)processModule.BaseAddress.ToInt64();
                    moduleEndAddr = moduleStartAddr + (ulong)processModule.ModuleMemorySize;
                }
                else
                {
                    moduleEndAddr = 1;
                }
            }
            catch
            {
                moduleEndAddr = 1;
            }
        }

        ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
        if (autoExpand)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
        }

        if (ImGui.TreeNode($"{obj}##print-obj-{addr:X}-{string.Join("-", path)}"))
        {
            ImGui.PopStyleColor();
            foreach (var f in obj.GetType().GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance))
            {
                var fixedBuffer = (FixedBufferAttribute)f.GetCustomAttribute(typeof(FixedBufferAttribute));
                if (fixedBuffer != null)
                {
                    ImGui.Text($"fixed");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{fixedBuffer.ElementType.Name}[0x{fixedBuffer.Length:X}]");
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{f.FieldType.Name}");
                }

                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.4f, 1), $"{f.Name}: ");
                ImGui.SameLine();

                ShowValue(addr, new List<string>(path) { f.Name }, f.FieldType, f.GetValue(obj));
            }

            foreach (var p in obj.GetType().GetProperties().Where(p => p.GetGetMethod()?.GetParameters().Length == 0))
            {
                ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{p.PropertyType.Name}");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1), $"{p.Name}: ");
                ImGui.SameLine();

                ShowValue(addr, new List<string>(path) { p.Name }, p.PropertyType, p.GetValue(obj));
            }

            ImGui.TreePop();
        }
        else
        {
            ImGui.PopStyleColor();
        }

        ImGui.PopStyleVar();
    }

    /// <summary>
    /// Show a structure in an ImGui context.
    /// </summary>
    /// <typeparam name="T">The type of the structure.</typeparam>
    /// <param name="obj">The pointer to the structure.</param>
    /// <param name="autoExpand">Whether or not this structure should start out expanded.</param>
    public static unsafe void ShowStruct<T>(T* obj, bool autoExpand = false) where T : unmanaged
    {
        ShowStruct(*obj, (ulong)&obj, autoExpand);
    }

    /// <summary>
    /// Show a GameObject's internal data in an ImGui-context.
    /// </summary>
    /// <param name="go">The GameObject to show.</param>
    /// <param name="autoExpand">Whether or not the struct should start as expanded.</param>
    public static unsafe void ShowGameObjectStruct(GameObject go, bool autoExpand = true)
    {
        switch (go)
        {
            case BattleChara bchara:
                ShowStruct(bchara.Struct, autoExpand);
                break;
            case Character chara:
                ShowStruct(chara.Struct, autoExpand);
                break;
            default:
                ShowStruct(go.Struct, autoExpand);
                break;
        }
    }

    /// <summary>
    /// Show all properties and fields of the provided object via ImGui.
    /// </summary>
    /// <param name="obj">The object to show.</param>
    public static void ShowObject(object obj)
    {
        var type = obj.GetType();

        ImGui.Text($"Object Dump({type.Name}) for {obj}({obj.GetHashCode()})");

        ImGuiHelpers.ScaledDummy(5);

        ImGui.TextColored(ImGuiColors.DalamudOrange, "-> Properties:");

        ImGui.Indent();

        foreach (var propertyInfo in type.GetProperties().Where(p => p.GetGetMethod()?.GetParameters().Length == 0))
        {
            var value = propertyInfo.GetValue(obj);
            var valueType = value?.GetType();
            if (valueType == typeof(IntPtr))
                ImGui.TextColored(ImGuiColors.DalamudOrange, $"    {propertyInfo.Name}: 0x{value:X}");
            else
                ImGui.TextColored(ImGuiColors.DalamudOrange, $"    {propertyInfo.Name}: {value}");
        }

        ImGui.Unindent();

        ImGuiHelpers.ScaledDummy(5);

        ImGui.TextColored(ImGuiColors.HealerGreen, "-> Fields:");

        ImGui.Indent();

        foreach (var fieldInfo in type.GetFields())
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, $"    {fieldInfo.Name}: {fieldInfo.GetValue(obj)}");
        }

        ImGui.Unindent();
    }

    /// <summary>
    /// Display an error MessageBox and exit the current process.
    /// </summary>
    /// <param name="message">MessageBox body.</param>
    /// <param name="caption">MessageBox caption (title).</param>
    /// <param name="exit">Specify whether to exit immediately.</param>
    public static void Fatal(string message, string caption, bool exit = true)
    {
        var flags = NativeFunctions.MessageBoxType.Ok | NativeFunctions.MessageBoxType.IconError | NativeFunctions.MessageBoxType.Topmost;
        _ = NativeFunctions.MessageBoxW(Process.GetCurrentProcess().MainWindowHandle, message, caption, flags);

        if (exit)
            Environment.Exit(-1);
    }

    /// <summary>
    /// Transform byte count to human readable format.
    /// </summary>
    /// <param name="bytes">Number of bytes.</param>
    /// <returns>Human readable version.</returns>
    public static string FormatBytes(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i;
        double dblSByte = bytes;
        for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblSByte = bytes / 1024.0;
        }

        return $"{dblSByte:0.00} {suffix[i]}";
    }

    /// <summary>
    /// Retrieve a UTF8 string from a null terminated byte array.
    /// </summary>
    /// <param name="array">A null terminated UTF8 byte array.</param>
    /// <returns>A UTF8 encoded string.</returns>
    public static string GetUTF8String(byte[] array)
    {
        var count = 0;
        for (; count < array.Length; count++)
        {
            if (array[count] == 0)
                break;
        }

        string text;
        if (count == array.Length)
        {
            text = Encoding.UTF8.GetString(array);
            Log.Warning($"Warning: text exceeds underlying array length ({text})");
        }
        else
        {
            text = Encoding.UTF8.GetString(array, 0, count);
        }

        return text;
    }

    /// <summary>
    /// Compress a string using GZip.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>The compressed output bytes.</returns>
    public static byte[] CompressString(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using (var gs = new GZipStream(mso, CompressionMode.Compress))
        {
            msi.CopyTo(gs);
        }

        return mso.ToArray();
    }

    /// <summary>
    /// Decompress a string using GZip.
    /// </summary>
    /// <param name="bytes">The input bytes.</param>
    /// <returns>The compressed output string.</returns>
    public static string DecompressString(byte[] bytes)
    {
        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using (var gs = new GZipStream(msi, CompressionMode.Decompress))
        {
            gs.CopyTo(mso);
        }

        return Encoding.UTF8.GetString(mso.ToArray());
    }

    /// <summary>
    /// Copy one stream to another.
    /// </summary>
    /// <param name="src">The source stream.</param>
    /// <param name="dest">The destination stream.</param>
    /// <param name="len">The maximum length to copy.</param>
    [Obsolete("Use Stream.CopyTo() instead", true)]
    public static void CopyTo(Stream src, Stream dest, int len = 4069)
    {
        var bytes = new byte[len];
        int cnt;

        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) dest.Write(bytes, 0, cnt);
    }

    /// <summary>
    /// Heuristically determine if Dalamud is running on Linux/WINE.
    /// </summary>
    /// <returns>Whether or not Dalamud is running on Linux/WINE.</returns>
    public static bool IsLinux()
    {
        bool Check1()
        {
            return EnvironmentConfiguration.XlWineOnLinux;
        }

        bool Check2()
        {
            var hModule = NativeFunctions.GetModuleHandleW("ntdll.dll");
            var proc1 = NativeFunctions.GetProcAddress(hModule, "wine_get_version");
            var proc2 = NativeFunctions.GetProcAddress(hModule, "wine_get_build_id");

            return proc1 != IntPtr.Zero || proc2 != IntPtr.Zero;
        }

        bool Check3()
        {
            return Registry.CurrentUser.OpenSubKey(@"Software\Wine") != null ||
                   Registry.LocalMachine.OpenSubKey(@"Software\Wine") != null;
        }

        return Check1() || Check2() || Check3();
    }

    /// <summary>
    /// Heuristically determine if the Windows version is higher than Windows 11's first build.
    /// </summary>
    /// <returns>If Windows 11 has been detected.</returns>
    public static bool IsWindows11() => Environment.OSVersion.Version.Build >= 22000;

    /// <summary>
    /// Open a link in the default browser.
    /// </summary>
    /// <param name="url">The link to open.</param>
    public static void OpenLink(string url)
    {
        var process = new ProcessStartInfo(url)
        {
            UseShellExecute = true,
        };
        Process.Start(process);
    }

    /// <summary>
    /// Perform a "zipper merge" (A, 1, B, 2, C, 3) of multiple enumerables, allowing for lists to end early.
    /// </summary>
    /// <param name="sources">A set of enumerable sources to combine.</param>
    /// <typeparam name="TSource">The resulting type of the merged list to return.</typeparam>
    /// <returns>A new enumerable, consisting of the final merge of all lists.</returns>
    public static IEnumerable<TSource> ZipperMerge<TSource>(params IEnumerable<TSource>[] sources)
    {
        // Borrowed from https://codereview.stackexchange.com/a/263451, thank you!
        var enumerators = new IEnumerator<TSource>[sources.Length];
        try
        {
            for (var i = 0; i < sources.Length; i++)
            {
                enumerators[i] = sources[i].GetEnumerator();
            }

            var hasNext = new bool[enumerators.Length];

            bool MoveNext()
            {
                var anyHasNext = false;
                for (var i = 0; i < enumerators.Length; i++)
                {
                    anyHasNext |= hasNext[i] = enumerators[i].MoveNext();
                }

                return anyHasNext;
            }

            while (MoveNext())
            {
                for (var i = 0; i < enumerators.Length; i++)
                {
                    if (hasNext[i])
                    {
                        yield return enumerators[i].Current;
                    }
                }
            }
        }
        finally
        {
            foreach (var enumerator in enumerators)
            {
                enumerator?.Dispose();
            }
        }
    }

    /// <summary>
    /// Dispose this object.
    /// </summary>
    /// <param name="obj">The object to dispose.</param>
    /// <typeparam name="T">The type of object to dispose.</typeparam>
    internal static void ExplicitDispose<T>(this T obj) where T : IDisposable
    {
        obj.Dispose();
    }

    /// <summary>
    /// Dispose this object.
    /// </summary>
    /// <param name="obj">The object to dispose.</param>
    /// <param name="logMessage">Log message to print, if specified and an error occurs.</param>
    /// <param name="moduleLog">Module logger, if any.</param>
    /// <typeparam name="T">The type of object to dispose.</typeparam>
    internal static void ExplicitDisposeIgnoreExceptions<T>(this T obj, string? logMessage = null, ModuleLog? moduleLog = null) where T : IDisposable
    {
        try
        {
            obj.Dispose();
        }
        catch (Exception e)
        {
            if (logMessage == null)
                return;

            if (moduleLog != null)
                moduleLog.Error(e, logMessage);
            else
                Log.Error(e, logMessage);
        }
    }

    /// <summary>
    /// Overwrite text in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="text">The text to write.</param>
    internal static void WriteAllTextSafe(string path, string text)
    {
        var tmpPath = path + ".tmp";
        if (File.Exists(tmpPath))
            File.Delete(tmpPath);

        File.WriteAllText(tmpPath, text);
        File.Move(tmpPath, path, true);
    }

    private static unsafe void ShowValue(ulong addr, IEnumerable<string> path, Type type, object value)
    {
        if (type.IsPointer)
        {
            var val = (Pointer)value;
            var unboxed = Pointer.Unbox(val);
            if (unboxed != null)
            {
                var unboxedAddr = (ulong)unboxed;
                ImGuiHelpers.ClickToCopyText($"{(ulong)unboxed:X}");
                if (moduleStartAddr > 0 && unboxedAddr >= moduleStartAddr && unboxedAddr <= moduleEndAddr)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, 0xffcbc0ff);
                    ImGuiHelpers.ClickToCopyText($"ffxiv_dx11.exe+{unboxedAddr - moduleStartAddr:X}");
                    ImGui.PopStyleColor();
                }

                try
                {
                    var eType = type.GetElementType();
                    var ptrObj = SafeMemory.PtrToStructure(new IntPtr(unboxed), eType);
                    ImGui.SameLine();
                    if (ptrObj == null)
                    {
                        ImGui.Text("null or invalid");
                    }
                    else
                    {
                        ShowStruct(ptrObj, (ulong)unboxed, path: new List<string>(path));
                    }
                }
                catch
                {
                    // Ignored
                }
            }
            else
            {
                ImGui.Text("null");
            }
        }
        else
        {
            if (!type.IsPrimitive)
            {
                ShowStruct(value, addr, path: new List<string>(path));
            }
            else
            {
                ImGui.Text($"{value}");
            }
        }
    }
}
