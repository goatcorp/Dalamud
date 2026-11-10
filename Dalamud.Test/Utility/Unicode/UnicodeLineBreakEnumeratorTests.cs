using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Dalamud.Utility.Unicode;

using Lumina.Text;

using Xunit;

namespace Dalamud.Test.Utility.Unicode
{
    public class UnicodeLineBreakEnumeratorTests
    {
        // https://www.unicode.org/Public/UCD/latest/ucd/auxiliary/LineBreakTest.txt
        private const string TestFile = "Utility/Unicode/LineBreakTest.txt";

        [Fact]
        public void PassesUnicodeLineBreakTest()
        {
            var failures = new List<string>();
            var cases = 0;
            foreach (var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, TestFile)))
            {
                var test = line.Split('#')[0].Trim();
                if (test.Length == 0)
                    continue;

                // In "× 0023 ÷ 0020 ÷", the marks are the break before each codepoint, and at the end.
                var bytes = new List<byte>();
                var expected = new List<int>();
                foreach (var token in test.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token == "÷")
                    {
                        if (bytes.Count != 0)
                            expected.Add(bytes.Count);
                    }
                    else if (token != "×")
                    {
                        bytes.AddRange(
                            Encoding.UTF8.GetBytes(char.ConvertFromUtf32(int.Parse(token, NumberStyles.HexNumber))));
                    }
                }

                var actual = new List<int>();
                foreach (var (offset, _) in new UnicodeLineBreakEnumerator(bytes.ToArray(), UtfEnumeratorFlags.Utf8, false))
                    actual.Add(offset);

                cases++;
                if (!expected.SequenceEqual(actual))
                    failures.Add(
                        $"{line}\n    got: {string.Join(", ", actual)}; expected: {string.Join(", ", expected)}");
            }

            Assert.True(cases > 0, "No test cases were read.");
            Assert.True(
                failures.Count == 0,
                $"{failures.Count} of {cases} cases failed:\n{string.Join("\n", failures.Take(50))}");
        }

        [Theory]
        [InlineData(true, new[] { "ภาษา", "ไทย", "ง่าย", "นิด", "เดียว" })]
        [InlineData(false, new[] { "ภาษาไทยง่ายนิดเดียว" })]
        public void BreaksThaiBetweenWords(bool findWords, string[] words)
        {
            var data = Encoding.UTF8.GetBytes(string.Concat(words));
            var expected = new List<int>();
            var end = 0;
            foreach (var word in words)
                expected.Add(end += Encoding.UTF8.GetByteCount(word));

            var actual = new List<int>();
            foreach (var (offset, _) in new UnicodeLineBreakEnumerator(data, UtfEnumeratorFlags.Utf8, findWords))
                actual.Add(offset);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IgnoresPayloadsWithoutRepresentativeCharacters()
        {
            var data = new SeStringBuilder()
                       .Append('a')
                       .PushColorType(1)
                       .Append('b')
                       .PopColorType()
                       .Append('c')
                       .GetViewAsSpan();
            var breaks = new List<int>();
            foreach (var (offset, _) in new UnicodeLineBreakEnumerator(data, UtfEnumeratorFlags.Utf8SeString))
                breaks.Add(offset);
            Assert.Equal([data.Length], breaks);
        }
    }
}
