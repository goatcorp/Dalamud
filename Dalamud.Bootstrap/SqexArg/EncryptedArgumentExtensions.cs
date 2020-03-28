using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using Dalamud.Bootstrap.Crypto;

namespace Dalamud.Bootstrap.SqexArg
{
    internal static class EncryptedArgumentExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="SqexArgException">Thrown when the data property does not have a valid base64 string.</exception>
        public static string Decrypt(this EncryptedArgument argument, uint key)
        {
            Span<byte> keyBytes = stackalloc byte[8];
            CreateKey(key, keyBytes);

            var encryptedData = DecodeDataString(argument.Data, out var encryptedDataLength);
            var decryptedData = new byte[encryptedData.Length];

            var blowfish = new Blowfish(keyBytes);
            blowfish.Decrypt(encryptedData, decryptedData);

            return Encoding.UTF8.GetString(decryptedData[..encryptedDataLength]);
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
        /// Converts the data string to bytes. It also allocates more bytes than actual data contained in base64 string for Blowfish.
        /// </summary>
        /// <param name="payload">A url-safe variant of base64 string.</param>
        /// <param name="payload">A data length that is actually written to the buffer.</param>
        private static byte[] DecodeDataString(string payload, out int dataLength)
        {
            var base64Str = payload
                .Replace('-', '+')
                .Replace('_', '/');

            // base64: 3 bytes per 4 characters
            dataLength = (payload.Length / 4) * 3;

            // round to next mutliple of block size which is what Blowfish can process. (i.e. 8 bytes)
            var alignedLength = (dataLength + (Blowfish.BlockSize - 1)) & (-Blowfish.BlockSize);

            var buffer = new byte[alignedLength];

            if (!Convert.TryFromBase64String(base64Str, buffer, out var _))
            {
                throw new SqexArgException($"A payload {payload} does not look like a valid encrypted argument.");
            }

            return buffer;
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
