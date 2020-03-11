using System.Collections.Generic;
using System.Text;

namespace Dalamud.Bootstrap.SqexArg
{
    internal sealed class ArgumentBuilder
    {
        private readonly Dictionary<string, string> m_dict;

        public ArgumentBuilder()
        {
            m_dict = new Dictionary<string, string>();
        }

        public ArgumentBuilder Add(string key, string value)
        {
            m_dict.Add(key, value);

            return this;
        }

        public ArgumentBuilder Clear()
        {
            m_dict.Clear();

            return this;
        }

        private static void Write(StringBuilder buffer, string key, string value)
        {
            var escapedKey = EscapeValue(key);
            var escapedvalue = EscapeValue(value);

            buffer.Append($" /{escapedKey} ={escapedvalue}");
        }

        private static string EscapeValue(string value)
        {
            // a single space character is represented as dobule spaces
            return value.Replace(" ", "  ");
        }

        public override string ToString()
        {
            var buffer = new StringBuilder(256);
            
            foreach (var kv in m_dict)
            {
                Write(buffer, kv.Key, kv.Value);
            }

            return buffer.ToString();
        }
    }
}
