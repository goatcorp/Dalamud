using System.IO;

namespace Dalamud.Game.Network.Structures;

/// <summary>
/// This class represents the "Result Dialog" packet. This is also used e.g. for reduction results, but we only care about tax rates.
/// We can do that by checking the "Category" field.
/// </summary>
public class MarketTaxRates
{
    private MarketTaxRates()
    {
    }

    /// <summary>
    /// Gets the category of this ResultDialog packet.
    /// </summary>
    public uint Category { get; private set; }

    /// <summary>
    /// Gets the tax rate in Limsa Lominsa.
    /// </summary>
    public uint LimsaLominsaTax { get; private set; }

    /// <summary>
    /// Gets the tax rate in Gridania.
    /// </summary>
    public uint GridaniaTax { get; private set; }

    /// <summary>
    /// Gets the tax rate in Ul'dah.
    /// </summary>
    public uint UldahTax { get; private set; }

    /// <summary>
    /// Gets the tax rate in Ishgard.
    /// </summary>
    public uint IshgardTax { get; private set; }

    /// <summary>
    /// Gets the tax rate in Kugane.
    /// </summary>
    public uint KuganeTax { get; private set; }

    /// <summary>
    /// Gets the tax rate in the Crystarium.
    /// </summary>
    public uint CrystariumTax { get; private set; }

    /// <summary>
    /// Gets the tax rate in the Crystarium.
    /// </summary>
    public uint SharlayanTax { get; private set; }

    /// <summary>
    /// Read a <see cref="MarketTaxRates"/> object from memory.
    /// </summary>
    /// <param name="dataPtr">Address to read.</param>
    /// <returns>A new <see cref="MarketTaxRates"/> object.</returns>
    public static unsafe MarketTaxRates Read(IntPtr dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
        using var reader = new BinaryReader(stream);

        var output = new MarketTaxRates();

        output.Category = reader.ReadUInt32();
        stream.Position += 4;
        output.LimsaLominsaTax = reader.ReadUInt32();
        output.GridaniaTax = reader.ReadUInt32();
        output.UldahTax = reader.ReadUInt32();
        output.IshgardTax = reader.ReadUInt32();
        output.KuganeTax = reader.ReadUInt32();
        output.CrystariumTax = reader.ReadUInt32();
        output.SharlayanTax = reader.ReadUInt32();

        return output;
    }
    
    /// <summary>
    /// Generate a MarketTaxRates wrapper class from information located in a CustomTalk packet.
    /// </summary>
    /// <param name="dataPtr">The pointer to the relevant CustomTalk data.</param>
    /// <returns>Returns a wrapped and ready-to-go MarketTaxRates record.</returns>
    public static unsafe MarketTaxRates ReadFromCustomTalk(IntPtr dataPtr)
    {
        using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
        using var reader = new BinaryReader(stream);

        return new MarketTaxRates
        {
            Category = 0xb0009, // shim
            LimsaLominsaTax = reader.ReadUInt32(),
            GridaniaTax = reader.ReadUInt32(),
            UldahTax = reader.ReadUInt32(),
            IshgardTax = reader.ReadUInt32(),
            KuganeTax = reader.ReadUInt32(),
            CrystariumTax = reader.ReadUInt32(),
            SharlayanTax = reader.ReadUInt32(),
        };
    }
}
