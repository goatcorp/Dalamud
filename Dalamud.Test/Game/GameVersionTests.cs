using System;

using Dalamud.Common.Game;

using Newtonsoft.Json;

using Xunit;

namespace Dalamud.Test.Game
{
    public class GameVersionTests
    {
        [Fact]
        public void VersionComparisons()
        {
            var v1 = GameVersion.Parse("2021.01.01.0000.0000");
            var v2 = GameVersion.Parse("2021.01.01.0000.0000");
            Assert.True(v1 == v2);
            Assert.False(v1 != v2);
            Assert.False(v1 < v2);
            Assert.True(v1 <= v2);
            Assert.False(v1 > v2);
            Assert.True(v1 >= v2);
        }

        [Fact]
        public void VersionAddition()
        {
            var v1 = GameVersion.Parse("2021.01.01.0000.0000");
            var v2 = GameVersion.Parse("2021.01.05.0000.0000");
            Assert.Equal(v2, v1 + TimeSpan.FromDays(4));
        }

        [Fact]
        public void VersionAdditionAny()
        {
            Assert.Equal(GameVersion.Any, GameVersion.Any + TimeSpan.FromDays(4));
        }

        [Fact]
        public void VersionSubtraction()
        {
            var v1 = GameVersion.Parse("2021.01.05.0000.0000");
            var v2 = GameVersion.Parse("2021.01.01.0000.0000");
            Assert.Equal(v2, v1 - TimeSpan.FromDays(4));
        }

        [Fact]
        public void VersionSubtractionAny()
        {
            Assert.Equal(GameVersion.Any, GameVersion.Any - TimeSpan.FromDays(4));
        }

        [Fact]
        public void VersionClone()
        {
            var v1 = GameVersion.Parse("2021.01.01.0000.0000");
            var v2 = v1.Clone();
            Assert.NotSame(v1, v2);
        }

        [Fact]
        public void VersionCast()
        {
            var v = GameVersion.Parse("2021.01.01.0000.0000");
            Assert.Equal("2021.01.01.0000.0000", v);
        }

        [Theory]
        [InlineData("any", "any")]
        [InlineData("2021.01.01.0000.0000", "2021.01.01.0000.0000")]
        public void VersionEquality(string ver1, string ver2)
        {
            var v1 = GameVersion.Parse(ver1);
            var v2 = GameVersion.Parse(ver2);

            Assert.Equal(v1, v2);
            Assert.Equal(0, v1.CompareTo(v2));
            Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        }

        [Fact]
        public void VersionNullEquality()
        {
            // Tests `Equals(GameVersion? value)`
            Assert.False(GameVersion.Parse("2021.01.01.0000.0000").Equals(null));

            // Tests `Equals(object? value)`
            Assert.False(GameVersion.Parse("2021.01.01.0000.0000").Equals((object)null));
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
        [InlineData("any", "2020.06.15.0000.0000")]
        public void VersionComparisonInverse(string ver1, string ver2)
        {
            var v1 = GameVersion.Parse(ver1);
            var v2 = GameVersion.Parse(ver2);

            Assert.True(v1.CompareTo(v2) > 0);
        }

        [Fact]
        public void VersionComparisonNull()
        {
            var v = GameVersion.Parse("2020.06.15.0000.0000");

            // Tests `CompareTo(GameVersion? value)`
            Assert.True(v.CompareTo(null) > 0);

            // Tests `CompareTo(object? value)`
            Assert.True(v.CompareTo((object)null) > 0);
        }

        [Fact]
        public void VersionComparisonBoxed()
        {
            var v1 = GameVersion.Parse("2020.06.15.0000.0000");
            var v2 = GameVersion.Parse("2020.06.15.0000.0000");
            Assert.Equal(0, v1.CompareTo((object)v2));
        }

        [Fact]
        public void VersionComparisonBoxedInvalid()
        {
            var v = GameVersion.Parse("2020.06.15.0000.0000");
            Assert.Throws<ArgumentException>(() => v.CompareTo(42));
        }

        [Theory]
        [InlineData("2020.06.15.0000.0000")]
        [InlineData("2021.01.01.0000")]
        [InlineData("2021.01.01")]
        [InlineData("2021.01")]
        [InlineData("2021")]
        public void VersionParse(string ver)
        {
            var v = GameVersion.Parse(ver);
            Assert.NotNull(v);
        }

        [Theory]
        [InlineData("2020.06.15.0000.0000")]
        [InlineData("2021.01.01.0000")]
        [InlineData("2021.01.01")]
        [InlineData("2021.01")]
        [InlineData("2021")]
        public void VersionTryParse(string ver)
        {
            Assert.True(GameVersion.TryParse(ver, out var v));
            Assert.NotNull(v);
        }

        [Theory]
        [InlineData("2020.06.15.0000.0000")]
        [InlineData("2021.01.01.0000")]
        [InlineData("2021.01.01")]
        [InlineData("2021.01")]
        [InlineData("2021")]
        public void VersionConstructor(string ver)
        {
            var v = new GameVersion(ver);
            Assert.NotNull(v);
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

        [Theory]
        [InlineData("any", "any")]
        [InlineData("2020.06.15.0000.0000", "2020.06.15.0000.0000")]
        [InlineData("2021.01.01.0000", "2021.01.01.0000.0000")]
        [InlineData("2021.01.01", "2021.01.01.0000.0000")]
        [InlineData("2021.01", "2021.01.00.0000.0000")]
        [InlineData("2021", "2021.00.00.0000.0000")]
        public void VersionToString(string ver1, string ver2)
        {
            var v1 = GameVersion.Parse(ver1);
            Assert.Equal(ver2, v1.ToString());
        }

        [Fact]
        public void VersionIsSerializationSafe()
        {
            var v = GameVersion.Parse("2020.06.15.0000.0000");
            var serialized = JsonConvert.SerializeObject(v);
            var deserialized = JsonConvert.DeserializeObject<GameVersion>(serialized);
            Assert.Equal(v, deserialized);
        }

        [Fact]
        public void VersionInvalidDeserialization()
        {
            var serialized = """
                             {
                                "Year": -1,
                                "Month": -1,
                                "Day": -1,
                                "Major": -1,
                                "Minor": -1,
                             }
                             """;
            Assert.Throws<ArgumentOutOfRangeException>(() => JsonConvert.DeserializeObject<GameVersion>(serialized));
        }

        [Fact]
        public void VersionInvalidTypeDeserialization()
        {
            var serialized = """
                             {
                                "Value": "Hello"
                             }
                             """;
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<GameVersion>(serialized));
        }

        [Fact]
        public void VersionConstructorNegativeYear()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameVersion(-2024));
        }

        [Fact]
        public void VersionConstructorNegativeMonth()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameVersion(2024, -3));
        }

        [Fact]
        public void VersionConstructorNegativeDay()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameVersion(2024, 3, -13));
        }

        [Fact]
        public void VersionConstructorNegativeMajor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameVersion(2024, 3, 13, -1));
        }

        [Fact]
        public void VersionConstructorNegativeMinor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameVersion(2024, 3, 13, 0, -1));
        }

        [Fact]
        public void VersionParseNull()
        {
            Assert.Throws<ArgumentNullException>(() => GameVersion.Parse(null!));
        }
    }
}
