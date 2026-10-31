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
    public class UnicodeGraphemeClusterEnumeratorTests
    {
        // https://www.unicode.org/Public/UCD/latest/ucd/auxiliary/GraphemeBreakTest.txt
        private const string TestFile = "Utility/Unicode/GraphemeBreakTest.txt";

        [Fact]
        public void PassesUnicodeGraphemeBreakTest()
        {
            var failures = new List<string>();
            var cases = 0;
            foreach (var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, TestFile)))
            {
                var test = line.Split('#')[0].Trim();
                if (test.Length == 0)
                    continue;

                // In "÷ 0020 × 0308 ÷", the marks are the boundary before each codepoint, and at the end.
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
                foreach (var offset in new UnicodeGraphemeClusterEnumerator(bytes.ToArray()))
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
        [InlineData("क्ष|त्र")] // Devanagari conjuncts (GB9c)
        [InlineData("ក្ក|ក")] // Khmer coeng (GB9c)
        [InlineData("e\u0301|a")] // a combining mark stays with its base (GB9)
        [InlineData("🇰🇷|🇯🇵")] // flags (GB12, GB13)
        public void KeepsClustersTogether(string separated)
        {
            var clusters = separated.Split('|');
            var data = Encoding.UTF8.GetBytes(string.Concat(clusters));
            var expected = new List<int>();
            var end = 0;
            foreach (var cluster in clusters)
                expected.Add(end += Encoding.UTF8.GetByteCount(cluster));

            var actual = new List<int>();
            foreach (var offset in new UnicodeGraphemeClusterEnumerator(data))
                actual.Add(offset);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IgnoresPayloadsWithoutRepresentativeCharacters()
        {
            var data = new SeStringBuilder()
                       .Append('e')
                       .PushColorType(1)
                       .Append('́')
                       .PopColorType()
                       .GetViewAsSpan();
            var boundaries = new List<int>();
            foreach (var offset in new UnicodeGraphemeClusterEnumerator(data, UtfEnumeratorFlags.Utf8SeString))
                boundaries.Add(offset);
            Assert.Equal([data.Length], boundaries);
        }

        [Fact]
        public void DoesNotAllocate()
        {
            // Text is laid out every frame, and the property tables are looked up per character, also in Debug builds
            // where the JIT does not optimize.
            var data = "Hello, 世界! 👋🏽 é"u8;
            foreach (var offset in new UnicodeGraphemeClusterEnumerator(data))
                _ = offset;
            var before = GC.GetAllocatedBytesForCurrentThread();
            var count = 0;
            foreach (var offset in new UnicodeGraphemeClusterEnumerator(data))
                count += offset > 0 ? 1 : 0;
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
            Assert.True(count > 0);
        }
    }
}
