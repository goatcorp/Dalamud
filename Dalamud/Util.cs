using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game;
using Serilog;

namespace Dalamud {
    internal static class Util {
        public static void DumpMemory(IntPtr offset, int len = 512) {
            var data = new byte[len];
            Marshal.Copy(offset, data, 0, len);
            Log.Information(ByteArrayToHex(data));
        }

        public static string ByteArrayToHex(byte[] bytes, int offset = 0, int bytesPerLine = 16) {
            if (bytes == null) return string.Empty;

            var hexChars = "0123456789ABCDEF".ToCharArray();

            var offsetBlock = 8 + 3;
            var byteBlock = offsetBlock + bytesPerLine * 3 + (bytesPerLine - 1) / 8 + 2;
            var lineLength = byteBlock + bytesPerLine + Environment.NewLine.Length;

            var line = (new string(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            var numLines = (bytes.Length + bytesPerLine - 1) / bytesPerLine;

            var sb = new StringBuilder(numLines * lineLength);

            for (var i = 0; i < bytes.Length; i += bytesPerLine) {
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

                for (var j = 0; j < bytesPerLine; j++) {
                    if (j > 0 && (j & 7) == 0) hexColumn++;

                    if (i + j >= bytes.Length) {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    } else {
                        var by = bytes[i + j];
                        line[hexColumn] = hexChars[(by >> 4) & 0xF];
                        line[hexColumn + 1] = hexChars[by & 0xF];
                        line[charColumn] = by < 32 ? '.' : (char) by;
                    }

                    hexColumn += 3;
                    charColumn++;
                }

                sb.Append(line);
            }

            return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public static string AssemblyVersion { get; } = Assembly.GetAssembly(typeof(ChatHandlers)).GetName().Version.ToString();
    }
}
