using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Xunit;

namespace Dalamud.Test.Game.Text.SeStringHandling
{
    public class SeStringManagerTests
    {
        [Fact]
        public void TestNewLinePayload()
        {
            var manager = new SeStringManager(null);
            var newLinePayloadBytes = new byte[] {0x02, 0x10, 0x01, 0x03};
            
            var seString = manager.Parse(newLinePayloadBytes);
            
            Assert.True(newLinePayloadBytes.SequenceEqual(seString.Encode()));
        }
    }
}
