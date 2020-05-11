using Lumina.Excel;

namespace Dalamud.Data.TransientSheet
{
    [Sheet( "PetMirage", columnHash: 0x720608f1 )]
    public class PetMirage : IExcelRow
    {
        // column defs from Sun, 26 Apr 2020 15:17:06 GMT


        // col: 02 offset: 0000
        public string Name;

        // col: 03 offset: 0004
        public ushort unknown4;

        // col: 33 offset: 0006
        public ushort unknown6;

        // col: 48 offset: 0008
        public ushort unknown8;

        // col: 18 offset: 000a
        public byte unknowna;

        // col: 04 offset: 000c
        public ushort unknownc;

        // col: 34 offset: 000e
        public ushort unknowne;

        // col: 49 offset: 0010
        public ushort unknown10;

        // col: 19 offset: 0012
        public byte unknown12;

        // col: 05 offset: 0014
        public ushort unknown14;

        // col: 35 offset: 0016
        public ushort unknown16;

        // col: 50 offset: 0018
        public ushort unknown18;

        // col: 20 offset: 001a
        public byte unknown1a;

        // col: 06 offset: 001c
        public ushort unknown1c;

        // col: 36 offset: 001e
        public ushort unknown1e;

        // col: 51 offset: 0020
        public ushort unknown20;

        // col: 21 offset: 0022
        public byte unknown22;

        // col: 07 offset: 0024
        public ushort unknown24;

        // col: 37 offset: 0026
        public ushort unknown26;

        // col: 52 offset: 0028
        public ushort unknown28;

        // col: 22 offset: 002a
        public byte unknown2a;

        // col: 08 offset: 002c
        public ushort unknown2c;

        // col: 38 offset: 002e
        public ushort unknown2e;

        // col: 53 offset: 0030
        public ushort unknown30;

        // col: 23 offset: 0032
        public byte unknown32;

        // col: 09 offset: 0034
        public ushort unknown34;

        // col: 39 offset: 0036
        public ushort unknown36;

        // col: 54 offset: 0038
        public ushort unknown38;

        // col: 24 offset: 003a
        public byte unknown3a;

        // col: 10 offset: 003c
        public ushort unknown3c;

        // col: 40 offset: 003e
        public ushort unknown3e;

        // col: 55 offset: 0040
        public ushort unknown40;

        // col: 25 offset: 0042
        public byte unknown42;

        // col: 11 offset: 0044
        public ushort unknown44;

        // col: 41 offset: 0046
        public ushort unknown46;

        // col: 56 offset: 0048
        public ushort unknown48;

        // col: 26 offset: 004a
        public byte unknown4a;

        // col: 12 offset: 004c
        public ushort unknown4c;

        // col: 42 offset: 004e
        public ushort unknown4e;

        // col: 57 offset: 0050
        public ushort unknown50;

        // col: 27 offset: 0052
        public byte unknown52;

        // col: 13 offset: 0054
        public ushort unknown54;

        // col: 43 offset: 0056
        public ushort unknown56;

        // col: 58 offset: 0058
        public ushort unknown58;

        // col: 28 offset: 005a
        public byte unknown5a;

        // col: 14 offset: 005c
        public ushort unknown5c;

        // col: 44 offset: 005e
        public ushort unknown5e;

        // col: 59 offset: 0060
        public ushort unknown60;

        // col: 29 offset: 0062
        public byte unknown62;

        // col: 15 offset: 0064
        public ushort unknown64;

        // col: 45 offset: 0066
        public ushort unknown66;

        // col: 60 offset: 0068
        public ushort unknown68;

        // col: 30 offset: 006a
        public byte unknown6a;

        // col: 16 offset: 006c
        public ushort unknown6c;

        // col: 46 offset: 006e
        public ushort unknown6e;

        // col: 61 offset: 0070
        public ushort unknown70;

        // col: 31 offset: 0072
        public byte unknown72;

        // col: 17 offset: 0074
        public ushort unknown74;

        // col: 47 offset: 0076
        public ushort unknown76;

        // col: 62 offset: 0078
        public ushort unknown78;

        // col: 32 offset: 007a
        public byte unknown7a;

        // col: 00 offset: 007c
        public float unknown7c;

        // col: 01 offset: 0080
        public ushort unknown80;


        public int RowId { get; set; }
        public int SubRowId { get; set; }

        public void PopulateData( RowParser parser, Lumina.Lumina lumina )
        {
            RowId = parser.Row;
            SubRowId = parser.SubRow;

            // col: 2 offset: 0000
            Name = parser.ReadOffset< string >( 0x0 );

            // col: 3 offset: 0004
            unknown4 = parser.ReadOffset< ushort >( 0x4 );

            // col: 33 offset: 0006
            unknown6 = parser.ReadOffset< ushort >( 0x6 );

            // col: 48 offset: 0008
            unknown8 = parser.ReadOffset< ushort >( 0x8 );

            // col: 18 offset: 000a
            unknowna = parser.ReadOffset< byte >( 0xa );

            // col: 4 offset: 000c
            unknownc = parser.ReadOffset< ushort >( 0xc );

            // col: 34 offset: 000e
            unknowne = parser.ReadOffset< ushort >( 0xe );

            // col: 49 offset: 0010
            unknown10 = parser.ReadOffset< ushort >( 0x10 );

            // col: 19 offset: 0012
            unknown12 = parser.ReadOffset< byte >( 0x12 );

            // col: 5 offset: 0014
            unknown14 = parser.ReadOffset< ushort >( 0x14 );

            // col: 35 offset: 0016
            unknown16 = parser.ReadOffset< ushort >( 0x16 );

            // col: 50 offset: 0018
            unknown18 = parser.ReadOffset< ushort >( 0x18 );

            // col: 20 offset: 001a
            unknown1a = parser.ReadOffset< byte >( 0x1a );

            // col: 6 offset: 001c
            unknown1c = parser.ReadOffset< ushort >( 0x1c );

            // col: 36 offset: 001e
            unknown1e = parser.ReadOffset< ushort >( 0x1e );

            // col: 51 offset: 0020
            unknown20 = parser.ReadOffset< ushort >( 0x20 );

            // col: 21 offset: 0022
            unknown22 = parser.ReadOffset< byte >( 0x22 );

            // col: 7 offset: 0024
            unknown24 = parser.ReadOffset< ushort >( 0x24 );

            // col: 37 offset: 0026
            unknown26 = parser.ReadOffset< ushort >( 0x26 );

            // col: 52 offset: 0028
            unknown28 = parser.ReadOffset< ushort >( 0x28 );

            // col: 22 offset: 002a
            unknown2a = parser.ReadOffset< byte >( 0x2a );

            // col: 8 offset: 002c
            unknown2c = parser.ReadOffset< ushort >( 0x2c );

            // col: 38 offset: 002e
            unknown2e = parser.ReadOffset< ushort >( 0x2e );

            // col: 53 offset: 0030
            unknown30 = parser.ReadOffset< ushort >( 0x30 );

            // col: 23 offset: 0032
            unknown32 = parser.ReadOffset< byte >( 0x32 );

            // col: 9 offset: 0034
            unknown34 = parser.ReadOffset< ushort >( 0x34 );

            // col: 39 offset: 0036
            unknown36 = parser.ReadOffset< ushort >( 0x36 );

            // col: 54 offset: 0038
            unknown38 = parser.ReadOffset< ushort >( 0x38 );

            // col: 24 offset: 003a
            unknown3a = parser.ReadOffset< byte >( 0x3a );

            // col: 10 offset: 003c
            unknown3c = parser.ReadOffset< ushort >( 0x3c );

            // col: 40 offset: 003e
            unknown3e = parser.ReadOffset< ushort >( 0x3e );

            // col: 55 offset: 0040
            unknown40 = parser.ReadOffset< ushort >( 0x40 );

            // col: 25 offset: 0042
            unknown42 = parser.ReadOffset< byte >( 0x42 );

            // col: 11 offset: 0044
            unknown44 = parser.ReadOffset< ushort >( 0x44 );

            // col: 41 offset: 0046
            unknown46 = parser.ReadOffset< ushort >( 0x46 );

            // col: 56 offset: 0048
            unknown48 = parser.ReadOffset< ushort >( 0x48 );

            // col: 26 offset: 004a
            unknown4a = parser.ReadOffset< byte >( 0x4a );

            // col: 12 offset: 004c
            unknown4c = parser.ReadOffset< ushort >( 0x4c );

            // col: 42 offset: 004e
            unknown4e = parser.ReadOffset< ushort >( 0x4e );

            // col: 57 offset: 0050
            unknown50 = parser.ReadOffset< ushort >( 0x50 );

            // col: 27 offset: 0052
            unknown52 = parser.ReadOffset< byte >( 0x52 );

            // col: 13 offset: 0054
            unknown54 = parser.ReadOffset< ushort >( 0x54 );

            // col: 43 offset: 0056
            unknown56 = parser.ReadOffset< ushort >( 0x56 );

            // col: 58 offset: 0058
            unknown58 = parser.ReadOffset< ushort >( 0x58 );

            // col: 28 offset: 005a
            unknown5a = parser.ReadOffset< byte >( 0x5a );

            // col: 14 offset: 005c
            unknown5c = parser.ReadOffset< ushort >( 0x5c );

            // col: 44 offset: 005e
            unknown5e = parser.ReadOffset< ushort >( 0x5e );

            // col: 59 offset: 0060
            unknown60 = parser.ReadOffset< ushort >( 0x60 );

            // col: 29 offset: 0062
            unknown62 = parser.ReadOffset< byte >( 0x62 );

            // col: 15 offset: 0064
            unknown64 = parser.ReadOffset< ushort >( 0x64 );

            // col: 45 offset: 0066
            unknown66 = parser.ReadOffset< ushort >( 0x66 );

            // col: 60 offset: 0068
            unknown68 = parser.ReadOffset< ushort >( 0x68 );

            // col: 30 offset: 006a
            unknown6a = parser.ReadOffset< byte >( 0x6a );

            // col: 16 offset: 006c
            unknown6c = parser.ReadOffset< ushort >( 0x6c );

            // col: 46 offset: 006e
            unknown6e = parser.ReadOffset< ushort >( 0x6e );

            // col: 61 offset: 0070
            unknown70 = parser.ReadOffset< ushort >( 0x70 );

            // col: 31 offset: 0072
            unknown72 = parser.ReadOffset< byte >( 0x72 );

            // col: 17 offset: 0074
            unknown74 = parser.ReadOffset< ushort >( 0x74 );

            // col: 47 offset: 0076
            unknown76 = parser.ReadOffset< ushort >( 0x76 );

            // col: 62 offset: 0078
            unknown78 = parser.ReadOffset< ushort >( 0x78 );

            // col: 32 offset: 007a
            unknown7a = parser.ReadOffset< byte >( 0x7a );

            // col: 0 offset: 007c
            unknown7c = parser.ReadOffset< float >( 0x7c );

            // col: 1 offset: 0080
            unknown80 = parser.ReadOffset< ushort >( 0x80 );


        }
    }
}
