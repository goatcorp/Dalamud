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

        /// <summary>
        /// Denotes that no checksum is encoded.
        /// </summary>
        private const char NoChecksumMarker = '!';

        private readonly ReadOnlyMemory<byte> m_data;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="checksum"></param>
        /// <returns></returns>
        private static Blowfish RecoverKey(ReadOnlySpan<byte> payload, char checksum)
        {
            var (keyFragment, step) = RecoverKeyFragmentFromChecksum(checksum);

            Span<byte> keyBytes = stackalloc byte[8];

            var keyCandicate = keyFragment;
            
            while (true)
            {
                if (!CreateKey(keyBytes, keyCandicate))
                {
                    var message = $"BUG";
                    throw new InvalidOperationException(message);
                }

                var blowfish = new Blowfish(keyBytes);
                
                if (/* if data looks valid, return blowfish */)
                {
                    // ...
                    // TODO: test if it's looks like valid utf8 string?
                    // ???

                    return blowfish;
                }

                // .. next key plz
                try
                {
                    keyCandicate = checked(keyCandicate + step);
                }
                catch (OverflowException)
                {
                    break;
                }
            }
        }

        private static bool CreateKey(Span<byte> destination, uint key) => Utf8Formatter.TryFormat(key, destination, out var _, new StandardFormat('X', 8));

        private static (uint keyFragment, uint step) RecoverKeyFragmentFromChecksum(char checksum)
        {
            if (checksum == NoChecksumMarker)
            {
                return (0x0001_0000, 0x0001_0000);
            }
                
            return MemoryExtensions.IndexOf(ChecksumTable, checksum) switch
            {
                -1 => throw new SqexArgException($"{checksum} is not a valid checksum character."),
                var index => ((uint) (index << 16), 0x0010_0000)
            };
        }

        /// <summary>
        /// Converts the url-safe variant of base64 string to bytes.
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
