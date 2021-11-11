using System;
using System.IO;

namespace Dalamud.Game.Network.Structures
{
    /// <summary>
    /// This class represents the market tax rates from a game network packet.
    /// </summary>
    public class MarketTaxRates
    {
        private MarketTaxRates()
        {
        }

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
        /// Read a <see cref="MarketTaxRates"/> object from memory.
        /// </summary>
        /// <param name="dataPtr">Address to read.</param>
        /// <returns>A new <see cref="MarketTaxRates"/> object.</returns>
        public static unsafe MarketTaxRates Read(IntPtr dataPtr)
        {
            using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
            using var reader = new BinaryReader(stream);

            var output = new MarketTaxRates();

            stream.Position += 8;
            output.LimsaLominsaTax = reader.ReadUInt32();
            output.GridaniaTax = reader.ReadUInt32();
            output.UldahTax = reader.ReadUInt32();
            output.IshgardTax = reader.ReadUInt32();
            output.KuganeTax = reader.ReadUInt32();
            output.CrystariumTax = reader.ReadUInt32();

            return output;
        }
    }
}
