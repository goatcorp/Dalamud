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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        public static EncodedArgument Parse(string argument)
        {
            if (argument.Length <= 17)
            {
                // does not contain: //**sqex0003 + payload + checksum + **//
                var exMessage = $"The string ({argument}) is too short to parse encoded argument.";
                throw new SqexArgException(exMessage);
            }

            if (!argument.StartsWith("//**sqex0003") || !argument.EndsWith("**//"))
            {
                var exMessage = $"The string ({argument}) doesn't look like valid encoded argument format."
                    + $"It either doesn't start with //**sqeex003 or end with **// marker.";
                throw new SqexArgException(exMessage);
            }

            var checksum = argument[^5];
            var payload = DecodeUrlSafeBase64(argument.Substring(12, argument.Length - 1 - 12 - 4)); // //**sqex0003, checksum, **//

            // ...
        }

        private static bool GetKeyFragmentFromChecksum(char checksum, out uint recoveredKey)
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
