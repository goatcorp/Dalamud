using System.Collections.Generic;
using System.Text;

namespace Dalamud.Bootstrap.SqexArg
{
    public sealed class SqexArgBuilder
    {
        private readonly IDictionary<string, string> m_dict;

        public SqexArgBuilder()
        {
            m_dict = new Dictionary<string, string>();
        }

        public SqexArgBuilder(IDictionary<string, string> collection)
        {
            m_dict = collection;
        }

        /// <summary>
        /// Creates an argument builder from the argument (e.g. /T =1234)
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        public static SqexArgBuilder Parse(string argument)
        {
            var arguments = new Dictionary<string, string>(SqexArgumentParser.Parse(argument)); // uhhh

            return new SqexArgBuilder(arguments);
        }

        public SqexArgBuilder Add(string key, string value)
        {
            m_dict.Add(key, value);

            return this;
        }

        public SqexArgBuilder Clear()
        {
            m_dict.Clear();

            return this;
        }

        public bool ContainsKey(string key) => m_dict.ContainsKey(key);

        //public bool ContainsValue(string value) => m_dict.ContainsValue(value);

        public SqexArgBuilder Remove(string key)
        {
            m_dict.Remove(key);

            return this;
        }

        public bool TryRemove(string key) => m_dict.Remove(key);



        private static string EscapeValue(string value)
        {
            // a single space character is represented as dobule spaces
            return value.Replace(" ", "  ");
        }

        private string BuildRawString()
        {
            // This is not exposed because (from chat):
            // This line, the = in your version has a space before it
            // If you're sending the arguments in plaintext, game doesn't like the space there
            // (But I think it needs to be there in crypted args)
            var buffer = new StringBuilder(300);

            foreach (var kv in m_dict)
            {
                WriteKeyValue(buffer, kv.Key, kv.Value);
            }

            return buffer.ToString();
        }

        private static void WriteKeyValue(StringBuilder buffer, string key, string value)
        {
            var escapedKey = EscapeValue(key);
            var escapedvalue = EscapeValue(value);

            buffer.Append($" /{escapedKey} ={escapedvalue}");
        }

        public string Build(uint key)
        {
            var plainText = BuildRawString();
            
            var enc = new SqexEncryptedArgument(plainText, key);
            
            return enc.Build();            
        }
    }
}
