using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Bootstrap.Crypto;

namespace Dalamud.Bootstrap.SqexArg
{
    internal sealed class EncodedArgument : IDisposable
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

        /// <summary>
        /// A data that is not encrypted.
        /// </summary>
        private IMemoryOwner<byte> m_data;

        /// <summary>
        /// Creates an object that can take (e.g. /T=1234)
        /// </summary>
        /// <param name="data">A data that is not encrypted.</param>
        /// <remarks>
        /// This takes the ownership of the data.
        /// </remarks>
        public EncodedArgument(IMemoryOwner<byte> data)
        {
            m_data = data;
        }

        
        public EncodedArgument(string argument)
        {
            var buffer = MemoryPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(argument));
            Encoding.UTF8.GetBytes(argument, buffer.Memory.Span);

            m_data = buffer;
        }

        public void Dispose()
        {
            m_data?.Dispose();
            m_data = null!;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        /// <exception cref="SqexArgException">
        /// Thrown when the function could not parse the encoded argument.
        /// Message property will carry additional information.
        /// </exception>
        public static EncodedArgument Parse(string argument)
        {
            // check if argument contains is large enough to contain start marker, checksum and end marker.
            if (argument.Length < "//**sqex0003!**//".Length)
            {
                var exMessage = $"The string ({argument}) is too short to parse the encoded argument."
                + $" It should be atleast large enough to store the start marker,checksum and end marker..";
                throw new SqexArgException(exMessage);
            }

            if (!argument.StartsWith("//**sqex0003") || !argument[13..].EndsWith("**//"))
            {
                var exMessage = $"The string ({argument}) doesn't look like the valid argument."
                    + $" It should start with //**sqex0003 and end with **// string.";
                throw new SqexArgException(exMessage);
            }

            // Extract the data
            var checksum = argument[^5];
            var encryptedData = DecodeUrlSafeBase64(argument.Substring(12, argument.Length - 1 - 12 - 4)); // //**sqex0003, checksum, **//

            // Dedice a partial key from the checksum
            var (partialKey, recoverStep) = RecoverKeyFragmentFromChecksum(checksum);

            var decryptedData = MemoryPool<byte>.Shared.Rent(encryptedData.Length);
            if (!RecoverKey(encryptedData, decryptedData.Memory.Span, partialKey, recoverStep))
            {
                // we need to free the memory to avoid a memory leak.
                decryptedData.Dispose();

                var exMessage = $"Could not find a valid key to decrypt the encoded argument.";
                throw new SqexArgException(exMessage);
            }
            
            return new EncodedArgument(decryptedData);
        }

        private static bool RecoverKey(ReadOnlySpan<byte> encryptedData, Span<byte> decryptedData, uint partialKey, uint recoverStep)
        {
            
            Span<byte> keyBytes = stackalloc byte[8];
            var keyCandicate = partialKey;
            
            while (true)
            {
                CreateKey(keyBytes, keyCandicate);

                var blowfish = new Blowfish(keyBytes);
                blowfish.Decrypt(encryptedData, decryptedData);

                // Check if the decrypted data looks valid
                if (CheckDecryptedData(decryptedData))
                {
                    return true;
                }

                // Try again with the next key.
                try
                {
                    keyCandicate = checked(keyCandicate + recoverStep);
                }
                catch (OverflowException)
                {
                    // We've exhausted the key space and could not find a valid key.
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="decryptedData"></param>
        /// <returns></returns>
        private static bool CheckDecryptedData(ReadOnlySpan<byte> decryptedData)
        {
            // TODO
            return false;
        }

        /// <summary>
        /// Formats the key.
        /// </summary>
        /// <param name="key">A secret key.</param>
        /// <param name="destination">A buffer where formatted key will be stored. This must be larger than 8 bytes.</param>
        private static void CreateKey(uint key, Span<byte> destination)
        {
            if (!Utf8Formatter.TryFormat(key, destination, out var _, new StandardFormat('X', 8)))
            {
                var message = $"BUG: Could not create a key"; // This should not fail but..
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Deduces a partial key from the checksum.
        /// </summary>
        /// <returns>
        /// `partialKey` can be or'd (a | partialKey) to recover some bits from the key.
        /// </returns>
        /// <remarks>
        /// The partialKey here is very useful because it can further reduce the number of possible key
        /// from 0xFFFF to 0xFFF which is 16 times smaller. (and therefore we can initialize the blowfish 16 times less which is quite expensive to do so.)   
        /// </remarks>
        private static (uint partialKey, uint step) RecoverKeyFragmentFromChecksum(char checksum)
        {                
            return MemoryExtensions.IndexOf(ChecksumTable, checksum) switch
            {
                -1 => (0x0001_0000, 0x0001_0000), // This covers '!' as well (no checksum are encoded)
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
