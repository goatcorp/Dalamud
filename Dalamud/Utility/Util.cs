using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Serilog;
using Windows.Win32.Storage.FileSystem;

namespace Dalamud.Utility;

/// <summary>
/// Class providing various helper methods for use in Dalamud and plugins.
/// </summary>
public static class Util
{
    private static readonly Type GenericSpanType = typeof(Span<>);
    private static string? gitHashInternal;
    private static int? gitCommitCountInternal;
    private static string? gitHashClientStructsInternal;

    private static ulong moduleStartAddr;
    private static ulong moduleEndAddr;

    /// <summary>
    /// Gets the assembly version of Dalamud.
    /// </summary>
    public static string AssemblyVersion { get; } =
        Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();

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
    /// <param name="hideAddress">Do not print addresses. Use when displaying a copied value.</param>
    public static void ShowStruct(object obj, ulong addr, bool autoExpand = false, IEnumerable<string>? path = null, bool hideAddress = false)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3, 2));
        path ??= new List<string>();
        var pathList = path is List<string> ? (List<string>)path : path.ToList();

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

        if (ImGui.TreeNode($"{obj}##print-obj-{addr:X}-{string.Join("-", pathList)}"))
        {
            ImGui.PopStyleColor();
            foreach (var f in obj.GetType()
                                 .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance))
            {
                var fixedBuffer = (FixedBufferAttribute)f.GetCustomAttribute(typeof(FixedBufferAttribute));
                if (fixedBuffer != null)
                {
                    ImGui.Text($"fixed");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1),
                                      $"{fixedBuffer.ElementType.Name}[0x{fixedBuffer.Length:X}]");
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{f.FieldType.Name}");
                }

                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.4f, 1), $"{f.Name}: ");
                ImGui.SameLine();

                pathList.Add(f.Name);
                try
                {
                    if (f.FieldType.IsGenericType && (f.FieldType.IsByRef || f.FieldType.IsByRefLike))
                        ImGui.Text("Cannot preview ref typed fields."); // object never contains ref struct
                    else
                        ShowValue(addr, pathList, f.FieldType, f.GetValue(obj), hideAddress);
                }
                catch (Exception ex)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.4f, 0.4f, 1f));
                    ImGui.TextUnformatted($"Error: {ex.GetType().Name}: {ex.Message}");
                    ImGui.PopStyleColor();
                }
                finally
                {
                    pathList.RemoveAt(pathList.Count - 1);
                }
            }

            foreach (var p in obj.GetType().GetProperties().Where(p => p.GetGetMethod()?.GetParameters().Length == 0))
            {
                ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{p.PropertyType.Name}");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1), $"{p.Name}: ");
                ImGui.SameLine();

                pathList.Add(p.Name);
                try
                {
                    if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == GenericSpanType)
                        ShowSpanProperty(addr, pathList, p, obj);
                    else if (p.PropertyType.IsGenericType && (p.PropertyType.IsByRef || p.PropertyType.IsByRefLike))
                        ImGui.Text("Cannot preview ref typed properties.");
                    else
                        ShowValue(addr, pathList, p.PropertyType, p.GetValue(obj), hideAddress);
                }
                catch (Exception ex)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.4f, 0.4f, 1f));
                    ImGui.TextUnformatted($"Error: {ex.GetType().Name}: {ex.Message}");
                    ImGui.PopStyleColor();
                }
                finally
                {
                    pathList.RemoveAt(pathList.Count - 1);
                }
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

        foreach (var p in type.GetProperties().Where(p => p.GetGetMethod()?.GetParameters().Length == 0))
        {
            if (p.PropertyType.IsGenericType && (p.PropertyType.IsByRef || p.PropertyType.IsByRefLike))
            {
                ImGui.TextColored(ImGuiColors.DalamudOrange, $"    {p.Name}: (ref typed property)");
            }
            else
            {
                var value = p.GetValue(obj);
                var valueType = value?.GetType();
                if (valueType == typeof(IntPtr))
                    ImGui.TextColored(ImGuiColors.DalamudOrange, $"    {p.Name}: 0x{value:X}");
                else
                    ImGui.TextColored(ImGuiColors.DalamudOrange, $"    {p.Name}: {value}");
            }
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
        var flags = NativeFunctions.MessageBoxType.Ok | NativeFunctions.MessageBoxType.IconError |
                    NativeFunctions.MessageBoxType.Topmost;
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
        string[] suffix = {"B", "KB", "MB", "GB", "TB"};
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
    /// Determine if Dalamud is currently running within a Wine context (e.g. either on macOS or Linux). This method
    /// will not return information about the host operating system.
    /// </summary>
    /// <returns>Returns true if Wine is detected, false otherwise.</returns>
    public static bool IsWine()
    {
        if (EnvironmentConfiguration.XlWineOnLinux) return true;
        if (Environment.GetEnvironmentVariable("XL_PLATFORM") is not null and not "Windows") return true;

        var ntdll = NativeFunctions.GetModuleHandleW("ntdll.dll");

        // Test to see if any Wine specific exports exist. If they do, then we are running on Wine.
        // The exports "wine_get_version", "wine_get_build_id", and "wine_get_host_version" will tend to be hidden
        // by most Linux users (else FFXIV will want a macOS license), so we will additionally check some lesser-known
        // exports as well.
        return AnyProcExists(
            ntdll,
            "wine_get_version",
            "wine_get_build_id",
            "wine_get_host_version",
            "wine_server_call",
            "wine_unix_to_nt_file_name");

        bool AnyProcExists(nint handle, params string[] procs) =>
            procs.Any(p => NativeFunctions.GetProcAddress(handle, p) != nint.Zero);
    }

    /// <summary>
    /// Gets the best guess for the current host's platform based on the <c>XL_PLATFORM</c> environment variable or
    /// heuristics.
    /// </summary>
    /// <remarks>
    /// macOS users running without <c>XL_PLATFORM</c> being set will be reported as Linux users. Due to the way our
    /// Wines work, there isn't a great (consistent) way to split the two apart if we're not told.
    /// </remarks>
    /// <returns>Returns the <see cref="OSPlatform"/> that Dalamud is currently running on.</returns>
    public static OSPlatform GetHostPlatform()
    {
        switch (Environment.GetEnvironmentVariable("XL_PLATFORM"))
        {
            case "Windows": return OSPlatform.Windows;
            case "MacOS": return OSPlatform.OSX;
            case "Linux": return OSPlatform.Linux;
        }
        
        // n.b. we had some fancy code here to check if the Wine host version returned "Darwin" but apparently
        // *all* our Wines report Darwin if exports aren't hidden. As such, it is effectively impossible (without some
        // (very cursed and inaccurate heuristics) to determine if we're on macOS or Linux unless we're explicitly told
        // by our launcher. See commit a7aacb15e4603a367e2f980578271a9a639d8852 for the old check.
        
        return IsWine() ? OSPlatform.Linux : OSPlatform.Windows;
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
    /// Request that Windows flash the game window to grab the user's attention.
    /// </summary>
    /// <param name="flashIfOpen">Attempt to flash even if the game is currently focused.</param>
    public static void FlashWindow(bool flashIfOpen = false)
    {
        if (NativeFunctions.ApplicationIsActivated() && !flashIfOpen)
            return;

        var flashInfo = new NativeFunctions.FlashWindowInfo
        {
            Size = (uint)Marshal.SizeOf<NativeFunctions.FlashWindowInfo>(),
            Count = uint.MaxValue,
            Timeout = 0,
            Flags = NativeFunctions.FlashWindow.All | NativeFunctions.FlashWindow.TimerNoFG,
            Hwnd = Process.GetCurrentProcess().MainWindowHandle,
        };

        NativeFunctions.FlashWindowEx(ref flashInfo);
    }

    /// <summary>
    /// Overwrite text in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="text">The text to write.</param>
    public static void WriteAllTextSafe(string path, string text)
    {
        WriteAllTextSafe(path, text, Encoding.UTF8);
    }
    
    /// <summary>
    /// Overwrite text in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="encoding">Encoding to use.</param>
    public static void WriteAllTextSafe(string path, string text, Encoding encoding)
    {
        WriteAllBytesSafe(path, encoding.GetBytes(text));
    }
    
    /// <summary>
    /// Overwrite data in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="bytes">The data to write.</param>
    public static unsafe void WriteAllBytesSafe(string path, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        
        // Open the temp file
        var tempPath = path + ".tmp";

        using var tempFile = Windows.Win32.PInvoke.CreateFile(
            tempPath, 
            (uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE), 
            FILE_SHARE_MODE.FILE_SHARE_NONE,
            null,
            FILE_CREATION_DISPOSITION.CREATE_ALWAYS,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
            null);

        if (tempFile.IsInvalid)
            throw new Win32Exception();
        
        // Write the data
        uint bytesWritten = 0;
        if (!Windows.Win32.PInvoke.WriteFile(tempFile, new ReadOnlySpan<byte>(bytes), &bytesWritten, null))
            throw new Win32Exception();

        if (bytesWritten != bytes.Length)
            throw new Exception($"Could not write all bytes to temp file ({bytesWritten} of {bytes.Length})");

        if (!Windows.Win32.PInvoke.FlushFileBuffers(tempFile))
            throw new Win32Exception();
        
        tempFile.Close();

        if (!Windows.Win32.PInvoke.MoveFileEx(tempPath, path, MOVE_FILE_FLAGS.MOVEFILE_REPLACE_EXISTING | MOVE_FILE_FLAGS.MOVEFILE_WRITE_THROUGH))
            throw new Win32Exception();
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
    internal static void ExplicitDisposeIgnoreExceptions<T>(
        this T obj, string? logMessage = null, ModuleLog? moduleLog = null) where T : IDisposable
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
    /// Gets a random, inoffensive, human-friendly string.
    /// </summary>
    /// <returns>A random human-friendly name.</returns>
    internal static string GetRandomName()
    {
        var data = Service<DataManager>.Get();
        var names = data.GetExcelSheet<BNpcName>(ClientLanguage.English)!;
        var rng = new Random();

        return names.ElementAt(rng.Next(0, names.Count() - 1)).Singular.RawString;
    }

    /// <summary>
    /// Print formatted GameObject Information to ImGui.
    /// </summary>
    /// <param name="actor">Game Object to Display.</param>
    /// <param name="tag">Display Tag.</param>
    /// <param name="resolveGameData">If the GameObjects data should be resolved.</param>
    internal static void PrintGameObject(GameObject actor, string tag, bool resolveGameData)
    {
        var actorString =
            $"{actor.Address.ToInt64():X}:{actor.ObjectId:X}[{tag}] - {actor.ObjectKind} - {actor.Name} - X{actor.Position.X} Y{actor.Position.Y} Z{actor.Position.Z} D{actor.YalmDistanceX} R{actor.Rotation} - Target: {actor.TargetObjectId:X}\n";

        if (actor is Npc npc)
            actorString += $"       DataId: {npc.DataId}  NameId:{npc.NameId}\n";

        if (actor is Character chara)
        {
            actorString +=
                $"       Level: {chara.Level} ClassJob: {(resolveGameData ? chara.ClassJob.GameData?.Name : chara.ClassJob.Id.ToString())} CHP: {chara.CurrentHp} MHP: {chara.MaxHp} CMP: {chara.CurrentMp} MMP: {chara.MaxMp}\n       Customize: {BitConverter.ToString(chara.Customize).Replace("-", " ")} StatusFlags: {chara.StatusFlags}\n";
        }

        if (actor is PlayerCharacter pc)
        {
            actorString +=
                $"       HomeWorld: {(resolveGameData ? pc.HomeWorld.GameData?.Name : pc.HomeWorld.Id.ToString())} CurrentWorld: {(resolveGameData ? pc.CurrentWorld.GameData?.Name : pc.CurrentWorld.Id.ToString())} FC: {pc.CompanyTag}\n";
        }

        ImGui.TextUnformatted(actorString);
        ImGui.SameLine();
        if (ImGui.Button($"C##{actor.Address.ToInt64()}"))
        {
            ImGui.SetClipboardText(actor.Address.ToInt64().ToString("X"));
        }
    }

    private static void ShowSpanProperty(ulong addr, IList<string> path, PropertyInfo p, object obj)
    {
        var objType = obj.GetType();
        var propType = p.PropertyType;
        if (p.GetGetMethod() is not { } getMethod)
        {
            ImGui.Text("(No getter available)");
            return;
        }

        var dm = new DynamicMethod(
            "-",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            null, 
            new[] { typeof(object), typeof(IList<string>), typeof(ulong) },
            obj.GetType(),
            true);

        var ilg = dm.GetILGenerator();
        var objLocalIndex = unchecked((byte)ilg.DeclareLocal(objType, true).LocalIndex);
        var propLocalIndex = unchecked((byte)ilg.DeclareLocal(propType, true).LocalIndex);
        ilg.Emit(OpCodes.Ldarg_0);
        if (objType.IsValueType)
        {
            ilg.Emit(OpCodes.Unbox_Any, objType);
            ilg.Emit(OpCodes.Stloc_S, objLocalIndex);
            ilg.Emit(OpCodes.Ldloca_S, objLocalIndex);
        }

        ilg.Emit(OpCodes.Call, getMethod);
        var mm = typeof(Util).GetMethod(nameof(ShowSpanPrivate), BindingFlags.Static | BindingFlags.NonPublic)!
                             .MakeGenericMethod(p.PropertyType.GetGenericArguments());
        ilg.Emit(OpCodes.Stloc_S, propLocalIndex);
        ilg.Emit(OpCodes.Ldarg_2); // addr = arg2
        ilg.Emit(OpCodes.Ldarg_1); // path = arg1
        ilg.Emit(OpCodes.Ldc_I4_0); // offset = 0
        ilg.Emit(OpCodes.Ldc_I4_1); // isTop = true
        ilg.Emit(OpCodes.Ldloca_S, propLocalIndex); // spanobj
        ilg.Emit(OpCodes.Call, mm);
        ilg.Emit(OpCodes.Ret);

        dm.Invoke(null, new[] { obj, path, addr });
    }

    private static unsafe void ShowSpanPrivate<T>(ulong addr, IList<string> path, int offset, bool isTop, in Span<T> spanobj)
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        if (isTop)
        {
            fixed (void* p = spanobj)
            {
                if (!ImGui.TreeNode(
                    $"Span<{typeof(T).Name}> of length {spanobj.Length:n0} (0x{spanobj.Length:X})" +
                    $"##print-obj-{addr:X}-{string.Join("-", path)}-head"))
                {
                    return;
                }
            }
        }

        try
        {
            const int batchSize = 20;
            if (spanobj.Length > batchSize)
            {
                var skip = batchSize;
                while ((spanobj.Length + skip - 1) / skip > batchSize)
                    skip *= batchSize;
                for (var i = 0; i < spanobj.Length; i += skip)
                {
                    var next = Math.Min(i + skip, spanobj.Length);
                    path.Add($"{offset + i:X}_{skip}");
                    if (ImGui.TreeNode(
                        $"{offset + i:n0} ~ {offset + next - 1:n0} (0x{offset + i:X} ~ 0x{offset + next - 1:X})" +
                        $"##print-obj-{addr:X}-{string.Join("-", path)}"))
                    {
                        try
                        {
                            ShowSpanPrivate(addr, path, offset + i, false, spanobj[i..next]);
                        }
                        finally
                        {
                            ImGui.TreePop();
                        }
                    }

                    path.RemoveAt(path.Count - 1);
                }
            }
            else
            {
                fixed (T* p = spanobj)
                {
                    var pointerType = typeof(T*);
                    for (var i = 0; i < spanobj.Length; i++)
                    {
                        ImGui.TextUnformatted($"[{offset + i:n0} (0x{offset + i:X})] ");
                        ImGui.SameLine();
                        path.Add($"{offset + i}");
                        ShowValue(addr, path, pointerType, Pointer.Box(p + i, pointerType), true);
                    }
                }
            }
        }
        finally
        {
            if (isTop)
                ImGui.TreePop();
        }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    }

    private static unsafe void ShowValue(ulong addr, IList<string> path, Type type, object value, bool hideAddress)
    {
        if (type.IsPointer)
        {
            var val = (Pointer)value;
            var unboxed = Pointer.Unbox(val);
            if (unboxed != null)
            {
                if (!hideAddress)
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

                    ImGui.SameLine();
                }

                try
                {
                    var eType = type.GetElementType();
                    var ptrObj = SafeMemory.PtrToStructure(new IntPtr(unboxed), eType);
                    if (ptrObj == null)
                    {
                        ImGui.Text("null or invalid");
                    }
                    else
                    {
                        ShowStruct(ptrObj, addr, path: path, hideAddress: hideAddress);
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
                ShowStruct(value, addr, path: path, hideAddress: hideAddress);
            }
            else
            {
                ImGui.Text($"{value}");
            }
        }
    }
}
