using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

using Dalamud.Game;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;
using Serilog;

namespace Dalamud
{
    /// <summary>
    /// Class providing various helper methods for use in Dalamud and plugins.
    /// </summary>
    public static class Util
    {
        private static string gitHashInternal;

        /// <summary>
        /// Gets the assembly version of Dalamud.
        /// </summary>
        public static string AssemblyVersion { get; } = Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();

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

            gitHashInternal = attrs.FirstOrDefault(a => a.Key == "GitHash")?.Value;

            return gitHashInternal;
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
        /// Show all properties and fields of the provided object via ImGui.
        /// </summary>
        /// <param name="obj">The object to show.</param>
        public static void ShowObject(object obj)
        {
            var type = obj.GetType();

            ImGui.Text($"Object Dump({type.Name}) for {obj}({obj.GetHashCode()})");

            ImGuiHelpers.ScaledDummy(5);

            ImGui.TextColored(ImGuiColors.DalamudOrange, "-> Properties:");
            foreach (var propertyInfo in type.GetProperties())
            {
                ImGui.TextColored(ImGuiColors.DalamudOrange, $"    {propertyInfo.Name}: {propertyInfo.GetValue(obj)}");
            }

            ImGuiHelpers.ScaledDummy(5);

            ImGui.TextColored(ImGuiColors.HealerGreen, "-> Fields:");
            foreach (var fieldInfo in type.GetFields())
            {
                ImGui.TextColored(ImGuiColors.HealerGreen, $"    {fieldInfo.Name}: {fieldInfo.GetValue(obj)}");
            }
        }

        /// <summary>
        /// Display an error MessageBox and exit the current process.
        /// </summary>
        /// <param name="message">MessageBox body.</param>
        /// <param name="caption">MessageBox caption (title).</param>
        public static void Fatal(string message, string caption)
        {
            var flags = NativeFunctions.MessageBoxType.Ok | NativeFunctions.MessageBoxType.IconError;
            _ = NativeFunctions.MessageBoxW(Process.GetCurrentProcess().MainWindowHandle, message, caption, flags);

            Environment.Exit(-1);
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

        // TODO: Someone implement GetUTF8String with some IntPtr overloads.
        // while(Marshal.ReadByte(0, sz) != 0) { sz++; }

        /// <summary>
        /// An extension method to chain usage of string.Format.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Format arguments.</param>
        /// <returns>Formatted string.</returns>
        public static string Format(this string format, params object[] args) => string.Format(format, args);
    }
}
