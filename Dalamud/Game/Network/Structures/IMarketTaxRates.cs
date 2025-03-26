namespace Dalamud.Game.Network.Structures;

/// <summary>
/// An interface that represents the tax rates received by the client when interacting with a retainer vocate.
/// </summary>
public interface IMarketTaxRates
{
    /// <summary>
    /// Gets the category of this ResultDialog packet.
    /// </summary>
    public uint Category { get; }

    /// <summary>
    /// Gets the tax rate in Limsa Lominsa.
    /// </summary>
    public uint LimsaLominsaTax { get; }

    /// <summary>
    /// Gets the tax rate in Gridania.
    /// </summary>
    public uint GridaniaTax { get; }

    /// <summary>
    /// Gets the tax rate in Ul'dah.
    /// </summary>
    public uint UldahTax { get; }

    /// <summary>
    /// Gets the tax rate in Ishgard.
    /// </summary>
    public uint IshgardTax { get; }

    /// <summary>
    /// Gets the tax rate in Kugane.
    /// </summary>
    public uint KuganeTax { get; }

    /// <summary>
    /// Gets the tax rate in the Crystarium.
    /// </summary>
    public uint CrystariumTax { get; }

    /// <summary>
    /// Gets the tax rate in the Crystarium.
    /// </summary>
    public uint SharlayanTax { get; }

    /// <summary>
    /// Gets the tax rate in Tuliyollal.
    /// </summary>
    public uint TuliyollalTax { get; }

    /// <summary>
    /// Gets until when these values are valid.
    /// </summary>
    public DateTime ValidUntil { get; }
}
