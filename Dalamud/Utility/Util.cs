using System.Collections.Generic;
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
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Support;
using Lumina.Excel.Sheets;
using Serilog;
using TerraFX.Interop.Windows;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.WindowsAndMessaging;

using static TerraFX.Interop.Windows.Windows;

using HWND = Windows.Win32.Foundation.HWND;
using Win32_PInvoke = Windows.Win32.PInvoke;

namespace Dalamud.Utility;

/// <summary>
/// Class providing various helper methods for use in Dalamud and plugins.
/// </summary>
public static class Util
{
    private static readonly string[] PageProtectionFlagNames = [
        "PAGE_NOACCESS",
        "PAGE_READONLY",
        "PAGE_READWRITE",
        "PAGE_WRITECOPY",
        "PAGE_EXECUTE",
        "PAGE_EXECUTE_READ",
        "PAGE_EXECUTE_READWRITE",
        "PAGE_EXECUTE_WRITECOPY",
        "PAGE_GUARD",
        "PAGE_NOCACHE",
        "PAGE_WRITECOMBINE",
        "PAGE_GRAPHICS_NOACCESS",
        "PAGE_GRAPHICS_READONLY",
        "PAGE_GRAPHICS_READWRITE",
        "PAGE_GRAPHICS_EXECUTE",
        "PAGE_GRAPHICS_EXECUTE_READ",
        "PAGE_GRAPHICS_EXECUTE_READWRITE",
        "PAGE_GRAPHICS_COHERENT",
        "PAGE_GRAPHICS_NOCACHE",
    ];

    private static readonly Type GenericSpanType = typeof(Span<>);
    private static string? scmVersionInternal;
    private static string? gitHashInternal;
    private static string? gitHashClientStructsInternal;

    private static ulong moduleStartAddr;
    private static ulong moduleEndAddr;

    /// <summary>
    /// Gets the assembly version of Dalamud.
    /// </summary>
    public static string AssemblyVersion { get; } =
        Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();

    /// <summary>
    /// Gets the SCM Version from the assembly, or null if it cannot be found. This method will generally return
    /// the <c>git describe</c> output for this build, which will be a raw version if this is a stable build or an
    /// appropriately-annotated version if this is *not* stable. Local builds will return a `Local Build` text string.
    /// </summary>
    /// <returns>The SCM version of the assembly.</returns>
    public static string GetScmVersion()
    {
        if (scmVersionInternal != null) return scmVersionInternal;

        var asm = typeof(Util).Assembly;
        var attrs = asm.GetCustomAttributes<AssemblyMetadataAttribute>();

        return scmVersionInternal = attrs.First(a => a.Key == "SCMVersion").Value
                                        ?? asm.GetName().Version!.ToString();
    }

    /// <summary>
    /// Gets the git commit hash value from the assembly or null if it cannot be found. Will be null for Debug builds,
    /// and will be suffixed with `-dirty` if in release with pending changes.
    /// </summary>
    /// <returns>The git hash of the assembly.</returns>
    public static string? GetGitHash()
    {
        if (gitHashInternal != null)
            return gitHashInternal;

        var asm = typeof(Util).Assembly;
        var attrs = asm.GetCustomAttributes<AssemblyMetadataAttribute>();

        return gitHashInternal = attrs.FirstOrDefault(a => a.Key == "GitHash")?.Value ?? "N/A";
    }

    /// <summary>
    /// Gets the git hash value from the assembly or null if it cannot be found.
    /// </summary>
    /// <returns>The git hash of the assembly.</returns>
    public static string? GetGitHashClientStructs()
    {
        if (gitHashClientStructsInternal != null)
            return gitHashClientStructsInternal;

        var asm = typeof(Util).Assembly;
        var attrs = asm.GetCustomAttributes<AssemblyMetadataAttribute>();

        gitHashClientStructsInternal = attrs.First(a => a.Key == "GitHashClientStructs").Value;

        return gitHashClientStructsInternal;
    }

    /// <inheritdoc cref="DescribeAddress(nint)"/>
    public static unsafe string DescribeAddress(void* p) => DescribeAddress((nint)p);

    /// <summary>Describes a memory address relative to module, or allocation base.</summary>
    /// <param name="p">Address.</param>
    /// <returns>Address description.</returns>
    public static unsafe string DescribeAddress(nint p)
    {
        Span<char> namebuf = stackalloc char[9];
        var modules = CurrentProcessModules.ModuleCollection;
        for (var i = 0; i < modules.Count; i++)
        {
            if (p < modules[i].BaseAddress) continue;
            var d = p - modules[i].BaseAddress;
            if (d > modules[i].ModuleMemorySize) continue;

            // Display module name without path, only if there exists exactly one module loaded in the memory.
            var fileName = modules[i].ModuleName;
            for (var j = 0; j < modules.Count; j++)
            {
                if (i == j)
                    continue;
                if (!modules[j].ModuleName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                fileName = modules[i].FileName;
                break;
            }

            var dos = (IMAGE_DOS_HEADER*)modules[i].BaseAddress;
            if (dos->e_magic != 0x5A4D)
                return $"0x{p:X}({fileName}+0x{d:X}: ???)";

            Span<IMAGE_SECTION_HEADER> sections;
            switch (((IMAGE_NT_HEADERS32*)(modules[i].BaseAddress + dos->e_lfanew))->OptionalHeader.Magic)
            {
                case IMAGE.IMAGE_NT_OPTIONAL_HDR32_MAGIC:
                {
                    var nth = (IMAGE_NT_HEADERS32*)(modules[i].BaseAddress + dos->e_lfanew);
                    if (d < dos->e_lfanew + sizeof(IMAGE_NT_HEADERS32)
                        + (nth->FileHeader.NumberOfSections * sizeof(IMAGE_SECTION_HEADER)))
                        goto default;
                    sections = new(
                        (void*)(modules[i].BaseAddress + dos->e_lfanew + sizeof(IMAGE_NT_HEADERS32)),
                        nth->FileHeader.NumberOfSections);
                    break;
                }

                case IMAGE.IMAGE_NT_OPTIONAL_HDR64_MAGIC:
                {
                    var nth = (IMAGE_NT_HEADERS64*)(modules[i].BaseAddress + dos->e_lfanew);
                    if (d < dos->e_lfanew + sizeof(IMAGE_NT_HEADERS64)
                        + (nth->FileHeader.NumberOfSections * sizeof(IMAGE_SECTION_HEADER)))
                        goto default;
                    sections = new(
                        (void*)(modules[i].BaseAddress + dos->e_lfanew + sizeof(IMAGE_NT_HEADERS64)),
                        nth->FileHeader.NumberOfSections);
                    break;
                }

                default:
                    return $"0x{p:X}({fileName}+0x{d:X}: header)";
            }

            for (var j = 0; j < sections.Length; j++)
            {
                if (d >= sections[j].VirtualAddress && d < sections[j].VirtualAddress + sections[j].Misc.VirtualSize)
                {
                    var d2 = d - sections[j].VirtualAddress;
                    var name8 = new Span<byte>((byte*)Unsafe.AsPointer(ref sections[j].Name[0]), 8).TrimEnd((byte)0);
                    return $"0x{p:X}({fileName}+0x{d:X}({namebuf[..Encoding.UTF8.GetChars(name8, namebuf)]}+0x{d2:X}))";
                }
            }

            return $"0x{p:X}({fileName}+0x{d:X}: ???)";
        }

        MEMORY_BASIC_INFORMATION mbi;
        if (VirtualQuery((void*)p, &mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) == 0)
            return $"0x{p:X}(???)";

        var sb = new StringBuilder();
        sb.Append($"0x{p:X}(");
        for (int i = 0, c = 0; i < PageProtectionFlagNames.Length; i++)
        {
            if ((mbi.Protect & (1 << i)) == 0)
                continue;
            if (c++ != 0)
                sb.Append(" | ");
            sb.Append(PageProtectionFlagNames[i]);
        }

        return sb.Append(')').ToString();
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
        => ShowStructInternal(obj, addr, autoExpand, path);

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
    public static unsafe void ShowGameObjectStruct(IGameObject go, bool autoExpand = true)
    {
        switch (go)
        {
            case BattleChara bchara:
                ShowStruct(bchara.Struct, autoExpand);
                break;
            case Character chara:
                ShowStruct(chara.Struct, autoExpand);
                break;
            case GameObject gameObject:
                ShowStruct(gameObject.Struct, autoExpand);
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
        var flags = MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR |
                    MESSAGEBOX_STYLE.MB_TOPMOST;
        _ = Windows.Win32.PInvoke.MessageBox(new HWND(Process.GetCurrentProcess().MainWindowHandle), message, caption, flags);

        if (exit)
        {
            Log.CloseAndFlush();
            Environment.Exit(-1);
        }
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
    /// Determine if Dalamud is currently running within a Wine context (e.g. either on macOS or Linux). This method
    /// will not return information about the host operating system.
    /// </summary>
    /// <returns>Returns true if running on Wine, false otherwise.</returns>
    public static bool IsWine() => Service<Dalamud>.Get().StartInfo.Platform != OSPlatform.Windows;

    /// <summary>
    /// Gets the current host's platform based on the injector launch arguments or heuristics.
    /// </summary>
    /// <returns>Returns the <see cref="OSPlatform"/> that Dalamud is currently running on.</returns>
    public static OSPlatform GetHostPlatform() => Service<Dalamud>.Get().StartInfo.Platform;

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
    [Api13ToDo("Remove.")]
    [Obsolete("Replaced with FilesystemUtil.WriteAllTextSafe()")]
    public static void WriteAllTextSafe(string path, string text) => FilesystemUtil.WriteAllTextSafe(path, text);

    /// <summary>
    /// Overwrite text in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="encoding">Encoding to use.</param>
    [Api13ToDo("Remove.")]
    [Obsolete("Replaced with FilesystemUtil.WriteAllTextSafe()")]
    public static void WriteAllTextSafe(string path, string text, Encoding encoding) => FilesystemUtil.WriteAllTextSafe(path, text, encoding);

    /// <summary>
    /// Overwrite data in a file by first writing it to a temporary file, and then
    /// moving that file to the path specified.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="bytes">The data to write.</param>
    [Api13ToDo("Remove.")]
    [Obsolete("Replaced with FilesystemUtil.WriteAllBytesSafe()")]
    public static void WriteAllBytesSafe(string path, byte[] bytes) => FilesystemUtil.WriteAllBytesSafe(path, bytes);

    /// <summary>Gets a temporary file name, for use as the sourceFileName in
    /// <see cref="File.Replace(string,string,string?)"/>.</summary>
    /// <param name="targetFile">The target file.</param>
    /// <returns>A temporary file name that should be usable with <see cref="File.Replace(string,string,string?)"/>.
    /// </returns>
    /// <remarks>No write operation is done on the filesystem.</remarks>
    public static string GetReplaceableFileName(string targetFile)
    {
        Span<byte> buf = stackalloc byte[9];
        Random.Shared.NextBytes(buf);
        for (var i = 0; ; i++)
        {
            var tempName =
                Path.GetFileName(targetFile) +
                Convert.ToBase64String(buf)
                       .TrimEnd('=')
                       .Replace('+', '-')
                       .Replace('/', '_');
            var tempPath = Path.Join(Path.GetDirectoryName(targetFile), tempName);
            if (i >= 64 || !Path.Exists(tempPath))
                return tempPath;
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

        return names.GetRowAt(rng.Next(0, names.Count - 1)).Singular.ExtractText();
    }

    /// <summary>
    /// Throws a corresponding exception if <see cref="HRESULT.FAILED"/> is true.
    /// </summary>
    /// <param name="hr">The result value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowOnError(this HRESULT hr)
    {
        if (hr.FAILED)
            Marshal.ThrowExceptionForHR(hr.Value);
    }

    /// <summary>Determines if the specified instance of <see cref="ComPtr{T}"/> points to null.</summary>
    /// <param name="f">The pointer.</param>
    /// <typeparam name="T">The COM interface type from TerraFX.</typeparam>
    /// <returns><c>true</c> if not empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe bool IsEmpty<T>(in this ComPtr<T> f) where T : unmanaged, IUnknown.Interface =>
        f.Get() is null;

    /// <summary>
    /// Calls <see cref="TaskCompletionSource.SetException(System.Exception)"/> if the task is incomplete.
    /// </summary>
    /// <param name="t">The task.</param>
    /// <param name="ex">The exception to set.</param>
    internal static void SetExceptionIfIncomplete(this TaskCompletionSource t, Exception ex)
    {
        if (t.Task.IsCompleted)
            return;
        try
        {
            t.SetException(ex);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Calls <see cref="TaskCompletionSource.SetException(System.Exception)"/> if the task is incomplete.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="t">The task.</param>
    /// <param name="ex">The exception to set.</param>
    internal static void SetExceptionIfIncomplete<T>(this TaskCompletionSource<T> t, Exception ex)
    {
        if (t.Task.IsCompleted)
            return;
        try
        {
            t.SetException(ex);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Print formatted IGameObject Information to ImGui.
    /// </summary>
    /// <param name="actor">IGameObject to Display.</param>
    /// <param name="tag">Display Tag.</param>
    /// <param name="resolveGameData">If the IGameObjects data should be resolved.</param>
    internal static void PrintGameObject(IGameObject actor, string tag, bool resolveGameData)
    {
        var actorString =
            $"{actor.Address.ToInt64():X}:{actor.GameObjectId:X}[{tag}] - {actor.ObjectKind} - {actor.Name} - X{actor.Position.X} Y{actor.Position.Y} Z{actor.Position.Z} D{actor.YalmDistanceX} R{actor.Rotation} - Target: {actor.TargetObjectId:X}\n";

        if (actor is Npc npc)
            actorString += $"       DataId: {npc.DataId}  NameId:{npc.NameId}\n";

        if (actor is ICharacter chara)
        {
            actorString +=
                $"       Level: {chara.Level} ClassJob: {(resolveGameData ? chara.ClassJob.ValueNullable?.Name : chara.ClassJob.RowId.ToString())} CHP: {chara.CurrentHp} MHP: {chara.MaxHp} CMP: {chara.CurrentMp} MMP: {chara.MaxMp}\n       Customize: {BitConverter.ToString(chara.Customize).Replace("-", " ")} StatusFlags: {chara.StatusFlags}\n";
        }

        if (actor is IPlayerCharacter pc)
        {
            actorString +=
                $"       HomeWorld: {(resolveGameData ? pc.HomeWorld.ValueNullable?.Name : pc.HomeWorld.RowId.ToString())} CurrentWorld: {(resolveGameData ? pc.CurrentWorld.ValueNullable?.Name : pc.CurrentWorld.RowId.ToString())} FC: {pc.CompanyTag}\n";
        }

        ImGui.TextUnformatted(actorString);
        ImGui.SameLine();
        if (ImGui.Button($"C##{actor.Address.ToInt64()}"))
        {
            ImGui.SetClipboardText(actor.Address.ToInt64().ToString("X"));
        }
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
            $"{actor.Address.ToInt64():X}:{actor.GameObjectId:X}[{tag}] - {actor.ObjectKind} - {actor.Name} - X{actor.Position.X} Y{actor.Position.Y} Z{actor.Position.Z} D{actor.YalmDistanceX} R{actor.Rotation} - Target: {actor.TargetObjectId:X}\n";

        if (actor is Npc npc)
            actorString += $"       DataId: {npc.DataId}  NameId:{npc.NameId}\n";

        if (actor is Character chara)
        {
            actorString +=
                $"       Level: {chara.Level} ClassJob: {(resolveGameData ? chara.ClassJob.ValueNullable?.Name : chara.ClassJob.RowId.ToString())} CHP: {chara.CurrentHp} MHP: {chara.MaxHp} CMP: {chara.CurrentMp} MMP: {chara.MaxMp}\n       Customize: {BitConverter.ToString(chara.Customize).Replace("-", " ")} StatusFlags: {chara.StatusFlags}\n";
        }

        if (actor is PlayerCharacter pc)
        {
            actorString +=
                $"       HomeWorld: {(resolveGameData ? pc.HomeWorld.ValueNullable?.Name : pc.HomeWorld.RowId.ToString())} CurrentWorld: {(resolveGameData ? pc.CurrentWorld.ValueNullable?.Name : pc.CurrentWorld.RowId.ToString())} FC: {pc.CompanyTag}\n";
        }

        ImGui.TextUnformatted(actorString);
        ImGui.SameLine();
        if (ImGui.Button($"C##{actor.Address.ToInt64()}"))
        {
            ImGui.SetClipboardText(actor.Address.ToInt64().ToString("X"));
        }
    }

    /// <summary>
    /// Copy files to the clipboard as if they were copied in Explorer.
    /// </summary>
    /// <param name="paths">Full paths to files to be copied.</param>
    /// <returns>Returns true on success.</returns>
    internal static unsafe bool CopyFilesToClipboard(IEnumerable<string> paths)
    {
        var pathBytes = paths
                        .Select(Encoding.Unicode.GetBytes)
                        .ToArray();
        var pathBytesSize = pathBytes
                            .Select(bytes => bytes.Length)
                            .Sum();
        var sizeWithTerminators = pathBytesSize + (pathBytes.Length * 2);

        var dropFilesSize = sizeof(DROPFILES);
        var hGlobal = Win32_PInvoke.GlobalAlloc_SafeHandle(
            GLOBAL_ALLOC_FLAGS.GHND,
            // struct size + size of encoded strings + null terminator for each
            // string + two null terminators for end of list
            (uint)(dropFilesSize + sizeWithTerminators + 4));
        var dropFiles = (DROPFILES*)Win32_PInvoke.GlobalLock(hGlobal);

        *dropFiles = default;
        dropFiles->fWide = true;
        dropFiles->pFiles = (uint)dropFilesSize;

        var pathLoc = (byte*)((nint)dropFiles + dropFilesSize);
        foreach (var bytes in pathBytes)
        {
            // copy the encoded strings
            for (var i = 0; i < bytes.Length; i++)
            {
                pathLoc![i] = bytes[i];
            }

            // null terminate
            pathLoc![bytes.Length] = 0;
            pathLoc[bytes.Length + 1] = 0;
            pathLoc += bytes.Length + 2;
        }

        // double null terminator for end of list
        for (var i = 0; i < 4; i++)
        {
            pathLoc![i] = 0;
        }

        Win32_PInvoke.GlobalUnlock(hGlobal);

        if (Win32_PInvoke.OpenClipboard(default))
        {
            Win32_PInvoke.SetClipboardData(
                (uint)CLIPBOARD_FORMAT.CF_HDROP,
                hGlobal);
            Win32_PInvoke.CloseClipboard();
            return true;
        }

        hGlobal.Dispose();
        return false;
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

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

    private static unsafe void ShowSpanPrivate<T>(ulong addr, IList<string> path, int offset, bool isTop, in Span<T> spanobj)
    {
        if (isTop)
        {
            fixed (void* p = spanobj)
            {
                using var tree = ImRaii.TreeNode($"Span<{typeof(T).Name}> of length {spanobj.Length:n0} (0x{spanobj.Length:X})" + $"##print-obj-{addr:X}-{string.Join("-", path)}-head", ImGuiTreeNodeFlags.SpanFullWidth);
                if (tree.Success)
                {
                    ShowSpanEntryPrivate(addr, path, offset, spanobj);
                }
            }
        }
        else
        {
            ShowSpanEntryPrivate(addr, path, offset, spanobj);
        }
    }

    private static unsafe void ShowSpanEntryPrivate<T>(ulong addr, IList<string> path, int offset, Span<T> spanobj)
    {
        const int batchSize = 20;
        if (spanobj.Length > batchSize)
        {
            var skip = batchSize;
            while ((spanobj.Length + skip - 1) / skip > batchSize)
            {
                skip *= batchSize;
            }

            for (var i = 0; i < spanobj.Length; i += skip)
            {
                var next = Math.Min(i + skip, spanobj.Length);
                path.Add($"{offset + i:X}_{skip}");

                using (var tree = ImRaii.TreeNode($"{offset + i:n0} ~ {offset + next - 1:n0} (0x{offset + i:X} ~ 0x{offset + next - 1:X})" + $"##print-obj-{addr:X}-{string.Join("-", path)}", ImGuiTreeNodeFlags.SpanFullWidth))
                {
                    if (tree.Success)
                    {
                        ShowSpanEntryPrivate(addr, path, offset + i, spanobj[i..next]);
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

#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

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
                        using (ImRaii.PushColor(ImGuiCol.Text, 0xffcbc0ff))
                        {
                            ImGuiHelpers.ClickToCopyText($"ffxiv_dx11.exe+{unboxedAddr - moduleStartAddr:X}");
                        }
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
                        ShowStructInternal(ptrObj, addr, path: path, hideAddress: hideAddress);
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
                ShowStructInternal(value, addr, path: path, hideAddress: hideAddress);
            }
            else
            {
                ImGui.Text($"{value}");
            }
        }
    }

    /// <summary>
    /// Show a structure in an ImGui context.
    /// </summary>
    /// <param name="obj">The structure to show.</param>
    /// <param name="addr">The address to the structure.</param>
    /// <param name="autoExpand">Whether or not this structure should start out expanded.</param>
    /// <param name="path">The already followed path.</param>
    /// <param name="hideAddress">Do not print addresses. Use when displaying a copied value.</param>
    private static void ShowStructInternal(object obj, ulong addr, bool autoExpand = false, IEnumerable<string>? path = null, bool hideAddress = false)
    {
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(3, 2)))
        {
            path ??= new List<string>();
            var pathList = path as List<string> ?? path.ToList();

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

            if (autoExpand)
            {
                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
            }

            using var col = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
            using var tree = ImRaii.TreeNode($"{obj}##print-obj-{addr:X}-{string.Join("-", pathList)}", ImGuiTreeNodeFlags.SpanFullWidth);
            col.Pop();

            if (tree.Success)
            {
                foreach (var f in obj.GetType()
                                     .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance))
                {
                    var fixedBuffer = (FixedBufferAttribute)f.GetCustomAttribute(typeof(FixedBufferAttribute));
                    var offset = (FieldOffsetAttribute)f.GetCustomAttribute(typeof(FieldOffsetAttribute));

                    if (fixedBuffer != null)
                    {
                        ImGui.Text("fixed");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{fixedBuffer.ElementType.Name}[0x{fixedBuffer.Length:X}]");
                    }
                    else
                    {
                        if (offset != null)
                        {
                            ImGui.TextDisabled($"[0x{offset.Value:X}]");
                            ImGui.SameLine();
                        }

                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{f.FieldType.Name}");
                    }

                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.4f, 1), $"{f.Name}: ");
                    ImGui.SameLine();

                    pathList.Add(f.Name);
                    try
                    {
                        if (f.FieldType.IsGenericType && (f.FieldType.IsByRef || f.FieldType.IsByRefLike))
                        {
                            ImGui.Text("Cannot preview ref typed fields."); // object never contains ref struct
                        }
                        else if (f.FieldType == typeof(bool) && offset != null)
                        {
                            ShowValue(addr, pathList, f.FieldType, Marshal.ReadByte((nint)addr + offset.Value) > 0, hideAddress);
                        }
                        else
                        {
                            ShowValue(addr, pathList, f.FieldType, f.GetValue(obj), hideAddress);
                        }
                    }
                    catch (Exception ex)
                    {
                        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0.4f, 0.4f, 1f)))
                        {
                            ImGui.TextUnformatted($"Error: {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                    finally
                    {
                        pathList.RemoveAt(pathList.Count - 1);
                    }
                }

                foreach (var p in obj.GetType().GetProperties().Where(static p => p.GetGetMethod()?.GetParameters().Length == 0))
                {
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{p.PropertyType.Name}");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1), $"{p.Name}: ");
                    ImGui.SameLine();

                    pathList.Add(p.Name);
                    try
                    {
                        if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == GenericSpanType)
                        {
                            ShowSpanProperty(addr, pathList, p, obj);
                        }
                        else if (p.PropertyType.IsGenericType && (p.PropertyType.IsByRef || p.PropertyType.IsByRefLike))
                        {
                            ImGui.Text("Cannot preview ref typed properties.");
                        }
                        else
                        {
                            ShowValue(addr, pathList, p.PropertyType, p.GetValue(obj), hideAddress);
                        }
                    }
                    catch (Exception ex)
                    {
                        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0.4f, 0.4f, 1f)))
                        {
                            ImGui.TextUnformatted($"Error: {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                    finally
                    {
                        pathList.RemoveAt(pathList.Count - 1);
                    }
                }
            }
        }
    }
}
