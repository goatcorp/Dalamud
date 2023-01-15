using System.Buffers.Binary;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Dalamud.Broker.Game;

internal sealed class SqexArgumentBuilder : ArgumentBuilder
{
    private static readonly char[] ChecksumTable =
    {
        'f', 'X', '1', 'p', 'G', 't', 'd', 'S',
        '5', 'C', 'A', 'P', '4', '_', 'V', 'L'
    };

    private readonly IBufferedCipher mCipher;
    private readonly char mChecksum;

    public SqexArgumentBuilder(uint key)
    {
        this.mCipher = new PaddedBufferedBlockCipher(new BlowfishEngine(), new ZeroBytePadding());
        this.mCipher.Init(true, CreateKeyParameter(key));

        this.mChecksum = GetChecksum(key);
    }

    public override string ToString()
    {
        var argument = base.ToString();

        // 
        // We manually align our 
        // We also take this opportunity to insert a null character at the end.
        var argumentUtf8Length = (Encoding.UTF8.GetByteCount(argument) + 8) & ~0b111;
        var argumentUtf8 = new byte[argumentUtf8Length];
        Encoding.UTF8.GetBytes(argument, argumentUtf8);

        // 
        var encryptedArgument = EncryptBytes(argumentUtf8);

        // Convert bytes into a base64url string.
        var base64Argument = Convert.ToBase64String(encryptedArgument)
                                    .Replace('+', '-')
                                    .Replace('/', '_');

        return $"//**sqex0003{base64Argument}{this.mChecksum}**//";
    }

    private static char GetChecksum(uint key)
    {
        // mask the nibble we're looking for
        var value = key & 0x000F_0000;

        return ChecksumTable[value >> 16];
    }

    private static KeyParameter CreateKeyParameter(uint key)
    {
        var keyStr = $"{key:x08}";
        var keyBytes = Encoding.UTF8.GetBytes(keyStr);

        return new KeyParameter(keyBytes);
    }

    private byte[] EncryptBytes(byte[] input)
    {
        // Again, we do this because FFXIV's implementation of constructing festiel network is unconventional.
        SwapEndian(input);
        var output = this.mCipher.DoFinal(input);
        SwapEndian(output);

        return output;
    }

    private static void SwapEndian(Span<byte> data)
    {
        var dataView = MemoryMarshal.Cast<byte, uint>(data);

        for (var i = 0; i < dataView.Length; i++)
        {
            dataView[i] = BinaryPrimitives.ReverseEndianness(dataView[i]);
        }
    }
}
