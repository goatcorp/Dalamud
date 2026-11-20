using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Dalamud.Utility.Unicode;

using Xunit;

namespace Dalamud.Test.Utility.Unicode
{
    internal static class UnicodeTestHelpers
    {
        public static List<int> Collect(UnicodeGraphemeClusterEnumerator enumerator)
        {
            var offsets = new List<int>();
            foreach (var offset in enumerator)
                offsets.Add(offset);
            return offsets;
        }

        public static List<int> Collect(UnicodeLineBreakEnumerator enumerator)
        {
            var offsets = new List<int>();
            foreach (var (offset, _) in enumerator)
                offsets.Add(offset);
            return offsets;
        }

        /// <summary>Runs the cases of a UCD break test file, such as <c>GraphemeBreakTest.txt</c>.</summary>
        /// <param name="testFile">The file, relative to the test output directory.</param>
        /// <param name="enumerate">Gets the offsets of the breaks in UTF-8 text.</param>
        public static void RunBreakTest(string testFile, Func<byte[], List<int>> enumerate)
        {
            var failures = new List<string>();
            var cases = 0;
            foreach (var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, testFile)))
            {
                var test = line.Split('#')[0].Trim();
                if (test.Length == 0)
                    continue;

                // In "÷ 0020 × 0308 ÷", the marks are the break before each codepoint, and at the end.
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

                var actual = enumerate(bytes.ToArray());
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
    }
}
