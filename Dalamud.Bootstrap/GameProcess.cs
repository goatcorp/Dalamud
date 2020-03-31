using System;
using Dalamud.Bootstrap.SqexArg;
using Dalamud.Bootstrap.Windows;

namespace Dalamud.Bootstrap
{
    internal sealed class GameProcess : IDisposable
    {
        private Process m_process;

        public GameProcess(Process process)
        {
            m_process = process;
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

        public void Dispose()
        {
            m_process?.Dispose();
            m_process = null!;
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

        public ArgumentBuilder ReadArguments()
        {
            var arguments = m_process.ReadArguments();

            if (arguments.Length < 2)
            {
                throw new BootstrapException($"Process id {m_process.GetPid()} have no arguments to parse.");
            }

            var argument = arguments[1];

            if (EncryptedArgument.TryParse(argument, out var encryptedArgument))
            {
                var key = GetArgumentEncryptionKey();
                argument = encryptedArgument.Decrypt(key);
            }

            return ArgumentBuilder.Parse(argument);
        }
    }
}
