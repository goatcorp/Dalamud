using System;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;

namespace Dalamud.Data.TransientSheet
{
    [Sheet("Item", columnHash: 0x9f2e970b)]
    public class Item : IExcelRow
    {
        // column defs from Mon, 24 Feb 2020 17:34:06 GMT


        // col: 00 offset: 0000
        public string Singular;

        // col: 02 offset: 0004
        public string Plural;

        // col: 08 offset: 0008
        public string Description;

        // col: 09 offset: 000c
        public string Name;

        // col: 01 offset: 0010
        public sbyte Adjective;

        // col: 03 offset: 0011
        public sbyte PossessivePronoun;

        // col: 04 offset: 0012
        public sbyte StartsWithVowel;

        // col: 05 offset: 0013
        public sbyte unknown13;

        // col: 06 offset: 0014
        public sbyte Pronoun;

        // col: 07 offset: 0015
        public sbyte Article;

        // col: 47 offset: 0018
        public ulong ModelMain;

        // col: 48 offset: 0020
        public ulong ModelSub;

        // col: 51 offset: 0028
        public ushort DamagePhys;

        // col: 52 offset: 002a
        public ushort DamageMag;

        // col: 53 offset: 002c
        public ushort Delayms;

        // col: 55 offset: 002e
        public ushort BlockRate;

        // col: 56 offset: 0030
        public ushort Block;

        // col: 57 offset: 0032
        public ushort DefensePhys;

        // col: 58 offset: 0034
        public ushort DefenseMag;

        // col: 66 offset: 003c
        public short unknown3c;

        // col: 68 offset: 003e
        public short unknown3e;

        // col: 70 offset: 0040
        public short unknown40;

        // col: 80 offset: 0048
        public short unknown48;

        // col: 82 offset: 004a
        public short unknown4a;

        // col: 84 offset: 004c
        public short unknown4c;

        // col: 40 offset: 004e
        public byte LevelEquip;

        // col: 41 offset: 004f
        public byte unknown4f;

        // col: 42 offset: 0050
        public byte EquipRestriction;

        // col: 43 offset: 0051
        public byte ClassJobCategory;

        // col: 44 offset: 0052
        public byte GrandCompany;

        // col: 45 offset: 0053
        public byte ItemSeries;

        // col: 46 offset: 0054
        public byte BaseParamModifier;

        // col: 49 offset: 0055
        public byte ClassJobUse;

        // col: 50 offset: 0056
        public byte unknown56;

        // col: 54 offset: 0057
        public byte unknown57;

        // col: 59 offset: 0058
        public short[] unknown58;

        // col: 65 offset: 005b
        public byte unknown5b;

        // col: 67 offset: 005c
        public byte unknown5c;

        // col: 69 offset: 005d
        public byte unknown5d;

        // col: 71 offset: 005e
        public byte ItemSpecialBonus;

        // col: 72 offset: 005f
        public byte ItemSpecialBonusParam;

        // col: 73 offset: 0060
        public short[] unknown60;

        // col: 79 offset: 0063
        public byte unknown63;

        // col: 81 offset: 0064
        public byte unknown64;

        // col: 83 offset: 0065
        public byte unknown65;

        // col: 85 offset: 0066
        public byte MaterializeType;

        // col: 86 offset: 0067
        public byte MateriaSlotCount;

        // col: 89 offset: 0068
        public byte unknown68;

        // col: 87 offset: 0069
        private byte packed69;
        public bool IsAdvancedMeldingPermitted => (packed69 & 0x1) == 0x1;
        public bool IsPvP => (packed69 & 0x2) == 0x2;
        public bool IsGlamourous => (packed69 & 0x4) == 0x4;

        // col: 14 offset: 0070
        public uint AdditionalData;

        // col: 19 offset: 0074
        public uint StackSize;

        // col: 24 offset: 0078
        public uint PriceMid;

        // col: 25 offset: 007c
        public uint PriceLow;

        // col: 33 offset: 0080
        public int ItemRepair;

        // col: 34 offset: 0084
        public int ItemGlamour;

        // col: 10 offset: 0088
        public ushort Icon;

        // col: 11 offset: 008a
        public ushort LevelItem;

        // col: 18 offset: 008c
        public ushort unknown8c;

        // col: 29 offset: 008e
        public ushort ItemAction;

        // col: 31 offset: 0090
        public ushort Cooldowns;

        // col: 35 offset: 0092
        public ushort Salvage;

        // col: 36 offset: 0094
        public ushort unknown94;

        // col: 39 offset: 0096
        public ushort AetherialReduce;

        // col: 12 offset: 0098
        public byte Rarity;

        // col: 13 offset: 0099
        public byte FilterGroup;

        // col: 15 offset: 009a
        public byte ItemUICategory;

        // col: 16 offset: 009b
        public byte ItemSearchCategory;

        // col: 17 offset: 009c
        public byte EquipSlotCategory;

        // col: 30 offset: 009d
        public byte unknown9d;

        // col: 32 offset: 009e
        public byte ClassJobRepair;

        // col: 20 offset: 009f
        private byte packed9f;
        public bool IsUnique => (packed9f & 0x1) == 0x1;
        public bool IsUntradable => (packed9f & 0x2) == 0x2;
        public bool IsIndisposable => (packed9f & 0x4) == 0x4;
        public bool Lot => (packed9f & 0x8) == 0x8;
        public bool CanBeHq => (packed9f & 0x10) == 0x10;
        public bool IsDyeable => (packed9f & 0x20) == 0x20;
        public bool IsCrestWorthy => (packed9f & 0x40) == 0x40;
        public bool IsCollectable => (packed9f & 0x80) == 0x80;

        // col: 38 offset: 00a0
        private byte packeda0;
        public bool AlwaysCollectable => (packeda0 & 0x1) == 0x1;


        public int RowId { get; set; }
        public int SubRowId { get; set; }

        public void PopulateData(RowParser parser, global::Lumina.Lumina lumina)
        {
            RowId = parser.Row;
            SubRowId = parser.SubRow;

            // col: 0 offset: 0000
            Singular = parser.ReadOffset<string>(0x0);

            // col: 2 offset: 0004
            Plural = parser.ReadOffset<string>(0x4);

            // col: 8 offset: 0008
            Description = parser.ReadOffset<string>(0x8);

            // col: 9 offset: 000c
            Name = parser.ReadOffset<string>(0xc);

            // col: 1 offset: 0010
            Adjective = parser.ReadOffset<sbyte>(0x10);

            // col: 3 offset: 0011
            PossessivePronoun = parser.ReadOffset<sbyte>(0x11);

            // col: 4 offset: 0012
            StartsWithVowel = parser.ReadOffset<sbyte>(0x12);

            // col: 5 offset: 0013
            unknown13 = parser.ReadOffset<sbyte>(0x13);

            // col: 6 offset: 0014
            Pronoun = parser.ReadOffset<sbyte>(0x14);

            // col: 7 offset: 0015
            Article = parser.ReadOffset<sbyte>(0x15);

            // col: 47 offset: 0018
            ModelMain = parser.ReadOffset<ulong>(0x18);

            // col: 48 offset: 0020
            ModelSub = parser.ReadOffset<ulong>(0x20);

            // col: 51 offset: 0028
            DamagePhys = parser.ReadOffset<ushort>(0x28);

            // col: 52 offset: 002a
            DamageMag = parser.ReadOffset<ushort>(0x2a);

            // col: 53 offset: 002c
            Delayms = parser.ReadOffset<ushort>(0x2c);

            // col: 55 offset: 002e
            BlockRate = parser.ReadOffset<ushort>(0x2e);

            // col: 56 offset: 0030
            Block = parser.ReadOffset<ushort>(0x30);

            // col: 57 offset: 0032
            DefensePhys = parser.ReadOffset<ushort>(0x32);

            // col: 58 offset: 0034
            DefenseMag = parser.ReadOffset<ushort>(0x34);

            // col: 66 offset: 003c
            unknown3c = parser.ReadOffset<short>(0x3c);

            // col: 68 offset: 003e
            unknown3e = parser.ReadOffset<short>(0x3e);

            // col: 70 offset: 0040
            unknown40 = parser.ReadOffset<short>(0x40);

            // col: 80 offset: 0048
            unknown48 = parser.ReadOffset<short>(0x48);

            // col: 82 offset: 004a
            unknown4a = parser.ReadOffset<short>(0x4a);

            // col: 84 offset: 004c
            unknown4c = parser.ReadOffset<short>(0x4c);

            // col: 40 offset: 004e
            LevelEquip = parser.ReadOffset<byte>(0x4e);

            // col: 41 offset: 004f
            unknown4f = parser.ReadOffset<byte>(0x4f);

            // col: 42 offset: 0050
            EquipRestriction = parser.ReadOffset<byte>(0x50);

            // col: 43 offset: 0051
            ClassJobCategory = parser.ReadOffset<byte>(0x51);

            // col: 44 offset: 0052
            GrandCompany = parser.ReadOffset<byte>(0x52);

            // col: 45 offset: 0053
            ItemSeries = parser.ReadOffset<byte>(0x53);

            // col: 46 offset: 0054
            BaseParamModifier = parser.ReadOffset<byte>(0x54);

            // col: 49 offset: 0055
            ClassJobUse = parser.ReadOffset<byte>(0x55);

            // col: 50 offset: 0056
            unknown56 = parser.ReadOffset<byte>(0x56);

            // col: 54 offset: 0057
            unknown57 = parser.ReadOffset<byte>(0x57);

            // col: 59 offset: 0058
            unknown58 = new short[6];
            unknown58[0] = parser.ReadOffset<byte>(0x58);
            unknown58[1] = parser.ReadOffset<short>(0x36);
            unknown58[2] = parser.ReadOffset<byte>(0x59);
            unknown58[3] = parser.ReadOffset<short>(0x38);
            unknown58[4] = parser.ReadOffset<byte>(0x5a);
            unknown58[5] = parser.ReadOffset<short>(0x3a);

            // col: 65 offset: 005b
            unknown5b = parser.ReadOffset<byte>(0x5b);

            // col: 67 offset: 005c
            unknown5c = parser.ReadOffset<byte>(0x5c);

            // col: 69 offset: 005d
            unknown5d = parser.ReadOffset<byte>(0x5d);

            // col: 71 offset: 005e
            ItemSpecialBonus = parser.ReadOffset<byte>(0x5e);

            // col: 72 offset: 005f
            ItemSpecialBonusParam = parser.ReadOffset<byte>(0x5f);

            // col: 73 offset: 0060
            unknown60 = new short[6];
            unknown60[0] = parser.ReadOffset<byte>(0x60);
            unknown60[1] = parser.ReadOffset<short>(0x42);
            unknown60[2] = parser.ReadOffset<byte>(0x61);
            unknown60[3] = parser.ReadOffset<short>(0x44);
            unknown60[4] = parser.ReadOffset<byte>(0x62);
            unknown60[5] = parser.ReadOffset<short>(0x46);

            // col: 79 offset: 0063
            unknown63 = parser.ReadOffset<byte>(0x63);

            // col: 81 offset: 0064
            unknown64 = parser.ReadOffset<byte>(0x64);

            // col: 83 offset: 0065
            unknown65 = parser.ReadOffset<byte>(0x65);

            // col: 85 offset: 0066
            MaterializeType = parser.ReadOffset<byte>(0x66);

            // col: 86 offset: 0067
            MateriaSlotCount = parser.ReadOffset<byte>(0x67);

            // col: 89 offset: 0068
            unknown68 = parser.ReadOffset<byte>(0x68);

            // col: 87 offset: 0069
            packed69 = parser.ReadOffset<byte>(0x69, ExcelColumnDataType.UInt8);

            // col: 14 offset: 0070
            AdditionalData = parser.ReadOffset<uint>(0x70);

            // col: 19 offset: 0074
            StackSize = parser.ReadOffset<uint>(0x74);

            // col: 24 offset: 0078
            PriceMid = parser.ReadOffset<uint>(0x78);

            // col: 25 offset: 007c
            PriceLow = parser.ReadOffset<uint>(0x7c);

            // col: 33 offset: 0080
            ItemRepair = parser.ReadOffset<int>(0x80);

            // col: 34 offset: 0084
            ItemGlamour = parser.ReadOffset<int>(0x84);

            // col: 10 offset: 0088
            Icon = parser.ReadOffset<ushort>(0x88);

            // col: 11 offset: 008a
            LevelItem = parser.ReadOffset<ushort>(0x8a);

            // col: 18 offset: 008c
            unknown8c = parser.ReadOffset<ushort>(0x8c);

            // col: 29 offset: 008e
            ItemAction = parser.ReadOffset<ushort>(0x8e);

            // col: 31 offset: 0090
            Cooldowns = parser.ReadOffset<ushort>(0x90);

            // col: 35 offset: 0092
            Salvage = parser.ReadOffset<ushort>(0x92);

            // col: 36 offset: 0094
            unknown94 = parser.ReadOffset<ushort>(0x94);

            // col: 39 offset: 0096
            AetherialReduce = parser.ReadOffset<ushort>(0x96);

            // col: 12 offset: 0098
            Rarity = parser.ReadOffset<byte>(0x98);

            // col: 13 offset: 0099
            FilterGroup = parser.ReadOffset<byte>(0x99);

            // col: 15 offset: 009a
            ItemUICategory = parser.ReadOffset<byte>(0x9a);

            // col: 16 offset: 009b
            ItemSearchCategory = parser.ReadOffset<byte>(0x9b);

            // col: 17 offset: 009c
            EquipSlotCategory = parser.ReadOffset<byte>(0x9c);

            // col: 30 offset: 009d
            unknown9d = parser.ReadOffset<byte>(0x9d);

            // col: 32 offset: 009e
            ClassJobRepair = parser.ReadOffset<byte>(0x9e);

            // col: 20 offset: 009f
            packed9f = parser.ReadOffset<byte>(0x9f, ExcelColumnDataType.UInt8);

            // col: 38 offset: 00a0
            packeda0 = parser.ReadOffset<byte>(0xa0, ExcelColumnDataType.UInt8);


        }
    }
}
