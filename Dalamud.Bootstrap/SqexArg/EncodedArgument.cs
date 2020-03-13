using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Bootstrap.Crypto;

namespace Dalamud.Bootstrap.SqexArg
{
    internal sealed class EncodedArgument
    {
        private static char[] ChecksumTable = new char[]
        {
            'f', 'X', '1', 'p', 'G', 't', 'd', 'S',
            '5', 'C', 'A', 'P', '4', '_', 'V', 'L'
        };

        private readonly ReadOnlyMemory<byte> m_data;

        private readonly uint m_key;

        //private readonly Blowfish m_cachedBlowfish;

        public EncodedArgument(ReadOnlyMemory<byte> data, uint key)
        {
            m_data = data;
            m_key = key;
        }

        public static bool TryParse(string argument, out EncodedArgument? value)
        {
            if (argument.Length <= 17)
            {
                // does not contain: //**sqex0003 + payload + checksum + **//
                value = null;
                return false;
            }

            if (!argument.StartsWith("//**sqex0003") || !argument.EndsWith("**//"))
            {
                value = null;
                return false;
            }

            var checksum = argument[^5];
            var payload = DecodeUrlSafeBase64(argument.Substring(12, argument.Length - 1 - 12 - 4)); // //**sqex0003, checksum, **//

            if (!FindPartialKey(checksum, out var partialKey))
            {
                value = null;
                return false;
            }

            for (var i = 0u; i <= 0xFFF; i++)
            {
                var key = (i << 20) | partialKey;
            }
        }

        private static bool FindPartialKey(char checksum, out uint recoveredKey)
        {
            var index = MemoryExtensions.IndexOf(ChecksumTable, checksum);
            
            if (index < 0)
            {
                recoveredKey = 0;
                return false;
            }

            recoveredKey = (uint) (index << 16);
            return true;
        }
        
        /// <summary>
        /// Converts url safe variant of base64 string to bytes.
        /// </summary>
        /// <param name="payload">A url-safe variant of base64 string.</param>
        private static byte[] DecodeUrlSafeBase64(string payload)
        {
            var base64Str = payload
                .Replace('-', '+')
                .Replace('_', '/');
            
            return Convert.FromBase64String(base64Str);
        }
    }
}
