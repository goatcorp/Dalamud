using System.ComponentModel;
using System.Data;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Dalamud.Broker.Win32;

internal static class ProcessLauncher
{
    public static ProcessHandle Start(ProcessLaunchContext context)
    {
        const PROCESS_CREATION_FLAGS requiredCreationFlags = PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT;

        unsafe
        {
            BOOL ok;
            STARTUPINFOEXW startupInfo;
            PROCESS_INFORMATION processInfo;

            var commandLine = CreateCommandLine(context.ApplicationPath, context.Arguments);

            // This is because commandLine technically requires to be "mutable".
            var mutableCommandLine = new char[commandLine.Length + 1 /* NIL */];
            commandLine.CopyTo(mutableCommandLine);

            startupInfo.StartupInfo.cb = (uint)sizeof(STARTUPINFOEXW);

            // NOTE:
            // pWorkingDirectory will be nullptr iff context.WorkingDirectory is also null.
            // [ref]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-7.3/pattern-based-fixed#detailed-design
            fixed (char* pAppPath = context.ApplicationPath)
            fixed (char* pCommandLine = mutableCommandLine)
            fixed (char* pWorkingDirectory = context.WorkingDirectory)
            {
                ok = PInvoke.CreateProcess(
                    pAppPath,
                    pCommandLine,
                    null,
                    null,
                    false,
                    context.CreationFlags | requiredCreationFlags,
                    null,
                    pWorkingDirectory,
                    (STARTUPINFOW*)&startupInfo,
                    &processInfo
                );
            }

            if (!ok)
            {
                throw new Win32Exception();
            }

            return new ProcessHandle
            {
                Process = new SafeProcessHandle(processInfo.hProcess, true),
                Thread = new SafeProcessHandle(processInfo.hThread, true),
            };
        }
    }

    private static string CreateCommandLine(string appPath, IEnumerable<string>? arguments)
    {
        var commandLine = new StringBuilder(200);

        // Just put appPath as it is (A double quote character (") is not allowed to be in the file path anyway.) 
        commandLine.Append($@"""{appPath}""");

        if (arguments != null)
        {
            foreach (var argument in arguments)
            {
                commandLine.Append(' ');
                AddCommandLineArgument(commandLine, argument);
            }
        }

        return commandLine.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    private static void AddCommandLineArgument(StringBuilder commandLine, string argument)
    {
        // gist of this function is:
        // 1. n backslashes **not** followed by a double quote (e.g. \, \\, \\\, ...) will produce n backslashes
        //    literally. (e.g. \, \\, \\\, ...respectively)
        // 2. 2n backslashes followed by a double quote (e.g. \\", \\\\", \\\\\\", ...) will produce n backslashes
        //    (same as above) and will toggle quoted mode.
        // 3. (2n)+1 backslashes followed by a double quote (e.g. \", \\\", \\\\\" ...) will produce n backslashes
        //    (again, same as above) and will not toggle quoted mode.
        //
        // ...but I really want to take this opportunity to say that commandline encoding in Windows is fucking bonkers.

        //
        if (argument.Contains('\x00'))
        {
            throw new ArgumentException("Process argument can't contain a null character.", nameof(argument));
        }

        // Begin "quoted mode" - we put everything in this mode to favor easy-to-understand implementation. 
        commandLine.Append('"');

        var backslashes = 0;
        foreach (var chr in argument)
        {
            if (chr is '\\')
            {
                backslashes += 1;
            }
            else if (chr is '"')
            {
                // Put n+1 more backslashes (2n+1 total) to match rule.2
                commandLine.Append('\\', backslashes + 1);
                backslashes = 0;
            }

            commandLine.Append(chr);
        }

        // Put n more backslashes (2n total) to trigger rule.2; This ends "quoted mode"
        commandLine.Append('\\', backslashes);
        commandLine.Append('"');
    }
}
