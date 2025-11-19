using System;
using System.Linq;

using Dalamud.Networking.Rpc.Model;

using Xunit;

namespace Dalamud.Test.Rpc
{
    public class DalamudUriTests
    {
        [Theory]
        [InlineData("https://www.google.com/", false)]
        [InlineData("dalamud://PluginInstaller/Dalamud.FindAnything", true)]
        public void ValidatesScheme(string uri, bool valid)
        {
            Action act = () => { _ = DalamudUri.FromUri(uri); };

            var ex = Record.Exception(act);
            if (valid)
            {
                Assert.Null(ex);
            }
            else
            {
                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
            }
        }

        [Theory]
        [InlineData("dalamud://PluginInstaller/Dalamud.FindAnything", "plugininstaller")]
        [InlineData("dalamud://Plugin/Dalamud.FindAnything/OpenWindow", "plugin")]
        [InlineData("dalamud://Test", "test")]
        public void ExtractsNamespace(string uri, string expectedNamespace)
        {
            var dalamudUri = DalamudUri.FromUri(uri);
            Assert.Equal(expectedNamespace, dalamudUri.Namespace);
        }

        [Theory]
        [InlineData("dalamud://foo/bar/baz/qux/?cow=moo", "/bar/baz/qux/")]
        [InlineData("dalamud://foo/bar/baz/qux?cow=moo", "/bar/baz/qux")]
        [InlineData("dalamud://foo/bar/baz", "/bar/baz")]
        [InlineData("dalamud://foo/bar", "/bar")]
        [InlineData("dalamud://foo/bar/", "/bar/")]
        [InlineData("dalamud://foo/", "/")]
        public void ExtractsPath(string uri, string expectedPath)
        {
            var dalamudUri = DalamudUri.FromUri(uri);
            Assert.Equal(expectedPath, dalamudUri.Path);
        }

        [Theory]
        [InlineData("dalamud://foo/bar/baz/qux/?cow=moo#frag", "/bar/baz/qux/?cow=moo#frag")]
        [InlineData("dalamud://foo/bar/baz/qux/?cow=moo", "/bar/baz/qux/?cow=moo")]
        [InlineData("dalamud://foo/bar/baz/qux?cow=moo", "/bar/baz/qux?cow=moo")]
        [InlineData("dalamud://foo/bar/baz", "/bar/baz")]
        [InlineData("dalamud://foo/bar?cow=moo", "/bar?cow=moo")]
        [InlineData("dalamud://foo/bar", "/bar")]
        [InlineData("dalamud://foo/bar/?cow=moo", "/bar/?cow=moo")]
        [InlineData("dalamud://foo/bar/", "/bar/")]
        [InlineData("dalamud://foo/?cow=moo#chicken", "/?cow=moo#chicken")]
        [InlineData("dalamud://foo/?cow=moo", "/?cow=moo")]
        [InlineData("dalamud://foo/", "/")]
        public void ExtractsData(string uri, string expectedData)
        {
            var dalamudUri = DalamudUri.FromUri(uri);

            Assert.Equal(expectedData, dalamudUri.Data);
        }

        [Theory]
        [InlineData("dalamud://foo/bar", 0)]
        [InlineData("dalamud://foo/bar?cow=moo", 1)]
        [InlineData("dalamud://foo/bar?cow=moo&wolf=awoo", 2)]
        [InlineData("dalamud://foo/bar?cow=moo&wolf=awoo&cat", 3)]
        public void ExtractsQueryParams(string uri, int queryCount)
        {
            var dalamudUri = DalamudUri.FromUri(uri);
            Assert.Equal(queryCount, dalamudUri.QueryParams.Count);
        }

        [Theory]
        [InlineData("dalamud://foo/bar/baz/qux/meh/?foo=bar", 5, true)]
        [InlineData("dalamud://foo/bar/baz/qux/meh/", 5, true)]
        [InlineData("dalamud://foo/bar/baz/qux/meh", 5)]
        [InlineData("dalamud://foo/bar/baz/qux", 4)]
        [InlineData("dalamud://foo/bar/baz", 3)]
        [InlineData("dalamud://foo/bar/", 2)]
        [InlineData("dalamud://foo/bar", 2)]
        public void ExtractsSegments(string uri, int segmentCount, bool finalSegmentEndsWithSlash = false)
        {
            var dalamudUri = DalamudUri.FromUri(uri);
            var segments = dalamudUri.Segments;

            // First segment must always be `/`
            Assert.Equal("/", segments[0]);

            Assert.Equal(segmentCount, segments.Length);

            if (finalSegmentEndsWithSlash)
            {
                Assert.EndsWith("/", segments.Last());
            }
        }
    }
}
