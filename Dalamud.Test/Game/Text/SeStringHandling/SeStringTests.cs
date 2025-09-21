using System;
using System.IO;

using Dalamud.Configuration;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using Xunit;

namespace Dalamud.Test.Game.Text.SeStringHandling
{
    public class SeStringTests
    {
        private class MockConfig : IPluginConfiguration, IEquatable<MockConfig>
        {
            public int Version { get; set; }
            
            public SeString Text { get; init; }

            public bool Equals(MockConfig other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Version == other.Version && Equals(Text.TextValue, other.Text.TextValue);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as MockConfig);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Text);
            }

            public static bool operator ==(MockConfig left, MockConfig right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(MockConfig left, MockConfig right)
            {
                return !Equals(left, right);
            }
        }
        
        // Dalamud#779
        [Fact]
        public void TestConfigSerializable()
        {
            var builder = new SeStringBuilder();
            var seString = builder.AddText("Some text").Build();
            var config = new MockConfig { Text = seString };
            PluginConfigurations.SerializeConfig(config);
        }

        [Fact]
        public void TestConfigDeserializable()
        {
            var builder = new SeStringBuilder();
            var seString = builder.AddText("Some text").Build();
            var config = new MockConfig { Text = seString };

            // This relies on the type information being maintained, which is why we're using these
            // static methods instead of default serialization/deserialization.
            var configSerialized = PluginConfigurations.SerializeConfig(config);
            var configDeserialized = (MockConfig)PluginConfigurations.DeserializeConfig(configSerialized);
            Assert.Equal(config, configDeserialized);
        }

        [Theory]
        [InlineData(49, 209)]
        [InlineData(71, 7)]
        [InlineData(62, 116)]
        public void TestAutoTranslatePayloadReencode(uint group, uint key)
        {
            var payload = new AutoTranslatePayload(group, key);

            Assert.Equal(group, payload.Group);
            Assert.Equal(key, payload.Key);

            var encoded = payload.Encode();
            using var stream = new MemoryStream(encoded);
            using var reader = new BinaryReader(stream);
            var decodedPayload = Payload.Decode(reader) as AutoTranslatePayload;

            Assert.Equal(group, decodedPayload.Group);
            Assert.Equal(key, decodedPayload.Key);

            Assert.Equal(encoded, decodedPayload.Encode());
        }
    }
}
