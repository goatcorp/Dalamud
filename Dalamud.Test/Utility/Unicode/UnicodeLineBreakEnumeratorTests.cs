using System.Collections.Generic;
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
        public void PassesUnicodeLineBreakTest() =>
            UnicodeTestHelpers.RunBreakTest(
                TestFile,
                bytes => UnicodeTestHelpers.Collect(
                    new UnicodeLineBreakEnumerator(bytes, UtfEnumeratorFlags.Utf8, false)));

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

            Assert.Equal(
                expected,
                UnicodeTestHelpers.Collect(new UnicodeLineBreakEnumerator(data, UtfEnumeratorFlags.Utf8, findWords)));
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
            var breaks =
                UnicodeTestHelpers.Collect(new UnicodeLineBreakEnumerator(data, UtfEnumeratorFlags.Utf8SeString));
            Assert.Equal([data.Length], breaks);
        }
    }
}
