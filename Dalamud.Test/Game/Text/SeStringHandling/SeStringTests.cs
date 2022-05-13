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
        
        [Fact]
        public void TestJsonDeserializable()
        {
            var builder = new SeStringBuilder();
            var seString = builder.AddText("Some text").Build();
            var serialized = JsonConvert.SerializeObject(seString);
            var seStringD = JsonConvert.DeserializeObject<SeString>(serialized);
            Assert.Equal(seString, seStringD);
        }
    }
}
