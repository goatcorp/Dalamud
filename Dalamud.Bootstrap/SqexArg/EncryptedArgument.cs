using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Bootstrap.Crypto;

namespace Dalamud.Bootstrap.SqexArg
{
    internal sealed class EncryptedArgument : IDisposable
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
        public EncryptedArgument(IMemoryOwner<byte> data)
        {
            m_data = data;
        }
        
        public EncryptedArgument(string argument)
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
        /// <param name="payload">An encrypted payload encoded in url-safe base64 string extracted. The value is undefined if the function fails.</param>
        /// <param name="checksum">A checksum of the key extracted. The value is undefined if the function fails.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public static bool Extract(string argument, out string payload, out char checksum)
        {
            // must start with //**sqex0003, some characters, one checksum character and end with **//
            var regex = new Regex(@"^\/\/\*\*sqex0003(?<payload>.+)(?<checksum>.)\*\*\/\/$");
            
            var match = regex.Match(argument);
            if (!match.Success)
            {
                payload = "";
                checksum = '\0';
                return false;
            }

            payload = match.Groups["payload"].Value;
            checksum = match.Groups["checksum"].Value[0];
            return true;
        }

        public static EncryptedArgument FromEncryptedData(string argument, uint key)
        {
            // Create the key
            Span<byte> keyBytes = stackalloc byte[8];
            CreateKey(key, keyBytes);

            // Allocate the buffer to store decrypted data
            var decryptedData = CreateAlignedBuffer(encryptedData.Length);
            
            // Decrypt the data with the key
            try
            {
                var blowfish = new Blowfish(keyBytes);
                blowfish.Decrypt(encryptedData, decryptedData.Memory.Span);
            }
            catch (Exception)
            {
                decryptedData?.Dispose(); // TODO: clean up this thing?
                throw;
            }
            
            return new EncryptedArgument(decryptedData);
        }

        private static IMemoryOwner<byte> CreateAlignedBuffer(int minimumSize)
        {
            // align (by padding) to block size if needed
            throw new NotImplementedException("TODO");
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
            if (!Utf8Formatter.TryFormat(key, destination, out var _, new StandardFormat('x', 8)))
            {
                var message = $"BUG: Could not create a key"; // This should not fail but..
                throw new InvalidOperationException(message);
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
            
            return Convert.FromBase64String(base64Str);
        }
    }
}
