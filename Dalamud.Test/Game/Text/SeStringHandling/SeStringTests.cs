using Dalamud.Game.Text.SeStringHandling;
using Newtonsoft.Json;
using Xunit;

namespace Dalamud.Test.Game.Text.SeStringHandling
{
    public class SeStringTests
    {
        // Dalamud#779
        [Fact]
        public void TestJsonSerializable()
        {
            var builder = new SeStringBuilder();
            var seString = builder.AddText("Some text").Build();
            JsonConvert.SerializeObject(seString);
        }
    }
}
