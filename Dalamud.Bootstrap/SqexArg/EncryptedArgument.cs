using System.Text.RegularExpressions;

namespace Dalamud.Bootstrap.SqexArg
{
    internal sealed class EncryptedArgument
    {
        /// <summary>
        /// A data that is encrypted and encoded in url-safe variant of base64.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// A checksum that is used to validate the encryption key.
        /// </summary>
        public char Checksum { get; }

        public EncryptedArgument(string data, char checksum)
        {
            Data = data;
            Checksum = checksum;
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
    }
}
