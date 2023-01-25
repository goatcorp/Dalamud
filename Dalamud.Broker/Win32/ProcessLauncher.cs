using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Dalamud.Broker.Win32;

internal static class ProcessLauncher
{
    public static ProcessHandle Start(string path)
    {
        var context = new ProcessLaunchContext()
        {
            ApplicationPath = path
        };
        return Start(context);
    }

    public static ProcessHandle Start(ProcessLaunchContext context)
    {
        const PROCESS_CREATION_FLAGS requiredCreationFlags = PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT;

        unsafe
        {
            BOOL ok;
            STARTUPINFOEXW startupInfo;
            PROCESS_INFORMATION processInfo;
            SECURITY_CAPABILITIES sc = default;
            LPPROC_THREAD_ATTRIBUTE_LIST procThreadAttrList = default;
            SidAndAttributeList? capabilities = default;
            nuint procThreadAttrListSize = default;

            try
            {
                var commandLine = CreateCommandLine(context.ApplicationPath, context.Arguments);

                // This is because commandLine technically requires to be "mutable".
                var mutableCommandLine = new char[commandLine.Length + 1 /* NIL */];
                commandLine.CopyTo(mutableCommandLine);

                // Initialize the container if exists.
                if (context.AppContainer is not null)
                {
                    // Initialize the capabilities list. If `context.Capabilities` is `null`, then `pCapabilities` will
                    // also be `null`.
                    //
                    // TODO:
                    // While cap is almost always non-null value because we want to have at least `CAP_INET`,
                    // is it `container != null && cap == null` actually safe? 
                    if (context.Capabilities is not null)
                    {
                        capabilities = new SidAndAttributeList(context.Capabilities);
                    }

                    // Initialize SecurityCapabilities. This is where we actually set the container sid and its capabilities.
                    sc.AppContainerSid = context.AppContainer.Psid;
                    sc.CapabilityCount = (uint)(context.Capabilities?.Length ?? 0);
                    sc.Capabilities = capabilities switch
                    {
                        null => null,
                        _ => capabilities.AsPointer(),
                    };

                    // Initialize procThreadAttrList. First call is to get the required size and the second call is what
                    // actually initializes it.
                    // 
                    // At this time we only use procThreadAttr to give the process capabilities. (PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES)
                    PInvoke.InitializeProcThreadAttributeList(
                        (LPPROC_THREAD_ATTRIBUTE_LIST)null,
                        1,
                        0,
                        ref procThreadAttrListSize
                    );
                    procThreadAttrList =
                        (LPPROC_THREAD_ATTRIBUTE_LIST)Marshal.AllocCoTaskMem((int)procThreadAttrListSize).ToPointer();
                    ok = PInvoke.InitializeProcThreadAttributeList(
                        procThreadAttrList,
                        1,
                        0,
                        ref procThreadAttrListSize
                    );
                    if (!ok)
                    {
                        throw new Win32Exception();
                    }

                    // Attach attributes to the process.
                    ok = PInvoke.UpdateProcThreadAttribute(
                        procThreadAttrList,
                        0,
                        PInvoke.PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES,
                        &sc,
                        (nuint)Marshal.SizeOf(sc)
                    );
                    if (!ok)
                    {
                        throw new Win32Exception();
                    }
                }

                // Initialize process startupInfo
                startupInfo.StartupInfo.cb = (uint)sizeof(STARTUPINFOEXW); // required
                startupInfo.lpAttributeList = procThreadAttrList;

                // Actually launch the process.
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
                    if (!ok)
                    {
                        throw new Win32Exception();
                    }
                }

                return new ProcessHandle
                {
                    Process = new SafeProcessHandle(processInfo.hProcess, true),
                    Thread = new SafeProcessHandle(processInfo.hThread, true),
                };
            } finally
            {
                if (procThreadAttrList.Value != null)
                {
                    PInvoke.DeleteProcThreadAttributeList(procThreadAttrList);
                    Marshal.FreeCoTaskMem((nint)procThreadAttrList.Value);
                }
            }
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
