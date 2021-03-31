using System;
using Lumina;
using Lumina.Data;
using Lumina.Excel;

namespace Dalamud.Data.TransientSheet
{
    [Obsolete("This sheet is transient and will be removed soon.", true)]
    [Sheet( "Completion", columnHash: 0x2e6c55a3 )]
    public class Completion : ExcelRow
    {
        // column defs from Mon, 02 Mar 2020 11:00:20 GMT


        // col: 03 offset: 0000
        public string Text;

        // col: 04 offset: 0004
        public string GroupTitle;

        // col: 02 offset: 0008
        public string LookupTable;

        // col: 00 offset: 000c
        public ushort Group;

        // col: 01 offset: 000e
        public ushort Key;


        public uint RowId { get; set; }
        public uint SubRowId { get; set; }

        public void PopulateData( RowParser parser, GameData lumina, Language language )
        {
            RowId = parser.Row;
            SubRowId = parser.SubRow;

            // col: 3 offset: 0000
            Text = parser.ReadOffset< string >( 0x0 );

            // col: 4 offset: 0004
            GroupTitle = parser.ReadOffset< string >( 0x4 );

            // col: 2 offset: 0008
            LookupTable = parser.ReadOffset< string >( 0x8 );

            // col: 0 offset: 000c
            Group = parser.ReadOffset< ushort >( 0xc );

            // col: 1 offset: 000e
            Key = parser.ReadOffset< ushort >( 0xe );


        }
    }
}
