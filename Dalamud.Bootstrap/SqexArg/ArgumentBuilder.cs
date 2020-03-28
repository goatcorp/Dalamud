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

        public static ArgumentBuilder Parse(string argument)
        {
            
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

        public bool ContainsKey(string key) => m_dict.ContainsKey(key);

        public bool ContainsValue(string value) => m_dict.ContainsValue(value);

        public ArgumentBuilder Remove(string key)
        {
            m_dict.Remove(key);

            return this;
        }

        public bool TryRemove(string key) => m_dict.Remove(key);

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
            var buffer = new StringBuilder(300);
            
            foreach (var kv in m_dict)
            {
                Write(buffer, kv.Key, kv.Value);
            }

            return buffer.ToString();
        }
    }
}
