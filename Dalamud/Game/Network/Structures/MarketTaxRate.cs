using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures
{
    public class MarketTaxRates
    {
        public uint LimsaLominsaTax;
        public uint GridaniaTax;
        public uint UldahTax;
        public uint IshgardTax;
        public uint KuganeTax;
        public uint CrystariumTax;


        public static unsafe MarketTaxRates Read(IntPtr dataPtr)
        {
            var output = new MarketTaxRates();

            using (var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544))
            {
                using (var reader = new BinaryReader(stream))
                {
                    stream.Position += 8;

                    output.LimsaLominsaTax = reader.ReadUInt32();
                    output.GridaniaTax = reader.ReadUInt32();
                    output.UldahTax = reader.ReadUInt32();
                    output.IshgardTax = reader.ReadUInt32();
                    output.KuganeTax = reader.ReadUInt32();
                    output.CrystariumTax = reader.ReadUInt32();
                }
            }

            return output;
        }
    }
}
