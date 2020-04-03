using System;
using Dalamud.Bootstrap.SqexArg;
using Dalamud.Bootstrap.Windows;
using Microsoft.Win32.SafeHandles;

namespace Dalamud.Bootstrap
{
    internal sealed class GameProcess : IDisposable
    {
        private Process m_process;
        public GameProcess(Process process)
        {
            m_process = process;
        }

        public void Dispose()
        {
            m_process?.Dispose();
            m_process = null!;
        }

        public static GameProcess Open(uint pid)
        {
            const PROCESS_ACCESS_RIGHT access = PROCESS_ACCESS_RIGHT.PROCESS_VM_OPERATION
                | PROCESS_ACCESS_RIGHT.PROCESS_VM_READ
                // | PROCESS_ACCESS_RIGHT.PROCESS_VM_WRITE // we don't need it for now
                | PROCESS_ACCESS_RIGHT.PROCESS_QUERY_LIMITED_INFORMATION
                | PROCESS_ACCESS_RIGHT.PROCESS_QUERY_INFORMATION
                | PROCESS_ACCESS_RIGHT.PROCESS_CREATE_THREAD
                | PROCESS_ACCESS_RIGHT.PROCESS_TERMINATE;

            // TODO: unfuck VM_WRITE

            var process = Process.Open(pid, access);

            return new GameProcess(process);
        }

        /// <summary>
        /// Recovers a key used in encrypting process arguments.
        /// </summary>
        /// <returns>A key recovered from the time when the process was created.</returns>
        /// <remarks>
        /// This is possible because the key to encrypt arguments is just a high nibble value from GetTickCount() at the time when the process was created.
        /// (Thanks Wintermute!)
        /// </remarks>
        private uint GetArgumentEncryptionKey()
        {
            var createdTime = m_process.GetCreationTime();

            // Get current tick
            var currentDt = DateTime.Now;
            var currentTick = Environment.TickCount;

            // We know that GetTickCount() is just a system uptime in milliseconds.
            var delta = currentDt - createdTime;
            var createdTick = (uint)currentTick - (uint)delta.TotalMilliseconds;

            // only the high nibble is used.
            return createdTick & 0xFFFF_0000;
        }

        /// <summary>
        /// Reads command-line arguments from the game and decrypts them if necessary.
        /// </summary>
        /// <returns>
        /// Command-line arguments that looks like this:
        /// /DEV.TestSID =ABCD /UserPath =C:\Examples
        /// </returns>
        public string ReadArguments()
        {
            var processArguments = m_process.GetArguments();

            // arg[0] is a path to exe(normally), arg[1] is actual stuff.
            if (processArguments.Length < 2)
            {
                throw new BootstrapException($"Process id {m_process.GetPid()} have no arguments to parse.");
            }

            // We're interested in argument that contains session id
            var argument = processArguments[1];

            // If it's encrypted, we need to decrypt it first
            if (EncryptedArgument.TryParse(argument, out var encryptedArgument))
            {
                var key = GetArgumentEncryptionKey();
                argument = encryptedArgument.Decrypt(key);
            }

            return argument;
        }
    }
}
