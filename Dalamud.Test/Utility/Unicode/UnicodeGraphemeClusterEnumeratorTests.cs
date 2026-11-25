using System;
using System.Collections.Generic;
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
        public void PassesUnicodeGraphemeBreakTest() =>
            UnicodeTestHelpers.RunBreakTest(
                TestFile,
                bytes => UnicodeTestHelpers.Collect(new UnicodeGraphemeClusterEnumerator(bytes)));

        [Theory]
        [InlineData("क्ष|त्र")] // Devanagari conjuncts (GB9c)
        [InlineData("ក្ក|ក")] // Khmer coeng (GB9c)
        [InlineData("é|a")] // a combining mark stays with its base (GB9)
        [InlineData("🇰🇷|🇯🇵")] // flags (GB12, GB13)
        public void KeepsClustersTogether(string separated)
        {
            var clusters = separated.Split('|');
            var data = Encoding.UTF8.GetBytes(string.Concat(clusters));
            var expected = new List<int>();
            var end = 0;
            foreach (var cluster in clusters)
                expected.Add(end += Encoding.UTF8.GetByteCount(cluster));

            Assert.Equal(expected, UnicodeTestHelpers.Collect(new UnicodeGraphemeClusterEnumerator(data)));
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
            var boundaries =
                UnicodeTestHelpers.Collect(new UnicodeGraphemeClusterEnumerator(data, UtfEnumeratorFlags.Utf8SeString));
            Assert.Equal([data.Length], boundaries);
        }

        [Fact]
        public void DoesNotAllocate()
        {
            // Text is laid out every frame, and the property tables are looked up per character, also in Debug builds
            // where the JIT does not optimize.
            var data = "Hello, 世界! 👋🏽 é"u8;
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
