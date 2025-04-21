using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Core
{
    public static class BiomeChecker
    {
        public static Dictionary<uint, Biome> Cache = [];

        public static readonly Dictionary<Biome, uint[]> Territories = new()
        {
            [Biome.Desert] = [
                OpenAreas.Amh_Araeng,
                852,856, //atlas peak
                OpenAreas.Urqopacha,
                OpenAreas.Shaaloani,
            ],
            [Biome.Tropical] = [
                1055, //unnamed island
                732, //The Forbidden Land, Eureka Anemos
                1176,1179,1180, //aloalo
                OpenAreas.Kozamauka,
                OpenAreas.Yak_Tel,
                Inns.The_Forard_Cabins,
            ],
            [Biome.Temperate] = [
                827, //The Forbidden Land, Eureka Hydatos
                Trials.the_Jade_Stoa, Trials.the_Jade_Stoa_Extreme,
                1137,1155,1156, //Mount Rokkon
                Raids.Sigmascape_V1_0, Raids.Sigmascape_V1_0_Savage,
                Dungeons.Neverreap,
                961,1014 ,956
                ],
            [Biome.Taiga] = [
                ],
            [Biome.Tundra] = [
                OpenAreas.Garlemald,
                763,//The Forbidden Land, Eureka Pagos
                795, //The Forbidden Land, Eureka Pyros
                905,909, //Great Glacier
                1126,//The Aetherfont
                1147,1148, //The Aetherial Slough
                1010,1012,//Magna Glacies
                1024, //The Nethergate
                1160, //Senatus
                1097, 1119, //Lapis Manalis
                Dungeons.the_Stone_Vigil, Dungeons.the_Stone_Vigil_Hard,
                ],
            [Biome.Wasteland] = [
                147, 410, 483, 402, 459, 818, 932, 959, 960, 1027, 920, 975, 1071, 1072, 1092, 1140, 1141, 1159, 1168, 1169, 1181, 936, 937, 911, 1077, 1162, 1006, 1007, 1081, 1082, 1083, 1084, 903, 907, 945, 949, 847, 881, 885, 916, 978, 1079, 995, 996, 1030, 1153, 1154, 970, 973, 992, 993, 1028, 997, 998, 1029, 1013, 1026, 897, 912, 793, 830, 1174, 507,
                1153,1154,
                Raids.Sigmascape_V2_0, Raids.Sigmascape_V2_0_Savage, Raids.Sigmascape_V3_0, Raids.Sigmascape_V3_0_Savage,
                799,803,
                878,965, //empty
                OpenAreas.Heritage_Found
                ],
            [Biome.Urban_Hot] = [
                 409, 474,  404,   130, 182, 251, 254, 259, 274, 790, 131, 666, 705, 706, 341, 635, 659, 963, 915, 1078, 178, 210, 535, 971,
                 OpenAreas.Tuliyollal,
                ],
            [Biome.Urban] = [
                132, 183, 133, 238, 865, 340, 144, 832, 641, 628, 664, 665, 667, 710, 682, 739, 759, 819, 820, 863, 1061, 844, 735, 828, 736, 919, 882, 896, 917, 928, 838, 841, 884, 987, 1057, 1073, 744, 629, 639, 353, 806, 738, 679, 727, 740, 829, 658, 690, 724, 756, 800, 801, 804, 805, 807, 808, 812, 1122, 388, 389, 390, 391, 417, 506, 589, 590, 591, 1197, 579, 940, 941, 179, 534, 994, 571, 1094, 626, 1142,136,339,250, 717,
                843,504,741,198,177,536,128,129,
                OpenAreas.Solution_Nine,
                OpenAreas.Living_Memory,
            ],
            [Biome.Urban_Cold] = [
                418, 458, 700, 419, 499, 886, 979, 478, 962, 429, 1034, 1060, 439, 428, 1001, 433, 427,
                985,999
                ],
            [Biome.Dungeons] = [
                1085, 1086, 895, 851, 855, 823, 836, 898, 918, 1095, 1096, 674, 761, 762, 623, 714, 1143, 742, 769, 789, 1173, 719, 720, 731, 1172, 171, 172, 1113, 294, 297, 331, 833, 834, 1047, 559, 349, 1038, 142, 162, 360, 460, 373, 1037, 1039, 167, 189, 396, 721, 363, 519, 722, 1069, 1075, 1076, 364, 1067, 167, 189, 396, 721, 355, 380, 704, 387, 1016, 1036, 293, 296, 954, 1046, 561, 562, 563, 564, 565, 570, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 604, 605, 606, 607,
                // Dawntrail Dungeons below
                1193, 1194, 1198, 1199, 1203, 1204, 1208, 1242, 1266,
                ],
            [Biome.Dungeons_2] = [
                794, 558, 733, 913, 950, 951, 991, 1070, 1089, 1091, 777, 1054, 1118, 1178, 1182, 734, 776, 826, 725, 712, 725, 879, 924, 1000, 1123, 658, 690, 724, 756, 800, 801, 804, 805, 807, 808, 812, 1122, 925, 926, 1004, 1005, 1008, 1009, 1087, 1088, 1093, 889, 890, 891, 892, 894, 914, 850, 854, 904, 908, 849, 853, 857, 942, 946, 944, 948, 966, 354, 845, 858, 873, 846, 848, 880, 922, 923, 931, 822, 840, 1151, 1152, 509, 952, 969, 1051, 1115, 976, 986, 770, 771, 772, 773, 774, 775, 780, 782, 783, 784, 785, 778, 779, 786, 810, 811, 824, 825, 616, 660, 1144, 768, 1017, 662, 764, 679, 730, 661, 1145, 403, 689, 1146, 663, 567, 508, 627, 436, 447, 437, 448, 1175, 517, 524, 576, 577, 637, 638, 1110, 430, 743, 513, 1018, 1066, 366, 968, 1043, 1044, 1048, 1052, 1053, 142, 934, 193, 194, 195, 196, 350, 1040, 1114, 356, 381, 357, 382, 358, 383, 160, 510, 281, 359, 241, 242, 243, 244, 245, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551,
                1002, 1003, 1025, 691, 695, 692, 696, 693, 697, 694, 698, 749, 753, 798, 802, 658, 724, 756, 807, 812, 808, 690, 800, 804, 801, 805, 1122,
                // Dawntrail Trial + Raid + Ultimate below
                1195, 1196, 1200, 1201, 1202, 1203, 1270, 1271, 1225, 1226, 1227, 1228, 1229, 1230, 1231, 1232, 1238, 1256, 1257, 1258, 1259, 1260, 1261, 1262, 1263,
                ]
        };
        public static readonly Dictionary<Biome, TerritoryRegion[]> Regions = new()
        {
            [Biome.Desert] = [TerritoryRegion.GyrAbania, TerritoryRegion.Thanalan],
            [Biome.Tropical] = [TerritoryRegion.LaNoscea, TerritoryRegion.Othard, TerritoryRegion.Ilsabard],
            [Biome.Temperate] = [TerritoryRegion.TheBlackShroud, TerritoryRegion.MorDhona, TerritoryRegion.Norvrandt, TerritoryRegion.TheWorldUnsundered,],
            [Biome.Taiga] = [TerritoryRegion.AbalanthiasSpine, TerritoryRegion.Dravania,],
            [Biome.Tundra] = [TerritoryRegion.Coerthas],
            [Biome.Wasteland] = [TerritoryRegion.TheNorthernEmpty, TerritoryRegion.TheHighSeas,],
            [Biome.Urban_Hot] = [TerritoryRegion.TheHighSeas,],
            [Biome.Urban] = [TerritoryRegion.Hingashi],
        };

        public static bool Check(this Biome biome)
        {
            if(Cache.TryGetValue(Svc.ClientState.TerritoryType, out var b))
            {
                return biome == b;
            }
            Cache[Svc.ClientState.TerritoryType] = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType).FindBiome();
            return biome == Cache[Svc.ClientState.TerritoryType];
        }

        public static Biome FindBiome(this TerritoryType t)
        {
            foreach(var x in Territories)
            {
                if(x.Value.Contains(t.RowId)) return x.Key;
            }
            if(((TerritoryIntendedUseEnum)t.TerritoryIntendedUse.RowId).EqualsAny(TerritoryIntendedUseEnum.Housing_Instances)) return Biome.No_biome;
            foreach(var x in Regions)
            {
                if(x.Value.Contains((TerritoryRegion)t.PlaceNameRegion.RowId)) return x.Key;
            }
            return Biome.No_biome;
        }
    }
}
