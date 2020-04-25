using System;
using System.Collections.Generic;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Dalamud.Bootstrap.SqexArg
{
    public static class SqexArgumentParser
    {
        private static readonly Parser<char, string> KeyMarkerNoEscape = String(" ");

        private static readonly Parser<char, string> KeyMarkerEscape = String(" /");

        private static readonly Parser<char, string> KeyMarker = Try(KeyMarkerEscape).Or(KeyMarkerNoEscape);

        private static readonly Parser<char, string> ValueMarker = String(" =");

        private static readonly Parser<char, char> EscapedSpace = String("  ").ThenReturn(' ');

        //private static readonly Parser<char, string> String = Try(EscapedSpace).Or(AnyCharExcept(' ')).ManyString();
        private static readonly Parser<char, string> String = Try(EscapedSpace).Or(AnyCharExcept(' ')).ManyString();

        private static readonly Parser<char, KeyValuePair<string, string>> KeyValue = Map
        (
            (_, key, _, value) => new KeyValuePair<string, string>(key, value),
            KeyMarker, String, ValueMarker, String
        );

        private static readonly Parser<char, IEnumerable<KeyValuePair<string, string>>> Parser = KeyValue.Many();

        /// <summary>
        /// Parses the argument
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="SqexArgException">Thrown when failed to parse the input.</exception>
        public static IEnumerable<KeyValuePair<string, string>> Parse(string input)
        {
            var test = KeyMarker.Parse(input);
            var result = Parser.Parse(input);

            if (!result.Success)
            {
                throw new SqexArgException($"Failed to parse the argument.\n{result.Error}");
            }

            return result.Value;
        }
    }
}
