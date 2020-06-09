using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel;

namespace Dalamud.Data.TransientSheet
{
    [Sheet("ContentFinderCondition")]
    public class ContentFinderCondition : IExcelRow
    {
        // column defs from Thu, 13 Feb 2020 20:46:12 GMT

        /* offset: 002c col: 0
         *  name: ShortCode
         *  type: 
         */

        /* offset: 0048 col: 1
         *  name: TerritoryType
         *  type: 
         */

        /* offset: 0054 col: 2
         *  name: ContentLinkType
         *  type: 
         */

        /* offset: 004a col: 3
         *  name: Content
         *  type: 
         */

        /* offset: 0061 col: 4
         *  name: PvP
         *  type: 
         */

        /* offset: 0055 col: 5
         *  no SaintCoinach definition found
         */

        /* offset: 0030 col: 6
         *  no SaintCoinach definition found
         */

        /* offset: 0034 col: 7
         *  no SaintCoinach definition found
         */

        /* offset: 0056 col: 8
         *  name: AcceptClassJobCategory
         *  type: 
         */

        /* offset: 0057 col: 9
         *  name: ContentMemberType
         *  type: 
         */

        /* offset: 0058 col: 10
         *  no SaintCoinach definition found
         */

        /* offset: 0059 col: 11
         *  no SaintCoinach definition found
         */

        /* offset: 005a col: 12
         *  no SaintCoinach definition found
         */

        /* offset: 0038 col: 13
         *  name: UnlockQuest
         *  type: 
         */

        /* offset: 004c col: 14
         *  no SaintCoinach definition found
         */

        /* offset: 005b col: 15
         *  name: ClassJobLevel{Required}
         *  type: 
         */

        /* offset: 005c col: 16
         *  name: ClassJobLevel{Sync}
         *  type: 
         */

        /* offset: 004e col: 17
         *  name: ItemLevel{Required}
         *  type: 
         */

        /* offset: 0050 col: 18
         *  name: ItemLevel{Sync}
         *  type: 
         */

        /* offset: 0061 col: 19
         *  name: AllowUndersized
         *  type: 
         */

        /* offset: 0061 col: 20
         *  name: AllowReplacement
         *  type: 
         */

        /* offset: 0061 col: 21
         *  no SaintCoinach definition found
         */

        /* offset: 0061 col: 22
         *  no SaintCoinach definition found
         */

        /* offset: 0061 col: 23
         *  no SaintCoinach definition found
         */

        /* offset: 005d col: 24
         *  no SaintCoinach definition found
         */

        /* offset: 0061 col: 25
         *  no SaintCoinach definition found
         */

        /* offset: 0061 col: 26
         *  name: HighEndDuty
         *  type: 
         */

        /* offset: 0062 col: 27
         *  no SaintCoinach definition found
         */

        /* offset: 0062 col: 28
         *  no SaintCoinach definition found
         */

        /* offset: 0062 col: 29
         *  no SaintCoinach definition found
         */

        /* offset: 0062 col: 30
         *  name: DutyRecorderAllowed
         *  type: 
         */

        /* offset: 0062 col: 31
         *  no SaintCoinach definition found
         */

        /* offset: 0062 col: 32
         *  no SaintCoinach definition found
         */

        /* offset: 0062 col: 33
         *  no SaintCoinach definition found
         */

        /* offset: 0000 col: 34
         *  name: Name
         *  type: 
         */

        /* offset: 005e col: 35
         *  name: ContentType
         *  type: 
         */

        /* offset: 005f col: 36
         *  name: TransientKey
         *  type: 
         */

        /* offset: 003c col: 37
         *  name: Transient
         *  type: 
         */

        /* offset: 0052 col: 38
         *  name: SortKey
         *  type: 
         */

        /* offset: 0040 col: 39
         *  name: Image
         *  type: 
         */

        /* offset: 0044 col: 40
         *  name: Icon
         *  type: 
         */

        /* offset: 0060 col: 41
         *  no SaintCoinach definition found
         */

        /* offset: 0004 col: 42
         *  name: LevelingRoulette
         *  type: 
         */

        /* offset: 0005 col: 43
         *  name: Level50/60Roulette
         *  type: 
         */

        /* offset: 0006 col: 44
         *  name: MSQRoulette
         *  type: 
         */

        /* offset: 0007 col: 45
         *  name: GuildHestRoulette
         *  type: 
         */

        /* offset: 0008 col: 46
         *  name: ExpertRoulette
         *  type: 
         */

        /* offset: 0009 col: 47
         *  name: TrialRoulette
         *  type: 
         */

        /* offset: 000a col: 48
         *  name: DailyFrontlineChallenge
         *  type: 
         */

        /* offset: 000b col: 49
         *  name: Level70Roulette
         *  type: 
         */

        /* offset: 000c col: 50
         *  name: MentorRoulette
         *  type: 
         */

        /* offset: 000d col: 51
         *  no SaintCoinach definition found
         */

        /* offset: 000e col: 52
         *  no SaintCoinach definition found
         */

        /* offset: 000f col: 53
         *  no SaintCoinach definition found
         */

        /* offset: 0010 col: 54
         *  no SaintCoinach definition found
         */

        /* offset: 0011 col: 55
         *  no SaintCoinach definition found
         */

        /* offset: 0012 col: 56
         *  name: AllianceRoulette
         *  type: 
         */

        /* offset: 0013 col: 57
         *  no SaintCoinach definition found
         */

        /* offset: 0014 col: 58
         *  name: NormalRaidRoulette
         *  type: 
         */

        /* offset: 0015 col: 59
         *  no SaintCoinach definition found
         */

        /* offset: 0016 col: 60
         *  no SaintCoinach definition found
         */

        /* offset: 0017 col: 61
         *  no SaintCoinach definition found
         */

        /* offset: 0018 col: 62
         *  no SaintCoinach definition found
         */

        /* offset: 0019 col: 63
         *  no SaintCoinach definition found
         */

        /* offset: 001a col: 64
         *  no SaintCoinach definition found
         */

        /* offset: 001b col: 65
         *  no SaintCoinach definition found
         */

        /* offset: 001c col: 66
         *  no SaintCoinach definition found
         */

        /* offset: 001d col: 67
         *  no SaintCoinach definition found
         */

        /* offset: 001e col: 68
         *  no SaintCoinach definition found
         */

        /* offset: 001f col: 69
         *  no SaintCoinach definition found
         */

        /* offset: 0020 col: 70
         *  no SaintCoinach definition found
         */

        /* offset: 0021 col: 71
         *  no SaintCoinach definition found
         */

        /* offset: 0022 col: 72
         *  no SaintCoinach definition found
         */

        /* offset: 0023 col: 73
         *  no SaintCoinach definition found
         */

        /* offset: 0024 col: 74
         *  no SaintCoinach definition found
         */

        /* offset: 0025 col: 75
         *  no SaintCoinach definition found
         */

        /* offset: 0026 col: 76
         *  no SaintCoinach definition found
         */

        /* offset: 0027 col: 77
         *  no SaintCoinach definition found
         */

        /* offset: 0028 col: 78
         *  no SaintCoinach definition found
         */

        /* offset: 0029 col: 79
         *  no SaintCoinach definition found
         */

        /* offset: 002a col: 80
         *  no SaintCoinach definition found
         */



        // col: 34 offset: 0000
        public string Name;

        // col: 42 offset: 0004
        public bool LevelingRoulette;

        // col: 43 offset: 0005
        public bool Level5060Roulette;

        // col: 44 offset: 0006
        public bool MSQRoulette;

        // col: 45 offset: 0007
        public bool GuildHestRoulette;

        // col: 46 offset: 0008
        public bool ExpertRoulette;

        // col: 47 offset: 0009
        public bool TrialRoulette;

        // col: 48 offset: 000a
        public bool DailyFrontlineChallenge;

        // col: 49 offset: 000b
        public bool Level70Roulette;

        // col: 50 offset: 000c
        public bool MentorRoulette;

        // col: 51 offset: 000d
        public bool unknownd;

        // col: 52 offset: 000e
        public bool unknowne;

        // col: 53 offset: 000f
        public bool unknownf;

        // col: 54 offset: 0010
        public bool unknown10;

        // col: 55 offset: 0011
        public bool unknown11;

        // col: 56 offset: 0012
        public bool AllianceRoulette;

        // col: 57 offset: 0013
        public bool unknown13;

        // col: 58 offset: 0014
        public bool NormalRaidRoulette;

        // col: 59 offset: 0015
        public bool unknown15;

        // col: 60 offset: 0016
        public bool unknown16;

        // col: 61 offset: 0017
        public bool unknown17;

        // col: 62 offset: 0018
        public bool unknown18;

        // col: 63 offset: 0019
        public bool unknown19;

        // col: 64 offset: 001a
        public bool unknown1a;

        // col: 65 offset: 001b
        public bool unknown1b;

        // col: 66 offset: 001c
        public bool unknown1c;

        // col: 67 offset: 001d
        public bool unknown1d;

        // col: 68 offset: 001e
        public bool unknown1e;

        // col: 69 offset: 001f
        public bool unknown1f;

        // col: 70 offset: 0020
        public bool unknown20;

        // col: 71 offset: 0021
        public bool unknown21;

        // col: 72 offset: 0022
        public bool unknown22;

        // col: 73 offset: 0023
        public bool unknown23;

        // col: 74 offset: 0024
        public bool unknown24;

        // col: 75 offset: 0025
        public bool unknown25;

        // col: 76 offset: 0026
        public bool unknown26;

        // col: 77 offset: 0027
        public bool unknown27;

        // col: 78 offset: 0028
        public bool unknown28;

        // col: 79 offset: 0029
        public bool unknown29;

        // col: 80 offset: 002a
        public bool unknown2a;

        // col: 00 offset: 002c
        public string ShortCode;

        // col: 06 offset: 0030
        public uint unknown30;

        // col: 07 offset: 0034
        public uint unknown34;

        // col: 13 offset: 0038
        public uint UnlockQuest;

        // col: 37 offset: 003c
        public uint Transient;

        // col: 39 offset: 0040
        public uint Image;

        // col: 40 offset: 0044
        public uint Icon;

        // col: 01 offset: 0048
        public ushort TerritoryType;

        // col: 03 offset: 004a
        public ushort Content;

        // col: 14 offset: 004c
        public ushort unknown4c;

        // col: 17 offset: 004e
        public ushort ItemLevelRequired;

        // col: 18 offset: 0050
        public ushort ItemLevelSync;

        // col: 38 offset: 0052
        public ushort SortKey;

        // col: 02 offset: 0054
        public byte ContentLinkType;

        // col: 05 offset: 0055
        public byte unknown55;

        // col: 08 offset: 0056
        public byte AcceptClassJobCategory;

        // col: 09 offset: 0057
        public byte ContentMemberType;

        // col: 10 offset: 0058
        public byte unknown58;

        // col: 11 offset: 0059
        public byte unknown59;

        // col: 12 offset: 005a
        public byte unknown5a;

        // col: 15 offset: 005b
        public byte ClassJobLevelRequired;

        // col: 16 offset: 005c
        public byte ClassJobLevelSync;

        // col: 24 offset: 005d
        public byte unknown5d;

        // col: 35 offset: 005e
        public byte ContentType;

        // col: 36 offset: 005f
        public byte TransientKey;

        // col: 41 offset: 0060
        public sbyte unknown60;

        // col: 04 offset: 0061
        private byte packed61;
        public bool PvP => (packed61 & 0x1) == 0x1;
        public bool AllowUndersized => (packed61 & 0x2) == 0x2;
        public bool AllowReplacement => (packed61 & 0x4) == 0x4;
        public bool unknown61_8 => (packed61 & 0x8) == 0x8;
        public bool unknown61_10 => (packed61 & 0x10) == 0x10;
        public bool unknown61_20 => (packed61 & 0x20) == 0x20;
        public bool unknown61_40 => (packed61 & 0x40) == 0x40;
        public bool HighEndDuty => (packed61 & 0x80) == 0x80;

        // col: 27 offset: 0062
        private byte packed62;
        public bool unknown62_1 => (packed62 & 0x1) == 0x1;
        public bool unknown62_2 => (packed62 & 0x2) == 0x2;
        public bool unknown62_4 => (packed62 & 0x4) == 0x4;
        public bool DutyRecorderAllowed => (packed62 & 0x8) == 0x8;
        public bool unknown62_10 => (packed62 & 0x10) == 0x10;
        public bool unknown62_20 => (packed62 & 0x20) == 0x20;
        public bool unknown62_40 => (packed62 & 0x40) == 0x40;


        public uint RowId { get; set; }
        public uint SubRowId { get; set; }

        public void PopulateData(RowParser parser, Lumina.Lumina lumina)
        {
            RowId = parser.Row;
            SubRowId = parser.SubRow;

            // col: 34 offset: 0000
            Name = parser.ReadOffset<string>(0x0);

            // col: 39 offset: 0040
            Image = parser.ReadOffset<uint>(0x40);
        }
    }
}
