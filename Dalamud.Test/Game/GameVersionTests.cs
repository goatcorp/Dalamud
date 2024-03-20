using Dalamud.Common.Game;
using Xunit;

namespace Dalamud.Test.Game
{
    public class GameVersionTests
    {
        [Theory]
        [InlineData("any", "any")]
        [InlineData("2021.01.01.0000.0000", "2021.01.01.0000.0000")]
        public void VersionEquality(string ver1, string ver2)
        {
            var v1 = GameVersion.Parse(ver1);
            var v2 = GameVersion.Parse(ver2);

            Assert.Equal(v1, v2);
        }

        [Theory]
        [InlineData("2020.06.15.0000.0000", "any")]
        [InlineData("2021.01.01.0000.0000", "2021.01.01.0000.0001")]
        [InlineData("2021.01.01.0000.0000", "2021.01.01.0001.0000")]
        [InlineData("2021.01.01.0000.0000", "2021.01.02.0000.0000")]
        [InlineData("2021.01.01.0000.0000", "2021.02.01.0000.0000")]
        [InlineData("2021.01.01.0000.0000", "2022.01.01.0000.0000")]
        public void VersionComparison(string ver1, string ver2)
        {
            var v1 = GameVersion.Parse(ver1);
            var v2 = GameVersion.Parse(ver2);

            Assert.True(v1.CompareTo(v2) < 0);
        }

        [Theory]
        [InlineData("2020.06.15.0000.0000")]
        [InlineData("2021.01.01.0000")]
        [InlineData("2021.01.01")]
        [InlineData("2021.01")]
        [InlineData("2021")]
        public void VersionConstructor(string ver)
        {
            var v = GameVersion.Parse(ver);

            Assert.True(v != null);
        }

        [Theory]
        [InlineData("2020.06.15.0000.0000.0000")]
        [InlineData("")]
        public void VersionConstructorInvalid(string ver)
        {
            var result = GameVersion.TryParse(ver, out var v);

            Assert.False(result);
            Assert.Null(v);
        }
    }
}
