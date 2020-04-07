using System.Runtime.InteropServices;

namespace Dalamud.Bootstrap.OS.Windows.Raw
{
    internal static unsafe class Shell32
    {
        private const string Name = "shell32";

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true)]
        public static extern char** CommandLineToArgvW(void* lpCmdLine, out int pNumArgs);
    }
}
