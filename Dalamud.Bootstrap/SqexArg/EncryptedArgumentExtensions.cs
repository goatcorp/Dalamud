using System;
using System.Buffers;
using System.Buffers.Text;
using Dalamud.Bootstrap.Crypto;

namespace Dalamud.Bootstrap.SqexArg
{
    internal static class EncryptedArgumentExtensions
    {
        public static void Decrypt(this EncryptedArgument argument, uint key)
        {
            Span<byte> keyBytes = stackalloc byte[8];
            CreateKey(key, keyBytes);

            var blowfish = new Blowfish(keyBytes);
            
            
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
