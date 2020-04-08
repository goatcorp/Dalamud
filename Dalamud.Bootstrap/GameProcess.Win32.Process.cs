using Dalamud.Bootstrap.OS.Windows.Raw;
using System;
using System.Text;

namespace Dalamud.Bootstrap
{
    public sealed partial class GameProcess : IDisposable
    {
        /// <summary>
        /// Reads command-line arguments from the process.
        /// </summary>
        public string[] GetProcessArguments()
        {
            PEB peb = default;
            RTL_USER_PROCESS_PARAMETERS procParam = default;

            // Find where the command line is allocated
            var pebAddr = GetPebAddress();
            ReadMemoryExact(pebAddr, ref peb);
            ReadMemoryExact(peb.ProcessParameters, ref procParam);

            // Read the command line (which is utf16-like string)
            var commandLine = ReadMemoryExact(procParam.CommandLine.Buffer, procParam.CommandLine.Length);

            return ParseCommandLineToArguments(commandLine);
        }

        /// <summary>
        /// Returns a time when the process was started.
        /// </summary>
        private DateTime GetCreationTime()
        {
            if (!Kernel32.GetProcessTimes(m_handle, out var creationTime, out var _, out var _, out var _))
            {
                ProcessException.ThrowLastOsError();
            }

            return creationTime.ToDateTime();
        }

        private string[] ParseCommandLineToArguments(ReadOnlySpan<byte> commandLine)
        {
            unsafe
            {
                char** argv;
                int argc;

                fixed (byte* pCommandLine = commandLine)
                {
                    argv = Shell32.CommandLineToArgvW(pCommandLine, out argc);
                }

                if (argv == null)
                {
                    ProcessException.ThrowLastOsError();
                }

                // NOTE: argv must be deallocated via LocalFree when we're done
                try
                {
                    var arguments = new string[argc];

                    for (var i = 0; i < argc; i++)
                    {
                        arguments[i] = new string(argv[i]);
                    }

                    return arguments;
                }
                finally
                {
                    Kernel32.LocalFree(argv);
                }
            }
        }

        public string GetImageFilePath()
        {
            var buffer = new StringBuilder(300);

            // From docs:
            // On input, specifies the size of the lpExeName buffer, in characters.
            // On success, receives the number of characters written to the buffer, not including the null-terminating character.
            var size = buffer.Capacity;

            if (!Kernel32.QueryFullProcessImageNameW(m_handle, 0, buffer, ref size))
            {
                ProcessException.ThrowLastOsError();
            }

            return buffer.ToString();
        }
    }
}
