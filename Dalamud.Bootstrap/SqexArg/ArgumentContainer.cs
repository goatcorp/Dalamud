using System;

namespace Dalamud.Bootstrap.SqexArg
{
    internal sealed class ArgumentContainer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static bool Parse(string argument, out ArgumentContainer? container)
        {
            if (argument.Length < 17)
            {
                // does not contain: //**sqex003 + payload + checksum + **//
                container = null;
                return false;
            }

            if (!argument.StartsWith("//**sqex003") || !argument.EndsWith("**//"))
            {
                container = null;
                return false;
            }

            var checksum = argument[^5];
            var payload = argument[11..^5];

            // decode 

            //Convert.FromBase64String();
            //container = new ArgumentContainer(payload, checksum);

            container = null;
            return true;
        }
    }
}
