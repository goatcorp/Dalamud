using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Bootstrap.Crypto;

namespace Dalamud.Bootstrap.SqexArg
{
    internal sealed class EncryptedArgument
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

        private IMemoryOwner<byte> m_encryptedData;

        /// <summary>
        /// Encrypts the argument with given key.
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="key"></param>
        public EncryptedArgument(string argument, uint key)
        {
            
        }

        /// <summary>
        /// Extracts the payload and checksum from the encrypted argument.
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="payload">An encrypted payload extracted. The value is undefined if the function fails.</param>
        /// <param name="checksum">A checksum of the key extracted. The value is undefined if the function fails.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public static bool Extract(string argument, out byte[] payload, out char checksum)
        {
            // must start with //**sqex0003, some characters, one checksum character and end with **//
            var regex = new Regex(@"^\/\/\*\*sqex0003(?<payload>.+)(?<checksum>.)\*\*\/\/$");
            
            var match = regex.Match(argument);
            if (!match.Success)
            {
                payload = null!;
                checksum = '\0';
                return false;
            }

            // Extract checksum
            checksum = match.Groups["checksum"].Value[0];
            
            // Extract payload
            var payloadStr = match.Groups["payload"].Value;
            payload = DecodeUrlSafeBase64(payloadStr);

            return true;
        }

        public override string ToString()
        {
            var checksum = GetChecksumFromKey();
        }

        private static char GetChecksumFromKey(uint key)
        {
            var index = (key & 0x000F_0000) >> 16;
            
            return ChecksumTable[index];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minimumSize">Indicates that returned buffer must be larger than `minimumSize` bytes.</param>
        /// <returns>
        /// A buffer aligned to next multiple of block size.
        /// Dispose() must be called when it's not used anymore.
        /// </returns>
        private static IMemoryOwner<byte> CreateAlignedBuffer(int minimumSize)
        {
            // align to next multiple of block size.
            var alignedSize = (minimumSize + Blowfish.BlockSize - 1) & (~(-Blowfish.BlockSize));
            
            return MemoryPool<byte>.Shared.Rent(alignedSize);
        }

        /// <summary>
        /// Formats a key.
        /// </summary>
        /// <param name="key">A secret key.</param>
        /// <param name="destination">A buffer where formatted key will be stored. This must be larger than 8 bytes.</param>
        private static void CreateKey(uint key, Span<byte> destination)
        {
            if (!Utf8Formatter.TryFormat(key, destination, out var _, new StandardFormat('x', 8)))
            {
                throw new InvalidOperationException("BUG: Could not create a key");
            }
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
            
            try
            {
                return Convert.FromBase64String(base64Str);
            }
            catch (FormatException ex)
            {
                // This is expected to happen if the argument is ill-formed
                throw new SqexArgException($"A payload {payload} does not look like a valid encrypted argument.", ex);
            }            
        }

        private static string EncodeUrlSafeBase64(byte[] payload)
        {
            var payloadStr = Convert.ToBase64String(payload);
            
            return payloadStr
                .Replace('+', '-')
                .Replace('/', '_');

        }
    }
}
