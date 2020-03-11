using System;

namespace Dalamud.Bootstrap.SqexArg
{
    internal static class ArgumentDecoder
    {
        public static void Decode(ReadOnlySpan<char> argument, uint key)
        {
            // 1. strip //**sqex003 and **//
            // 2. extract checksum
            // 3. deduce upper nibble key
            // 4. 

            // //**c**//
            if (argument.Length <= 9)
            {
                throw new ArgumentException(nameof(argument));
            }

            if (!argument.StartsWith("//**") || !argument.EndsWith("**//"))
            {
                throw new ArgumentException(nameof(argument));
            }

            var payload = argument[4..^5];
            var checksum = argument[^5];

            // undo url safe
            //payload.re

            
            
            // stuff
            
        }

        private static void DecodeUrlSafeBase64(ReadOnlySpan<char> content)
        {
            var buffer = new byte[(content.Length / 3) * 4];
            if (!Convert.TryFromBase64Chars(payload, buffer, out var _))
            {
                // TODO
            }
        }

    }
}
