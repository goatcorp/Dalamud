using Dalamud.Bootstrap.SqexArg;
using System;

namespace Dalamud.Bootstrap
{
    public sealed partial class GameProcess : IDisposable
    {
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
            var createdTime = GetCreationTime();

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
        public ArgumentBuilder GetGameArguments()
        {
            var processArguments = GetProcessArguments();

            // arg[0] is a path to exe(normally), arg[1] is actual stuff.
            if (processArguments.Length < 2)
            {
                throw new ProcessException($"There's only {processArguments.Length} process arguments. It must have at least 2 arguments.");
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
