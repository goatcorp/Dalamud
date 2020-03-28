using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Bootstrap.Crypto;

namespace Dalamud.Bootstrap.SqexArg
{
    internal sealed class EncryptedArgument
    {
        private static readonly char[] ChecksumTable =
        {
            'f', 'X', '1', 'p', 'G', 't', 'd', 'S',
            '5', 'C', 'A', 'P', '4', '_', 'V', 'L'
        };
        
        /// <summary>
        /// A data that is encrypted and encoded in url-safe variant of base64.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// A checksum that is used to validate the encryption key.
        /// </summary>
        public char Checksum { get; }

        /// <summary>
        /// Creates an encrypted argument.
        /// Unlike other constructors, this does not encrypt the data passed to encdoedData parameter and assume that process is already done.
        /// </summary>
        /// <param name="encodedData">A string that is already encrypted and encoded to url-safe variant of base64.</param>
        /// <param name="checksum">A checksum that is used to validate the encryption key.</param>
        private EncryptedArgument(string encodedData, char checksum)
        {
            Data = encodedData;
            Checksum = checksum;
        }

        /// <summary>
        /// Encrypts a string with given key.
        /// </summary>
        /// <param name="plainText">A data that is not encrypted.</param>
        /// <param name="key">A key that is used to encrypt the data.</param>
        public EncryptedArgument(string plainText, uint key)
        {
            Span<byte> keyBytes = stackalloc byte[8];
            CreateKey(key, keyBytes);
            
            var blowfish = new Blowfish(keyBytes);

            Data = EncodeString(plainText, blowfish);
            Checksum = GetChecksum(key);
        }

        private static char GetChecksum(uint key)
        {
            // There's no OoB since ChecksumTable has 16 elements and we mask the key with 0xF
            var index = (key >> 16) & 0x0000_000F;

            return ChecksumTable[index];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="SqexArgException">Thrown when the data property does not have a valid base64 string.</exception>
        public string Decrypt(uint key)
        {
            Span<byte> keyBytes = stackalloc byte[8];
            CreateKey(key, keyBytes);

            var blowfish = new Blowfish(keyBytes);

            return DecodeString(Data, blowfish);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="blowfish"></param>
        /// <returns></returns>
        /// <exception cref="SqexArgException">Thrown when the data property does not have a valid base64 string.</exception>
        private static string DecodeString(string payload, Blowfish blowfish)
        {
            // plainText <- utf8Bytes <- encryptedBytes(payload) <- base64(payloadStr)

            // base64: 3 bytes per 4 characters
            // We also want the size to be aligned with the block size.
            var dataLength = (payload.Length / 4) * 3;
            var alignedDataLength = AlignBufferLength(dataLength);
            var encryptedData = new byte[alignedDataLength];
            var decryptedData = new byte[alignedDataLength];

            // Converts to standard base64 string so that we can feed it to stdlib
            var base64Str = payload
                .Replace('-', '+')
                .Replace('_', '/');
            
            // base64 -> encryptedBytes
            if (!Convert.TryFromBase64String(base64Str, encryptedData, out var _))
            {
                // We don't care about bytesWritten because we can't handle failure anyway
                throw new SqexArgException($"A payload {payload} does not look like a valid encrypted argument.");
            }

            // encryptedBytes -> utf8Bytes (decrypted)
            blowfish.Decrypt(encryptedData, decryptedData);

            // utf8Bytes -> C# string
            var plainText = Encoding.UTF8.GetString(decryptedData[..dataLength]);

            return plainText;
        }

        /// <summary>
        /// Converts plain text string to url-safe variant of base64 string.
        /// </summary>
        private static string EncodeString(string plainText, Blowfish blowfish)
        {
            // plainText -> utf8Bytes -> encryptedBytes(payload) -> base64(payloadStr)

            // This is needed because we want the size to be aligned with the block size. We'll also need to pad them zero.
            var utf8BytesLength = AlignBufferLength(Encoding.UTF8.GetByteCount(plainText));
            var utf8Bytes = new byte[utf8BytesLength];

            // We also need the buffer to store encrypted bytes
            var encryptedBytes = new byte[utf8Bytes.Length];

            // Now we can the string to UTF8
            // NOTE: This should fail as GetByteCount returns the exact size required to encode utf8 string, but if this assumption is wrong, please make an issue.
            Encoding.UTF8.GetBytes(plainText, utf8Bytes);

            // Encrypt it
            blowfish.Encrypt(utf8Bytes, encryptedBytes);

            // Convert to url-safe variant of base64
            var base64Str = Convert.ToBase64String(encryptedBytes, Base64FormattingOptions.None)
                    .Replace('+', '-')
                    .Replace('/', '_');

            return base64Str;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument">An argument that is encrypted and usually starts with //**sqex0003 and ends with **//</param>
        /// <returns>Returns true if successful, false otherwise.</returns>
        public static bool TryParse(string argument, out EncryptedArgument output)
        {
            if (!Extract(argument, out var data, out var checksum))
            {
                output = null!;
                return false;
            }

            output = new EncryptedArgument(data, checksum);
            return true;
        }

        /// <summary>
        /// Extracts the payload and checksum from the encrypted argument.
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="payload">An encrypted payload extracted. The value is undefined if the function fails.</param>
        /// <param name="checksum">A checksum of the key extracted. The value is undefined if the function fails.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        private static bool Extract(string argument, out string payload, out char checksum)
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

            // Extract
            checksum = match.Groups["checksum"].Value[0];
            payload = match.Groups["payload"].Value;

            return true;
        }

        public override string ToString() => $"//**sqex0003{Data}{Checksum}**//";

        /// <summary>
        /// Formats a key.
        /// </summary>
        /// <param name="key">A secret key.</param>
        /// <param name="destination">A buffer where formatted key will be stored. This must be larger than 8 bytes.</param>
        internal static void CreateKey(uint key, Span<byte> destination)
        {
            if (!Utf8Formatter.TryFormat(key, destination, out var _, new StandardFormat('x', 8)))
            {
                throw new InvalidOperationException("BUG: Could not create a key");
            }
        }

        /// <summary>
        /// Rounds to next mutliple of block size (i.e. 8 bytes) to satisfy with Blowfish requirements.
        /// </summary>
        internal static int AlignBufferLength(int length) => (length + (Blowfish.BlockSize - 1)) & (-Blowfish.BlockSize);
    }
}
