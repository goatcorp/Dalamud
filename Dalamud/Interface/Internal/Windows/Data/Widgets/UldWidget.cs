using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Data;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Uld;

using static Lumina.Data.Parsing.Uld.Keyframes;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Displays the full data of the selected ULD element.
/// </summary>
internal class UldWidget : IDataWindowWidget
{
    // static generated from ResLogger2 data
    private static readonly Dictionary<string, (uint FileHash, uint FolderHash)> UldHashLookup = new() {
        { "achievement", (871534932, 2819375915) },
        { "achievementinfo", (544099056, 2819375915) },
        { "achievementsetting", (3094324132, 2819375915) },
        { "actionbar", (2487433129, 2819375915) },
        { "actionbarcombo", (1334866850, 2819375915) },
        { "actionbarcustom", (4276620601, 2819375915) },
        { "actionbarhorizontal", (197901317, 2819375915) },
        { "actionbarvertical", (259710333, 2819375915) },
        { "actioncontents", (2747778885, 2819375915) },
        { "actioncross", (3019956219, 2819375915) },
        { "actioncross_ps3", (3230628446, 2819375915) },
        { "actioncrosseditor", (2944963117, 2819375915) },
        { "actiondetail", (3702893265, 2819375915) },
        { "actiondoublecrossleft", (143162848, 2819375915) },
        { "actiondoublecrossright", (3197290370, 2819375915) },
        { "actionmenu", (886795755, 2819375915) },
        { "actionmenuactionsetting", (194067308, 2819375915) },
        { "actionmenureplacelist", (101152187, 2819375915) },
        { "adventurenotebook", (1400589365, 2819375915) },
        { "adventurenotice", (3556182905, 2819375915) },
        { "aethergauge", (332097323, 2819375915) },
        { "alarm", (1315135609, 2819375915) },
        { "alarmsetting", (3187165093, 2819375915) },
        { "alliance", (3744608001, 2819375915) },
        { "alliance48", (529184308, 2819375915) },
        { "alliancememberlist", (2993922469, 2819375915) },
        { "aozactivesetinputstring", (3095638073, 2819375915) },
        { "aozactivesetlist", (3719905058, 2819375915) },
        { "aozbonuslist", (3435700604, 2819375915) },
        { "aozbriefing", (1345071035, 2819375915) },
        { "aozcontentsresult", (528836007, 2819375915) },
        { "aoznotebook", (186838218, 2819375915) },
        { "aoznotebookfiltersettings", (1149295185, 2819375915) },
        { "aquariumfishlist", (416304855, 2819375915) },
        { "aquariumsetting", (1715355897, 2819375915) },
        { "archiveitem", (4243436021, 2819375915) },
        { "areamap", (3161511113, 2819375915) },
        { "areatext", (1722969793, 2819375915) },
        { "armouryboard", (3312407139, 2819375915) },
        { "armourynotebook", (38209433, 2819375915) },
        { "bagstatus", (2494986458, 2819375915) },
        { "bagstatus_ps3", (2378682390, 2819375915) },
        { "balloonmessage", (2590259324, 2819375915) },
        { "bank", (1754143531, 2819375915) },
        { "bannerconfirm", (4137655574, 2819375915) },
        { "bannercontents", (3898787058, 2819375915) },
        { "bannercontentssetting", (3758409020, 2819375915) },
        { "bannereditor", (4190032736, 2819375915) },
        { "bannergearsetlink", (1322080778, 2819375915) },
        { "bannerlist", (856071894, 2819375915) },
        { "bannermip", (3050720720, 2819375915) },
        { "bannermipsetting", (1572458802, 2819375915) },
        { "bannerupdateview", (4187039755, 2819375915) },
        { "basketball", (663328596, 2819375915) },
        { "battletalk", (558225298, 2819375915) },
        { "beasttribesupplylist", (867401907, 2819375915) },
        { "beginnerchannelinvitedialogue", (1493539873, 2819375915) },
        { "beginnerchanneljoinlist", (2977397123, 2819375915) },
        { "beginnerchannelkick", (2126884080, 2819375915) },
        { "beginnersroomcompletedtraining", (3919695690, 2819375915) },
        { "beginnersroommainwindow", (4010256647, 2819375915) },
        { "blacklist", (1326344429, 2819375915) },
        { "blacklistinputmemo", (3067426616, 2819375915) },
        { "botanistgame", (1114236746, 2819375915) },
        { "breedcoupling", (325529331, 2819375915) },
        { "breednamelist", (2509782574, 2819375915) },
        { "breednaming", (3555705662, 2819375915) },
        { "breedtraining", (1652978057, 2819375915) },
        { "buddy", (114133471, 2819375915) },
        { "buddyaction", (4013333767, 2819375915) },
        { "buddyappearance", (1296822192, 2819375915) },
        { "buddyequiplist", (4231257325, 2819375915) },
        { "buddyinspect", (831728365, 2819375915) },
        { "buddyinventory", (10158151, 2819375915) },
        { "buddyskill", (3441226318, 2819375915) },
        { "cabinetstore", (422433717, 2819375915) },
        { "cabinetwithdraw", (775356483, 2819375915) },
        { "camerasettings", (4134075780, 2819375915) },
        { "cardtripletriad", (2618256002, 2819375915) },
        { "castbar", (1334603998, 2819375915) },
        { "casting", (350064131, 2819375915) },
        { "character", (1228542427, 2819375915) },
        { "characterbestequipment", (3930381176, 2819375915) },
        { "characterbonus", (3141377564, 2819375915) },
        { "charactercard", (1334652868, 2819375915) },
        { "charactercardclassjobsetting", (1918550923, 2819375915) },
        { "charactercarddesignsetting", (3143154900, 2819375915) },
        { "charactercardpermissionsetting", (569242700, 2819375915) },
        { "charactercardplaystylesetting", (2061379422, 2819375915) },
        { "charactercardprofilesetting", (2642742410, 2819375915) },
        { "characterclass", (47717960, 2819375915) },
        { "charactercurrency", (2328709644, 2819375915) },
        { "characterglassselector", (2362896720, 2819375915) },
        { "characterinspect", (227172086, 2819375915) },
        { "characternote", (1871612901, 2819375915) },
        { "characterprofile", (2129996420, 2819375915) },
        { "characterrepute", (1892691787, 2819375915) },
        { "charactersalvage", (932234532, 2819375915) },
        { "characterstatus", (372480449, 2819375915) },
        { "charactertitle", (2858376849, 2819375915) },
        { "charactertolerance", (1867545158, 2819375915) },
        { "charamake_bgselector", (529505276, 2819375915) },
        { "charamake_birthday", (46582413, 2819375915) },
        { "charamake_charname", (2716567195, 2819375915) },
        { "charamake_city", (2399043223, 2819375915) },
        { "charamake_classselector", (2853722201, 2819375915) },
        { "charamake_dataexport", (2241200495, 2819375915) },
        { "charamake_dataimport", (2046682847, 2819375915) },
        { "charamake_dataimportdialog", (1134991636, 2819375915) },
        { "charamake_datainputstring", (2403316509, 2819375915) },
        { "charamake_feature", (287246135, 2819375915) },
        { "charamake_feature_color_eye", (1663908122, 2819375915) },
        { "charamake_feature_color_facepaint", (808309359, 2819375915) },
        { "charamake_feature_color_hair", (904515231, 2819375915) },
        { "charamake_feature_color_l", (243929273, 2819375915) },
        { "charamake_feature_color_lip", (1566795424, 2819375915) },
        { "charamake_feature_listicon_facepaint", (1745363265, 2819375915) },
        { "charamake_feature_listicon_facetype", (123865114, 2819375915) },
        { "charamake_feature_listicon_feature", (1163355883, 2819375915) },
        { "charamake_feature_listicon_hair", (3231554388, 2819375915) },
        { "charamake_feature_listicon_tail", (878286547, 2819375915) },
        { "charamake_feature_listicon_tatoo", (1998396340, 2819375915) },
        { "charamake_feature_option12", (3162738817, 2819375915) },
        { "charamake_feature_option2", (1297017177, 2819375915) },
        { "charamake_feature_option3", (1882112233, 2819375915) },
        { "charamake_feature_option4", (3255704825, 2819375915) },
        { "charamake_feature_option5", (4285412681, 2819375915) },
        { "charamake_feature_option6", (3100528537, 2819375915) },
        { "charamake_feature_return", (944038849, 2819375915) },
        { "charamake_feature_slider", (3874604420, 2819375915) },
        { "charamake_guardian", (1706203117, 2819375915) },
        { "charamake_help", (4137806386, 2819375915) },
        { "charamake_notice", (349173447, 2819375915) },
        { "charamake_pose", (2802792286, 2819375915) },
        { "charamake_progress", (1737298923, 2819375915) },
        { "charamake_progressindicator", (3461758426, 2819375915) },
        { "charamake_progresssub", (2405901367, 2819375915) },
        { "charamake_return", (2740390826, 2819375915) },
        { "charamake_selectyesno", (3590021045, 2819375915) },
        { "charamake_shadow", (2593180480, 2819375915) },
        { "charamake_title", (1468076612, 2819375915) },
        { "charamake_worldserver", (152730444, 2819375915) },
        { "charaselect_detail", (164512685, 2819375915) },
        { "charaselect_dkt_progress", (3234036657, 2819375915) },
        { "charaselect_dkt_select", (4047369376, 2819375915) },
        { "charaselect_dkt_yesno", (320575091, 2819375915) },
        { "charaselect_info", (3718800368, 2819375915) },
        { "charaselect_info_progress", (1152008191, 2819375915) },
        { "charaselect_legacy", (3786804741, 2819375915) },
        { "charaselect_listmenu", (1292221024, 2819375915) },
        { "charaselect_nameinput", (3615154426, 2819375915) },
        { "charaselect_remain", (977980258, 2819375915) },
        { "charaselect_return", (2738793447, 2819375915) },
        { "charaselect_shadow", (2600037133, 2819375915) },
        { "charaselect_title", (1042374468, 2819375915) },
        { "charaselect_warning", (2843597990, 2819375915) },
        { "charaselect_wkt_info", (3402963482, 2819375915) },
        { "charaselect_wkt_progress", (254603741, 2819375915) },
        { "charaselect_wkt_yesno", (450255975, 2819375915) },
        { "charaselect_worldserver", (2736000542, 2819375915) },
        { "chatconfig", (1990203436, 2819375915) },
        { "chatlog", (4086969148, 2819375915) },
        { "chatlogpanel", (2591720249, 2819375915) },
        { "checkboxdialogue", (3663759364, 2819375915) },
        { "circleblacklist", (3805561258, 2819375915) },
        { "circleboardedit", (1581968715, 2819375915) },
        { "circlebook", (3804668030, 2819375915) },
        { "circlebooksetting", (3603812804, 2819375915) },
        { "circlefinder", (1604862581, 2819375915) },
        { "circlefinderhighlight", (924872399, 2819375915) },
        { "circlefindersearch", (1604357812, 2819375915) },
        { "circlefindersetting", (1180459228, 2819375915) },
        { "circleinputstring", (646723306, 2819375915) },
        { "circleinvite", (503424451, 2819375915) },
        { "circlelist", (37242309, 2819375915) },
        { "circlemembergroupedit", (1420010561, 2819375915) },
        { "circlequestionnaire", (2791810456, 2819375915) },
        { "colorantcoloringselector", (408644185, 2819375915) },
        { "colorantequipmentselector", (412816911, 2819375915) },
        { "colosseumrecord", (1683977475, 2819375915) },
        { "companycraftdelivery", (2635188554, 2819375915) },
        { "companycraftrecipe", (3080455300, 2819375915) },
        { "concentration", (2734766554, 2819375915) },
        { "config", (500280138, 2819375915) },
        { "configbackupcharacterdownloadselect", (2984699856, 2819375915) },
        { "configbackupcharacterload", (2628257579, 2819375915) },
        { "configbackupcharactermainmenu", (3824537490, 2819375915) },
        { "configbackupcharacterrestore", (1628868436, 2819375915) },
        { "configbackupsystemload", (1864360765, 2819375915) },
        { "configbackupsystemmainmenu", (3076269172, 2819375915) },
        { "configbackupsystemrestore", (380256418, 2819375915) },
        { "configcharacter", (3227898373, 2819375915) },
        { "configcharacter_ps3", (3775701598, 2819375915) },
        { "configcharacter_ps4", (1395424846, 2819375915) },
        { "configcharacterchatlog", (1741672299, 2819375915) },
        { "configcharacterchatlog_ps3", (844197989, 2819375915) },
        { "configcharacterchatlogdetail", (1351042245, 2819375915) },
        { "configcharacterchatlogdetail_ps4", (1085822496, 2819375915) },
        { "configcharacterchatloggeneral", (3113352889, 2819375915) },
        { "configcharacterchatloggeneral_ps3", (3740664281, 2819375915) },
        { "configcharacterchatloggeneral_ps4", (1826020809, 2819375915) },
        { "configcharacterchatlognamesetting", (4145984995, 2819375915) },
        { "configcharacterchatlogringtone", (2064990183, 2819375915) },
        { "configcharacterchatlogringtone_ps3", (1338801132, 2819375915) },
        { "configcharacterchatlogringtone_ps4", (4260145148, 2819375915) },
        { "configcharacterhotbar", (2307985499, 2819375915) },
        { "configcharacterhotbar_ps3", (656754028, 2819375915) },
        { "configcharacterhotbarcommon", (2831765301, 2819375915) },
        { "configcharacterhotbarcommon_ps3", (2771872723, 2819375915) },
        { "configcharacterhotbarcommon_ps4", (387430339, 2819375915) },
        { "configcharacterhotbardisplay", (921850167, 2819375915) },
        { "configcharacterhotbardisplay_ps4", (2158207049, 2819375915) },
        { "configcharacterhotbarxhb", (3361378554, 2819375915) },
        { "configcharacterhotbarxhb_ps3", (437411599, 2819375915) },
        { "configcharacterhotbarxhb_ps4", (2821882655, 2819375915) },
        { "configcharacterhotbarxhbcustom", (4265802535, 2819375915) },
        { "configcharacterhotbarxhbcustom_ps3", (3339654986, 2819375915) },
        { "configcharacterhotbarxhbcustom_ps4", (1966066522, 2819375915) },
        { "configcharacterhud", (3132254443, 2819375915) },
        { "configcharacterhud_ps3", (1640768617, 2819375915) },
        { "configcharacterhudgeneral", (3772936231, 2819375915) },
        { "configcharacterhudgeneral_ps3", (2120704673, 2819375915) },
        { "configcharacterhudgeneral_ps4", (3427253937, 2819375915) },
        { "configcharacterhudhud", (525571198, 2819375915) },
        { "configcharacterhudhud_ps3", (3059034753, 2819375915) },
        { "configcharacterhudhud_ps4", (74839697, 2819375915) },
        { "configcharacterhudpartylist", (1162103477, 2819375915) },
        { "configcharacterhudpartylist_ps3", (506685376, 2819375915) },
        { "configcharacterhudpartylist_ps4", (2886974416, 2819375915) },
        { "configcharacterhudpartylistrolesortsetting", (2823096115, 2819375915) },
        { "configcharacteritem", (72174747, 2819375915) },
        { "configcharacteritem_ps3", (2320107824, 2819375915) },
        { "configcharacteritem_ps4", (946525472, 2819375915) },
        { "configcharacternameplate", (1343344584, 2819375915) },
        { "configcharacternameplate_ps3", (3398011664, 2819375915) },
        { "configcharacternameplategeneral", (2498442858, 2819375915) },
        { "configcharacternameplategeneral_ps3", (3852737887, 2819375915) },
        { "configcharacternameplategeneral_ps4", (1468318031, 2819375915) },
        { "configcharacternameplatemyself", (2058757261, 2819375915) },
        { "configcharacternameplatemyself_ps3", (4178182054, 2819375915) },
        { "configcharacternameplatemyself_ps4", (1260987318, 2819375915) },
        { "configcharacternameplatenpc", (184339656, 2819375915) },
        { "configcharacternameplatenpc_ps3", (2829123586, 2819375915) },
        { "configcharacternameplatenpc_ps4", (444718098, 2819375915) },
        { "configcharacternameplateothers", (1057047939, 2819375915) },
        { "configcharacternameplateothers_ps3", (4260472034, 2819375915) },
        { "configcharacternameplateothers_ps4", (1339128050, 2819375915) },
        { "configcharacternameplatepvp", (3422823700, 2819375915) },
        { "configcharacteroperation", (1547713928, 2819375915) },
        { "configcharacteroperation_ps3", (1305171117, 2819375915) },
        { "configcharacteroperationcharacter", (838918396, 2819375915) },
        { "configcharacteroperationcharacter_ps3", (1991558751, 2819375915) },
        { "configcharacteroperationcharacter_ps4", (3298040399, 2819375915) },
        { "configcharacteroperationcircle", (1734509001, 2819375915) },
        { "configcharacteroperationcircle_ps3", (3030303621, 2819375915) },
        { "configcharacteroperationcircle_ps4", (113123221, 2819375915) },
        { "configcharacteroperationgeneral", (3340739266, 2819375915) },
        { "configcharacteroperationgeneral_ps3", (366155886, 2819375915) },
        { "configcharacteroperationgeneral_ps4", (2817770622, 2819375915) },
        { "configcharacteroperationmouse", (3273701065, 2819375915) },
        { "configcharacteroperationmouse_ps4", (879666934, 2819375915) },
        { "configcharacteroperationtarget", (2533913257, 2819375915) },
        { "configcharacteroperationtarget_ps3", (446007358, 2819375915) },
        { "configcharacteroperationtarget_ps4", (2830455854, 2819375915) },
        { "configkeybind", (4109200768, 2819375915) },
        { "configkeybind_ps3", (661109644, 2819375915) },
        { "configkeybind_ps4", (2504484764, 2819375915) },
        { "configlog", (3794233567, 2819375915) },
        { "configlogcolor", (508465132, 2819375915) },
        { "configlogfilter", (931509722, 2819375915) },
        { "configlogfilterinputstring", (1638401713, 2819375915) },
        { "configpadcalibration", (1036564745, 2819375915) },
        { "configpadcustomize", (352440154, 2819375915) },
        { "configpadcustomize_ps3", (2444494193, 2819375915) },
        { "configpadcustomize_ps4", (596957537, 2819375915) },
        { "configsystem", (3814913726, 2819375915) },
        { "configsystem_ps3", (4179452492, 2819375915) },
        { "configsystem_ps4", (1262337628, 2819375915) },
        { "contactlist", (4252276372, 2819375915) },
        { "contentgauge", (720487321, 2819375915) },
        { "contentmemberlist", (740648638, 2819375915) },
        { "contentsfinder", (3573578694, 2819375915) },
        { "contentsfinderconfirm", (906465654, 2819375915) },
        { "contentsfindermenu", (3170870677, 2819375915) },
        { "contentsfinderready", (3400308951, 2819375915) },
        { "contentsfindersetting", (3053969804, 2819375915) },
        { "contentsfinderstatus", (291946041, 2819375915) },
        { "contentsfindersupply", (741012526, 2819375915) },
        { "contentsinfo", (3960326241, 2819375915) },
        { "contentsinfodetail", (1914202463, 2819375915) },
        { "contentsnotebook", (726624389, 2819375915) },
        { "contentsreplayplayer", (314995455, 2819375915) },
        { "contentsreplayreadycheck", (145967726, 2819375915) },
        { "contentsreplayreadycheckalliance", (1712154164, 2819375915) },
        { "contentsreplaysetting", (1579146082, 2819375915) },
        { "contexticonmenu", (3751924152, 2819375915) },
        { "contextmenu", (465685782, 2819375915) },
        { "contextmenutitle", (3863271229, 2819375915) },
        { "convertimage", (3804059358, 2819375915) },
        { "convertimage_gcarmy_face", (489566107, 2819375915) },
        { "countdownsettingdialog", (4253240207, 2819375915) },
        { "creditcast", (3071315185, 2819375915) },
        { "creditcast2", (3806369680, 2819375915) },
        { "creditend", (3155429850, 2819375915) },
        { "creditend2", (1314351701, 2819375915) },
        { "creditplayer", (4092058729, 2819375915) },
        { "creditstaff", (163032530, 2819375915) },
        { "crossworldlinkshell", (1367107025, 2819375915) },
        { "cruisingcreel", (487316271, 2819375915) },
        { "cruisinginfo", (2239675332, 2819375915) },
        { "cruisingresult", (2304098289, 2819375915) },
        { "cruisingtimetable", (515805210, 2819375915) },
        { "currency", (3491385950, 2819375915) },
        { "currencysetting", (1460531768, 2819375915) },
        { "cursor", (1150553927, 2819375915) },
        { "cursorlocation", (2916985728, 2819375915) },
        { "cursorrect", (211960162, 2819375915) },
        { "cutscene", (395903610, 2819375915) },
        { "cutscenedetail", (857702847, 2819375915) },
        { "dawnparty", (4146448038, 2819375915) },
        { "dawnpartyhelp", (3120860873, 2819375915) },
        { "debug_launcher", (730437715, 2819375915) },
        { "deepdungeon", (4127877418, 2819375915) },
        { "deepdungeoninformation", (1741504194, 2819375915) },
        { "deepdungeoninspect", (3435267709, 2819375915) },
        { "deepdungeonnavimap", (3057020560, 2819375915) },
        { "deepdungeonresult", (1696544338, 2819375915) },
        { "deepdungeonsavedata", (1471812508, 2819375915) },
        { "deepdungeonscorelist", (1043016783, 2819375915) },
        { "deepdungeontopmenu", (430950110, 2819375915) },
        { "description", (3005842832, 2819375915) },
        { "descriptiondd", (2965242303, 2819375915) },
        { "descriptionmini", (1877270592, 2819375915) },
        { "descriptionytc", (1932479588, 2819375915) },
        { "dialogue", (3694300363, 2819375915) },
        { "dpschallenge", (2234967980, 2819375915) },
        { "dragtargeta", (1816830166, 2819375915) },
        { "dtr", (2676705107, 2819375915) },
        { "emj", (1303757815, 2819375915) },
        { "emjconfig", (3866447785, 2819375915) },
        { "emjintroduction", (4040520057, 2819375915) },
        { "emjrankpoint", (3134623482, 2819375915) },
        { "emjrankresult", (1416632945, 2819375915) },
        { "emote", (1765007978, 2819375915) },
        { "emotestatushelp", (2923262504, 2819375915) },
        { "enemylist", (4066433516, 2819375915) },
        { "eurekachaintext", (494010651, 2819375915) },
        { "eurekaelementaledit", (2920188705, 2819375915) },
        { "eurekaelementalhud", (3752726654, 2819375915) },
        { "eurekalogosactionnote", (45335115, 2819375915) },
        { "eurekalogosaetherlist", (1602117431, 2819375915) },
        { "eurekalogosshardlist", (3012804135, 2819375915) },
        { "eurekalogosshardsynthesis", (2549480364, 2819375915) },
        { "eurekaweaponupgrade", (2677854853, 2819375915) },
        { "eurekaweaponupgradedialogue", (4065101937, 2819375915) },
        { "eventskip", (2277878569, 2819375915) },
        { "ex_hotbar_editor", (3728207535, 2819375915) },
        { "excellenttrade", (987570648, 2819375915) },
        { "exp", (889118716, 2819375915) },
        { "exp_ps3", (987696628, 2819375915) },
        { "explorationchart", (4052808218, 2819375915) },
        { "explorationconfirm", (2763080408, 2819375915) },
        { "explorationdetail", (2632649155, 2819375915) },
        { "explorationreport", (1538237392, 2819375915) },
        { "explorationship", (4290426670, 2819375915) },
        { "explorationsubmarine", (4271948435, 2819375915) },
        { "fashioncheck", (3086679058, 2819375915) },
        { "fashioncheckscoregauge", (361778749, 2819375915) },
        { "fateprogress", (1843110148, 2819375915) },
        { "fatereward", (1542783232, 2819375915) },
        { "fgscountdown", (3128165544, 2819375915) },
        { "fgsenterdialogue", (3241653078, 2819375915) },
        { "fgsexitdialogue", (1753179622, 2819375915) },
        { "fgshudgoal", (671941869, 2819375915) },
        { "fgshudscore", (3006126856, 2819375915) },
        { "fgshudstatus", (4265319341, 2819375915) },
        { "fgshudwatchwindow", (2365834810, 2819375915) },
        { "fgsracelog", (275224952, 2819375915) },
        { "fgsresult", (4285828936, 2819375915) },
        { "fgsresultwinner", (699610794, 2819375915) },
        { "fgsscreeneliminated", (3053460172, 2819375915) },
        { "fgsscreenqualified", (3259143551, 2819375915) },
        { "fgsscreenroundover", (323741985, 2819375915) },
        { "fgsscreenwinner", (1574539963, 2819375915) },
        { "fgsstagedescription", (1633619392, 2819375915) },
        { "fgsstageintrobanner", (852069739, 2819375915) },
        { "fieldmarker", (4131715793, 2819375915) },
        { "fishguide", (3757942419, 2819375915) },
        { "fishguidefiltersetting", (3602138186, 2819375915) },
        { "fishharpoontip", (2075319391, 2819375915) },
        { "fishingbait", (2454744217, 2819375915) },
        { "fishingharpoon", (124052280, 2819375915) },
        { "fishingnotebook", (867175354, 2819375915) },
        { "fishrecords", (2030492065, 2819375915) },
        { "fishrelease", (2419776746, 2819375915) },
        { "fittingroom", (1897698852, 2819375915) },
        { "fittingroomglassselect", (3134013232, 2819375915) },
        { "fittingroomstore", (3828380634, 2819375915) },
        { "fittingroomstoresetting", (329036313, 2819375915) },
        { "flyingpermission", (264452878, 2819375915) },
        { "flytext", (808797647, 2819375915) },
        { "focustargetinfo", (459904192, 2819375915) },
        { "freecompany", (1927243914, 2819375915) },
        { "freecompanyaction", (254391732, 2819375915) },
        { "freecompanyactivity", (1517955475, 2819375915) },
        { "freecompanyapplication", (1649483074, 2819375915) },
        { "freecompanychest", (1075977391, 2819375915) },
        { "freecompanychestlog", (3258900752, 2819375915) },
        { "freecompanycrestcolor", (392397972, 2819375915) },
        { "freecompanycrestdecal", (4048604655, 2819375915) },
        { "freecompanycresteditor", (2589003913, 2819375915) },
        { "freecompanycrestsymbolcolor", (489115315, 2819375915) },
        { "freecompanyexchange", (2502546363, 2819375915) },
        { "freecompanyinputmessage", (3841891962, 2819375915) },
        { "freecompanyinputstring", (905500685, 2819375915) },
        { "freecompanylog", (1961318266, 2819375915) },
        { "freecompanymember", (58133423, 2819375915) },
        { "freecompanyprofile", (275030913, 2819375915) },
        { "freecompanyprofileedit", (2261659660, 2819375915) },
        { "freecompanyrank", (3008077525, 2819375915) },
        { "freecompanyrename", (1411777867, 2819375915) },
        { "freecompanyrights", (4089223638, 2819375915) },
        { "freecompanystatus", (3971775318, 2819375915) },
        { "freecompanytopics", (1130533310, 2819375915) },
        { "freeshop", (3481741944, 2819375915) },
        { "friendgroupedit", (3110701024, 2819375915) },
        { "gacha", (1449054711, 2819375915) },
        { "gateresult", (2283643448, 2819375915) },
        { "gathering", (3526643186, 2819375915) },
        { "gatheringcollectable", (10618115, 2819375915) },
        { "gatheringlocationeffect", (4085786308, 2819375915) },
        { "gatheringmasterpiece", (3451751471, 2819375915) },
        { "gatheringnotebook", (3259237289, 2819375915) },
        { "gcarmycaptureselect", (1501688533, 2819375915) },
        { "gcarmychangeclass", (4194554505, 2819375915) },
        { "gcarmychangeexpeditiontrait", (2110379163, 2819375915) },
        { "gcarmychangemirageprism", (2801962217, 2819375915) },
        { "gcarmychangetactic", (2834497139, 2819375915) },
        { "gcarmyexpeditionresult", (938950552, 2819375915) },
        { "gcarmyexpeditionselect", (3945158970, 2819375915) },
        { "gcarmymemberlist", (4168059592, 2819375915) },
        { "gcarmymemberprofile", (24414709, 2819375915) },
        { "gcarmymirageprism", (3781510342, 2819375915) },
        { "gcarmymirageprismdialogue", (2906200070, 2819375915) },
        { "gcarmyorder", (3550092218, 2819375915) },
        { "gcarmytraininglist", (2528352681, 2819375915) },
        { "gearsetlist", (2532726004, 2819375915) },
        { "gearsetpreview", (1859614849, 2819375915) },
        { "gearsetregistered", (2717329209, 2819375915) },
        { "gearsetview", (3919766081, 2819375915) },
        { "getaction", (1579064478, 2819375915) },
        { "goldsaucer", (1633801315, 2819375915) },
        { "goldsaucercarddeck", (2474358775, 2819375915) },
        { "goldsaucercarddeckedit", (1784551652, 2819375915) },
        { "goldsaucercardfilter", (3697205544, 2819375915) },
        { "goldsaucercardlist", (1717093630, 2819375915) },
        { "goldsauceremj", (2529026832, 2819375915) },
        { "goldsaucergeneral", (152255661, 2819375915) },
        { "goldsaucerraceappearance", (2809274575, 2819375915) },
        { "goldsaucerraceparameter", (2439077902, 2819375915) },
        { "goldsaucerracepedigree", (2875688648, 2819375915) },
        { "goldsaucerreward", (4286791018, 2819375915) },
        { "goldsaucerverminion", (867932273, 2819375915) },
        { "grandcompany0", (3702937418, 2819375915) },
        { "grandcompany1", (3788930810, 2819375915) },
        { "grandcompany2", (2792752170, 2819375915) },
        { "grandcompanyexchange", (124812906, 2819375915) },
        { "grandcompanyrank", (2369497248, 2819375915) },
        { "grandcompanyrankup", (2009970007, 2819375915) },
        { "grandcompanysupplylist", (466349544, 2819375915) },
        { "grandcompanysupplyreward", (2940317075, 2819375915) },
        { "groupposecamerasetting", (3313993641, 2819375915) },
        { "groupposeframe", (1855970209, 2819375915) },
        { "groupposeframe9grid001", (3063585974, 2819375915) },
        { "groupposeframe9grid002", (4047169126, 2819375915) },
        { "groupposeframe9grid003", (3428503510, 2819375915) },
        { "groupposeguide", (1740757332, 2819375915) },
        { "groupposestampeditor", (3265682579, 2819375915) },
        { "groupposestampimage", (2927221, 2819375915) },
        { "gsscreentext", (612378439, 2819375915) },
        { "guildleve", (2634824831, 2819375915) },
        { "guildlevedifficulty", (202676480, 2819375915) },
        { "guildlevehistory", (2977520649, 2819375915) },
        { "hammer", (2126053567, 2819375915) },
        { "heavensturndescription", (4262921601, 2819375915) },
        { "helpwindow", (750455933, 2819375915) },
        { "hotobarcopy", (4205569883, 2819375915) },
        { "housingappealsetting", (1646631900, 2819375915) },
        { "housingblacklistauthority", (2655152823, 2819375915) },
        { "housingblacklisteviction", (4160193423, 2819375915) },
        { "housingblacklistsetting", (1906656694, 2819375915) },
        { "housingchocobolist", (3552254301, 2819375915) },
        { "housingconfig", (2278940174, 2819375915) },
        { "housingconfiglight", (3046942635, 2819375915) },
        { "housingeditexterior", (472384628, 2819375915) },
        { "housingeditinterior", (1600210813, 2819375915) },
        { "housingeditmessage", (2686846894, 2819375915) },
        { "housingfurniturecatalog", (618081872, 2819375915) },
        { "housinggardening", (689909623, 2819375915) },
        { "housinggoods", (1380999081, 2819375915) },
        { "housingguestbook", (3856071374, 2819375915) },
        { "housinglayout", (246287426, 2819375915) },
        { "housingmate", (1634079918, 2819375915) },
        { "housingpadguide", (3425172642, 2819375915) },
        { "housingreleaseauthority", (3545194630, 2819375915) },
        { "housingselectblock", (562531498, 2819375915) },
        { "housingselectdeed", (718070790, 2819375915) },
        { "housingselectroom", (2565063411, 2819375915) },
        { "housingsignboard", (1002370693, 2819375915) },
        { "housingtravellersnote", (155420840, 2819375915) },
        { "housingwarehousestatus", (2173348236, 2819375915) },
        { "housingwheelstand", (2663178829, 2819375915) },
        { "housingwithdrawstorage", (935751039, 2819375915) },
        { "howto", (3302541384, 2819375915) },
        { "howtolist", (3692173614, 2819375915) },
        { "howtonortice", (1683240363, 2819375915) },
        { "howtonotice", (1745319541, 2819375915) },
        { "hudlayout", (1883262367, 2819375915) },
        { "hudlayoutbg", (861932886, 2819375915) },
        { "hudlayoutcurrentbuffstatus", (4033573111, 2819375915) },
        { "hudlayoutcurrenttarget", (2298004100, 2819375915) },
        { "hudlayoutcurrentwindow", (617659600, 2819375915) },
        { "hudlayoutrect", (1488747796, 2819375915) },
        { "hudlayoutsetcopy", (59596582, 2819375915) },
        { "hudlayoutsnapsetting", (60814655, 2819375915) },
        { "hwdgathererinspection", (2536248414, 2819375915) },
        { "hwdgathererinspectiontargetlist", (1010906263, 2819375915) },
        { "hwdinfoboard", (1815465407, 2819375915) },
        { "hwdlottery", (2021839447, 2819375915) },
        { "hwdmonument", (3644091425, 2819375915) },
        { "hwdscorelist", (2808240154, 2819375915) },
        { "hwdsupply", (3915545334, 2819375915) },
        { "iconverminion", (685928255, 2819375915) },
        { "idlingcamerasetting", (1591383425, 2819375915) },
        { "image", (1517951510, 2819375915) },
        { "image2", (3697511133, 2819375915) },
        { "image3", (3775112045, 2819375915) },
        { "imageactionlearnedaoz", (2993854512, 2819375915) },
        { "imagebossname", (1510765375, 2819375915) },
        { "imagecutscenehildibrandattack", (952825415, 2819375915) },
        { "imageendingmessage210", (2751651253, 2819375915) },
        { "imagegenerictitle", (1050960189, 2819375915) },
        { "imagelocationtitle", (225424282, 2819375915) },
        { "imagelocationtitleshort", (2139283462, 2819375915) },
        { "imagenextpreview", (4022066363, 2819375915) },
        { "imagepatchlogotitle", (1313583407, 2819375915) },
        { "imagequestacceptedniku", (2701687226, 2819375915) },
        { "imagequestcompleteniku", (3096703127, 2819375915) },
        { "imagequestfailedniku", (141028331, 2819375915) },
        { "imagesnipetitle", (511620610, 2819375915) },
        { "imagestreakbkc", (3871103201, 2819375915) },
        { "imagestreaksxt", (1889006652, 2819375915) },
        { "imagesystem", (1513495255, 2819375915) },
        { "inclusionshop", (2827394091, 2819375915) },
        { "inputmessage", (2367070854, 2819375915) },
        { "inputnumeric", (250672803, 2819375915) },
        { "inputsearchcomment", (539207997, 2819375915) },
        { "inputstring", (3674659486, 2819375915) },
        { "invaluable", (1825838750, 2819375915) },
        { "inventory", (2080023841, 2819375915) },
        { "inventoryevent", (2312499303, 2819375915) },
        { "inventoryeventgrid", (1239823123, 2819375915) },
        { "inventoryexpansion", (3058833343, 2819375915) },
        { "inventorygrid", (3695277044, 2819375915) },
        { "inventorygridcrystal", (1840391991, 2819375915) },
        { "inventorylarge", (2348692404, 2819375915) },
        { "itemdetail", (1953083196, 2819375915) },
        { "itemdetailcompare", (2660614219, 2819375915) },
        { "itemfinder", (2180605897, 2819375915) },
        { "iteminspection", (1009035851, 2819375915) },
        { "iteminspectionlist", (2392554769, 2819375915) },
        { "iteminspectionresult", (3570905917, 2819375915) },
        { "itemsearch", (2272069911, 2819375915) },
        { "itemsearchfilter", (3571041900, 2819375915) },
        { "itemsearchhistory", (3328254355, 2819375915) },
        { "itemsearchresult", (3070990513, 2819375915) },
        { "itemsearchsetting", (4261359970, 2819375915) },
        { "jigsaw", (885277863, 2819375915) },
        { "jigsawevent", (2158233688, 2819375915) },
        { "jobhudacn0", (3258717394, 2819375915) },
        { "jobhudarc0", (3205206134, 2819375915) },
        { "jobhudast0", (128952801, 2819375915) },
        { "jobhudblm0", (1013911926, 2819375915) },
        { "jobhudblm1", (17774790, 2819375915) },
        { "jobhudbrd0", (746694445, 2819375915) },
        { "jobhuddnc0", (1469409448, 2819375915) },
        { "jobhuddnc1", (1794457880, 2819375915) },
        { "jobhuddrg0", (1819991044, 2819375915) },
        { "jobhuddrk0", (465050751, 2819375915) },
        { "jobhuddrk1", (651703759, 2819375915) },
        { "jobhudgff0", (3124277407, 2819375915) },
        { "jobhudgff1", (2270730543, 2819375915) },
        { "jobhudgnb0", (306625774, 2819375915) },
        { "jobhudmanual", (3082663515, 2819375915) },
        { "jobhudmch0", (1137369728, 2819375915) },
        { "jobhudmch1", (2125122352, 2819375915) },
        { "jobhudmnk0", (2760503790, 2819375915) },
        { "jobhudmnk1", (2582239326, 2819375915) },
        { "jobhudnin0", (3211548976, 2819375915) },
        { "jobhudnin1", (2181853312, 2819375915) },
        { "jobhudnotice", (2576731881, 2819375915) },
        { "jobhudpld0", (637433576, 2819375915) },
        { "jobhudrdb0", (2188238063, 2819375915) },
        { "jobhudrdb1", (3205363039, 2819375915) },
        { "jobhudrdm0", (1933274682, 2819375915) },
        { "jobhudrpm0", (3769292519, 2819375915) },
        { "jobhudrpm1", (3721068375, 2819375915) },
        { "jobhudrrp0", (3363690123, 2819375915) },
        { "jobhudrrp1", (4112383803, 2819375915) },
        { "jobhudsam0", (3987292419, 2819375915) },
        { "jobhudsam1", (3502860467, 2819375915) },
        { "jobhudsch0", (721142169, 2819375915) },
        { "jobhudsch1", (396093481, 2819375915) },
        { "jobhudsmn0", (2895993305, 2819375915) },
        { "jobhudsmn1", (2449305705, 2819375915) },
        { "jobhudwar0", (2607022007, 2819375915) },
        { "jobhudwhm0", (4235921962, 2819375915) },
        { "journal", (2265795954, 2819375915) },
        { "journalaccept", (2845123510, 2819375915) },
        { "journaldetail", (3403547209, 2819375915) },
        { "journalresult", (2036797394, 2819375915) },
        { "legacyitemstorage", (2329899322, 2819375915) },
        { "legacyplayer", (2139945438, 2819375915) },
        { "letteraddress", (3927788258, 2819375915) },
        { "lettereditor", (1771923107, 2819375915) },
        { "letterhistory", (1341935010, 2819375915) },
        { "letterlist", (2454861842, 2819375915) },
        { "letterviewer", (2476185515, 2819375915) },
        { "leveldown", (3339153840, 2819375915) },
        { "levelup", (2644588584, 2819375915) },
        { "levelup2", (499469396, 2819375915) },
        { "lfa", (3308931062, 2819375915) },
        { "lfg", (1249555030, 2819375915) },
        { "lfgcondition", (3216385180, 2819375915) },
        { "lfgdetail", (4078444499, 2819375915) },
        { "lfgfiltersettings", (1432963566, 2819375915) },
        { "lfgprivate", (2435953505, 2819375915) },
        { "lfgrecruiternamesearch", (1748168461, 2819375915) },
        { "lfgsearch", (1882616, 2819375915) },
        { "lfgsearchlogsetting", (813819315, 2819375915) },
        { "lfgselectrole", (1371224236, 2819375915) },
        { "limitbreak", (2594741569, 2819375915) },
        { "linkshell", (1535528853, 2819375915) },
        { "listcheckbox", (2184939465, 2819375915) },
        { "listcolorchooser", (3667714959, 2819375915) },
        { "listdragtarget", (1598029575, 2819375915) },
        { "listgrid", (1091176453, 2819375915) },
        { "listicon", (163608305, 2819375915) },
        { "loading", (1734414762, 2819375915) },
        { "logo", (4290020684, 2819375915) },
        { "logo_de", (1509528600, 2819375915) },
        { "logo_en", (3849680556, 2819375915) },
        { "logo_fr", (3337715329, 2819375915) },
        { "logo_ja", (2524140456, 2819375915) },
        { "lotterydaily", (396943319, 2819375915) },
        { "lotteryweekly", (1325928552, 2819375915) },
        { "lotteryweeklyhistory", (447999190, 2819375915) },
        { "lotteryweeklyrewardlist", (3230659321, 2819375915) },
        { "lovmactiondetail", (1303734236, 2819375915) },
        { "lovmconfirm", (2593873133, 2819375915) },
        { "lovmheader", (214957786, 2819375915) },
        { "lovmhelp", (4043435317, 2819375915) },
        { "lovmhudcutin", (3087326282, 2819375915) },
        { "lovmminimap", (4121825813, 2819375915) },
        { "lovmnameplate", (2578234824, 2819375915) },
        { "lovmpalette", (4078941219, 2819375915) },
        { "lovmpaletteedit", (2366457736, 2819375915) },
        { "lovmpartylist", (4259757007, 2819375915) },
        { "lovmqueuelist", (227917786, 2819375915) },
        { "lovmranking", (1317779653, 2819375915) },
        { "lovmready", (2448556194, 2819375915) },
        { "lovmresult", (1799088831, 2819375915) },
        { "lovmstatus", (827483410, 2819375915) },
        { "lovmtargetinfo", (2943190326, 2819375915) },
        { "lovmtypeinfo", (3269446249, 2819375915) },
        { "lovmversus", (1532227618, 2819375915) },
        { "macro", (3725444341, 2819375915) },
        { "macrotextcommandlist", (2097450388, 2819375915) },
        { "maincommand", (572440063, 2819375915) },
        { "maincross", (3136667368, 2819375915) },
        { "maincrossnotification", (2457149864, 2819375915) },
        { "materiaattach", (2598828517, 2819375915) },
        { "materiaattachdialog", (676004843, 2819375915) },
        { "materiaattachedrepair", (3721459308, 2819375915) },
        { "materiabroker", (2070095201, 2819375915) },
        { "materiabrokerdialog", (2479373283, 2819375915) },
        { "materialize", (2123849929, 2819375915) },
        { "materializedialog", (4183505986, 2819375915) },
        { "materiaparameter", (465690706, 2819375915) },
        { "materiarepair", (3796187011, 2819375915) },
        { "materiaretrievedialog", (2417304342, 2819375915) },
        { "memberrankassign", (1639558730, 2819375915) },
        { "memberrankedit", (511889261, 2819375915) },
        { "memberrankorderedit", (1497391061, 2819375915) },
        { "mentorcertification", (2670339803, 2819375915) },
        { "mentorcondition", (3842330165, 2819375915) },
        { "mentorrenewdialogue", (115661319, 2819375915) },
        { "merchantequipselect", (2882627856, 2819375915) },
        { "merchantsetting", (3599085779, 2819375915) },
        { "merchantshop", (1008570013, 2819375915) },
        { "minerbotanistaim", (942420039, 2819375915) },
        { "minergame", (1686189451, 2819375915) },
        { "minidungeon", (3707078566, 2819375915) },
        { "minidungeonanswer", (21647359, 2819375915) },
        { "minidungeonresult", (1884708488, 2819375915) },
        { "minionnotebook", (523079663, 2819375915) },
        { "minionnotebookykw", (3068186170, 2819375915) },
        { "minitalk", (602889709, 2819375915) },
        { "mirageprism", (3997531512, 2819375915) },
        { "mirageprismbox", (1464276760, 2819375915) },
        { "mirageprismboxfiltersetting", (917152903, 2819375915) },
        { "mirageprismboxitemdetail", (263126417, 2819375915) },
        { "mirageprismcrystalize", (153949050, 2819375915) },
        { "mirageprismexecute", (2248699906, 2819375915) },
        { "mirageprismplate", (2645334744, 2819375915) },
        { "mirageprismplatedialogue", (892404149, 2819375915) },
        { "mirageprismremove", (891906841, 2819375915) },
        { "mjianimalbreeding", (3693764751, 2819375915) },
        { "mjianimalbreedingautomatic", (3632127481, 2819375915) },
        { "mjianimalnameinputstring", (3370478012, 2819375915) },
        { "mjibuilding", (1594527363, 2819375915) },
        { "mjibuildingmove", (2142074415, 2819375915) },
        { "mjibuildingprogress", (2784071742, 2819375915) },
        { "mjicraftdemandresearch", (3614970314, 2819375915) },
        { "mjicraftmaterialconfirmation", (759427875, 2819375915) },
        { "mjicraftsales", (2661755828, 2819375915) },
        { "mjicraftschedule", (4134773662, 2819375915) },
        { "mjicraftschedulemaintenance", (235545895, 2819375915) },
        { "mjicraftschedulemateriallist", (3127023341, 2819375915) },
        { "mjicraftschedulepreset", (1669979014, 2819375915) },
        { "mjicraftschedulesetting", (1420116689, 2819375915) },
        { "mjidisposeshop", (601513779, 2819375915) },
        { "mjidisposeshopshipping", (24773575, 2819375915) },
        { "mjidisposeshopshippingbulk", (640252451, 2819375915) },
        { "mjientrance", (1527695212, 2819375915) },
        { "mjifarmautomatic", (2127802982, 2819375915) },
        { "mjifarmmanagement", (3823574581, 2819375915) },
        { "mjigatheringhouse", (2267768131, 2819375915) },
        { "mjigatheringhouseexplore", (2303018486, 2819375915) },
        { "mjigatheringnotebook", (3290413480, 2819375915) },
        { "mjihousinggoods", (3747154332, 2819375915) },
        { "mjihud", (549524674, 2819375915) },
        { "mjiminionmanagement", (1708452275, 2819375915) },
        { "mjiminionnotebook", (1058288785, 2819375915) },
        { "mjimissioncomplete", (1277470236, 2819375915) },
        { "mjinekomimirequest", (607367553, 2819375915) },
        { "mjipadguide", (3186040439, 2819375915) },
        { "mjipouch", (1543801114, 2819375915) },
        { "mjirecipenotebook", (2893402576, 2819375915) },
        { "mjisetting", (3622656707, 2819375915) },
        { "mobhunt", (1341389832, 2819375915) },
        { "mobhunt2", (645480024, 2819375915) },
        { "mobhunt3", (454629352, 2819375915) },
        { "mobhunt4", (2839137272, 2819375915) },
        { "mobhunt5", (2488918600, 2819375915) },
        { "mobhunt6", (3556350104, 2819375915) },
        { "mogcatcher", (2518713800, 2819375915) },
        { "money", (1529079558, 2819375915) },
        { "money_ps3", (283609220, 2819375915) },
        { "monsternotebook", (1884602424, 2819375915) },
        { "mooglecollection", (1747492723, 2819375915) },
        { "mooglecollectionrewardlist", (1430331964, 2819375915) },
        { "mount_speed", (892521959, 2819375915) },
        { "mountnotebook", (1780029403, 2819375915) },
        { "moviesubtitle_opening_chs", (2335316256, 2819375915) },
        { "moviesubtitle_opening_cht", (957531440, 2819375915) },
        { "moviesubtitle_opening_credit", (157018412, 2819375915) },
        { "moviesubtitle_opening_de", (2774599987, 2819375915) },
        { "moviesubtitle_opening_en", (435022727, 2819375915) },
        { "moviesubtitle_opening_fr", (980018090, 2819375915) },
        { "moviesubtitle_opening_ja", (1793838723, 2819375915) },
        { "moviesubtitle_trailer_de", (2527985619, 2819375915) },
        { "moviesubtitle_trailer_en", (706813287, 2819375915) },
        { "moviesubtitle_trailer_fr", (161813834, 2819375915) },
        { "moviesubtitle_trailer_ja", (1495742563, 2819375915) },
        { "moviesubtitle_voyage_chs", (1494050450, 2819375915) },
        { "moviesubtitle_voyage_cht", (3945642626, 2819375915) },
        { "moviesubtitle_voyage_de", (2543535637, 2819375915) },
        { "moviesubtitle_voyage_en", (722969761, 2819375915) },
        { "moviesubtitle_voyage_fr", (143895692, 2819375915) },
        { "moviesubtitle_voyage_ja", (1477542309, 2819375915) },
        { "multiplehelpwindow", (3119416245, 2819375915) },
        { "mutelist", (3711581488, 2819375915) },
        { "mycactionselect", (3369978366, 2819375915) },
        { "mycbattleareainfo", (2276826360, 2819375915) },
        { "myccharacternote", (2602647228, 2819375915) },
        { "mycduelrequest", (1523004580, 2819375915) },
        { "mycduelrequestdialogue", (3764184587, 2819375915) },
        { "mycinfo", (539555248, 2819375915) },
        { "mycitembag", (2604438578, 2819375915) },
        { "mycitembox", (1132918033, 2819375915) },
        { "mycitemmyset", (250990214, 2819375915) },
        { "mycrelicgrowth", (3002344257, 2819375915) },
        { "mycrelicgrowth2", (696565939, 2819375915) },
        { "nameplate", (299904569, 2819375915) },
        { "navimap", (2780397541, 2819375915) },
        { "needgreed", (3889319681, 2819375915) },
        { "needgreedtargeting", (1405831890, 2819375915) },
        { "negotiation", (4137477434, 2819375915) },
        { "ngwordedit", (794043127, 2819375915) },
        { "ngwordfilterlist", (340706362, 2819375915) },
        { "ngwordfiltersetting", (3996432499, 2819375915) },
        { "notification", (2206138226, 2819375915) },
        { "notificationitem", (2228476605, 2819375915) },
        { "operationguide", (1126952254, 2819375915) },
        { "orchestrion", (2280992756, 2819375915) },
        { "orchestrionplaylist", (620993753, 2819375915) },
        { "orchestrionplaylistedit", (3679423663, 2819375915) },
        { "orchestrionplaylistmusicselect", (2126813683, 2819375915) },
        { "ornamentnotebook", (1543393972, 2819375915) },
        { "padmousemode", (1352080217, 2819375915) },
        { "parameter", (109609964, 2819375915) },
        { "partylist", (1974085694, 2819375915) },
        { "partymemberlist", (1757020999, 2819375915) },
        { "pcsearchdetail", (2703209918, 2819375915) },
        { "pcsearchselectclass", (3717583565, 2819375915) },
        { "pcsearchselectlocation", (2974360921, 2819375915) },
        { "performance", (776959863, 2819375915) },
        { "performancegamepadguide", (599539816, 2819375915) },
        { "performancekeybind", (3557669752, 2819375915) },
        { "performancemetronome", (3235202286, 2819375915) },
        { "performancemetronomesetting", (2578150539, 2819375915) },
        { "performanceplayguide", (1472249906, 2819375915) },
        { "performanceplayguidesetting", (4169042587, 2819375915) },
        { "performancereadycheck", (2362334322, 2819375915) },
        { "performancereadycheckreceive", (3033512824, 2819375915) },
        { "performancetonechange", (1987644681, 2819375915) },
        { "performancewide", (2785410595, 2819375915) },
        { "picturepreview", (924056986, 2819375915) },
        { "pointselect", (3036002267, 2819375915) },
        { "popuptext", (2242850733, 2819375915) },
        { "previewa", (2269539895, 2819375915) },
        { "progressbar", (3885651733, 2819375915) },
        { "punchingmachine", (2059895011, 2819375915) },
        { "purify", (461149941, 2819375915) },
        { "purifyauto", (3093385019, 2819375915) },
        { "purifydialog", (49648027, 2819375915) },
        { "purifyresult", (2742936471, 2819375915) },
        { "puzzle", (2586071798, 2819375915) },
        { "pvp", (991038949, 2819375915) },
        { "pvpaction", (1547006098, 2819375915) },
        { "pvpactionadditional", (2089508903, 2819375915) },
        { "pvpactionjob", (254184681, 2819375915) },
        { "pvpactionquickchat", (316948257, 2819375915) },
        { "pvpactiontraits", (2217451789, 2819375915) },
        { "pvpcalendar", (4277285797, 2819375915) },
        { "pvpcalender", (1701015987, 2819375915) },
        { "pvpcharacter", (3143242254, 2819375915) },
        { "pvpcolosseum", (138703536, 2819375915) },
        { "pvpcolosseumheader", (1840758869, 2819375915) },
        { "pvpcolosseumpartylist4", (3927393643, 2819375915) },
        { "pvpcolosseumresult", (172217392, 2819375915) },
        { "pvpduelrequest", (2392989422, 2819375915) },
        { "pvpfrontline", (2932325677, 2819375915) },
        { "pvpfrontlinegauge", (1286424079, 2819375915) },
        { "pvpfrontlineheader", (2232648389, 2819375915) },
        { "pvpfrontlineresult", (3806828192, 2819375915) },
        { "pvpfrontlineresultdetail", (2631096754, 2819375915) },
        { "pvpmksbattlelog", (2246942676, 2819375915) },
        { "pvpmkscountdown", (1216824131, 2819375915) },
        { "pvpmksheader", (2284022490, 2819375915) },
        { "pvpmksintroduction", (165308968, 2819375915) },
        { "pvpmksnavimap", (1745303581, 2819375915) },
        { "pvpmkspartylist5", (1945062377, 2819375915) },
        { "pvpmksrankratingfunction", (513513853, 2819375915) },
        { "pvpmksresult", (4023877311, 2819375915) },
        { "pvpmksreward", (2704293105, 2819375915) },
        { "pvppresetlist", (750173715, 2819375915) },
        { "pvppresetview", (1407322278, 2819375915) },
        { "pvprankemblem1", (3474571730, 2819375915) },
        { "pvprankemblem2", (2293873410, 2819375915) },
        { "pvprankemblem3", (3050934962, 2819375915) },
        { "pvprankemblem4", (133779106, 2819375915) },
        { "pvprankemblem5", (983131922, 2819375915) },
        { "pvprankemblem6", (2100895170, 2819375915) },
        { "pvprankpromotionqualifier", (3995310124, 2819375915) },
        { "pvprankratingfunction", (856407302, 2819375915) },
        { "pvpscreeninformation", (2400338352, 2819375915) },
        { "pvpscreeninformationhotbar", (3625885061, 2819375915) },
        { "pvpsimulation", (116946341, 2819375915) },
        { "pvpsimulationalliance", (3309677600, 2819375915) },
        { "pvpsimulationdisplay", (2073181451, 2819375915) },
        { "pvpsimulationdisplay2", (3208908323, 2819375915) },
        { "pvpsimulationheader", (3518387013, 2819375915) },
        { "pvpsimulationheader2", (780814042, 2819375915) },
        { "pvpsimulationmachineselect", (3788468761, 2819375915) },
        { "pvpsimulationresult", (3057796896, 2819375915) },
        { "pvpspectatorcameralist", (3370572079, 2819375915) },
        { "pvpspectatorheader", (3469797678, 2819375915) },
        { "pvpspectatorlist", (2330567431, 2819375915) },
        { "pvpspectatorpartylist4", (81137835, 2819375915) },
        { "pvpspectatorpartylist8", (3240557994, 2819375915) },
        { "pvptacticalsituationlist", (3638183376, 2819375915) },
        { "pvpteam", (2742045341, 2819375915) },
        { "pvpteamactivity", (687459526, 2819375915) },
        { "pvpteammember", (1072042940, 2819375915) },
        { "pvpteamorganization", (2628427545, 2819375915) },
        { "pvpteamresult", (2319772904, 2819375915) },
        { "pvpteamstatus", (3492635461, 2819375915) },
        { "pvpwelcomedialogue", (203600283, 2819375915) },
        { "qtebutton", (2161976055, 2819375915) },
        { "qtebuttonkeep", (3705292659, 2819375915) },
        { "qtebuttonmashing", (2755286071, 2819375915) },
        { "qtescreeninfo", (2162486326, 2819375915) },
        { "qtestreak", (2867244191, 2819375915) },
        { "questredoui", (1680753245, 2819375915) },
        { "questredouihud", (1213334853, 2819375915) },
        { "racechocoboconfirm", (3931237272, 2819375915) },
        { "racechocoboitembox", (499128005, 2819375915) },
        { "racechocoboparameter", (2307509067, 2819375915) },
        { "racechocoboranking", (1044451248, 2819375915) },
        { "racechocoboready", (3639246146, 2819375915) },
        { "racechocoboresult", (3413643450, 2819375915) },
        { "racechocobostatus", (2433851159, 2819375915) },
        { "raidfinder", (2132543109, 2819375915) },
        { "raidfinderhelp", (1959501553, 2819375915) },
        { "readycheck", (563306187, 2819375915) },
        { "recipemateriallist", (3262045754, 2819375915) },
        { "recipenotebook", (2353818286, 2819375915) },
        { "recipenotebookfiltersetting", (258588802, 2819375915) },
        { "recipenotebookinspectionlist", (738738647, 2819375915) },
        { "recipeproductlist", (409756601, 2819375915) },
        { "recipetree", (1765213259, 2819375915) },
        { "recommendlist", (3578030859, 2819375915) },
        { "reconstruction", (420262580, 2819375915) },
        { "reconstructionbuyback", (1269835501, 2819375915) },
        { "relic2glass", (973728133, 2819375915) },
        { "relic2growth", (1600418752, 2819375915) },
        { "relic2growthfragment", (1621882494, 2819375915) },
        { "relicglass", (982230590, 2819375915) },
        { "relicmagicite", (2815347881, 2819375915) },
        { "relicmandervilleconfirm", (1791185115, 2819375915) },
        { "relicmandervillegrowth", (3281793539, 2819375915) },
        { "relicnotebook", (3520103741, 2819375915) },
        { "relicspherescroll", (779729036, 2819375915) },
        { "relicsphereupgrade", (4005548095, 2819375915) },
        { "repair", (1141231207, 2819375915) },
        { "repairrequest", (3369844307, 2819375915) },
        { "request", (3485109001, 2819375915) },
        { "residentdragtarget", (66189290, 2819375915) },
        { "retainercharacter", (668614514, 2819375915) },
        { "retainerhistory", (2060053200, 2819375915) },
        { "retainerinventory", (354061192, 2819375915) },
        { "retainerinventorygrid", (713421090, 2819375915) },
        { "retainerinventorygridcrystal", (1229038587, 2819375915) },
        { "retainerinventorylarge", (3837296855, 2819375915) },
        { "retainerlist", (3448856323, 2819375915) },
        { "retainersell", (230324989, 2819375915) },
        { "retainerselllist", (2232942281, 2819375915) },
        { "retainersort", (3230046747, 2819375915) },
        { "retainertask", (3012574121, 2819375915) },
        { "retainertaskdetail", (2835189759, 2819375915) },
        { "retainertaskresult", (457474660, 2819375915) },
        { "retainertasksupply", (2086750174, 2819375915) },
        { "retainertransferlist", (2773108875, 2819375915) },
        { "retainertransferprogress", (280400717, 2819375915) },
        { "returnerdialogue", (2411686349, 2819375915) },
        { "returnerdialoguedetail", (2892758404, 2819375915) },
        { "rhythmaction", (1849750502, 2819375915) },
        { "rhythmactionresult", (2670364221, 2819375915) },
        { "rhythmactionstatus", (3309504912, 2819375915) },
        { "rideshooting", (1561234597, 2819375915) },
        { "rideshootingbg", (4205448263, 2819375915) },
        { "rideshootingflytext", (1955036244, 2819375915) },
        { "rideshootingresult", (813890854, 2819375915) },
        { "roadstone", (1461016324, 2819375915) },
        { "roadstoneresult", (2523407469, 2819375915) },
        { "salvage", (3847720154, 2819375915) },
        { "salvageauto", (2113205383, 2819375915) },
        { "salvagedialog", (372694974, 2819375915) },
        { "salvageresult", (3082726834, 2819375915) },
        { "satisfactionsupply", (2714319707, 2819375915) },
        { "satisfactionsupplychangemirageprism", (2353507590, 2819375915) },
        { "satisfactionsupplymirageprism", (3405856735, 2819375915) },
        { "satisfactionsupplyresult", (3937996096, 2819375915) },
        { "scenariotree", (974506507, 2819375915) },
        { "scenariotreedetail", (3172868337, 2819375915) },
        { "screenframe", (3547333670, 2819375915) },
        { "screeninfo", (21783946, 2819375915) },
        { "screeninfo_countdown", (2930331812, 2819375915) },
        { "screeninfo_racechocobo", (3433157138, 2819375915) },
        { "screeninfo_racechocobologo", (1972521447, 2819375915) },
        { "selectcustomstring", (404103618, 2819375915) },
        { "selecticonstring", (612852840, 2819375915) },
        { "selectlist", (3105634636, 2819375915) },
        { "selectok", (3943770900, 2819375915) },
        { "selectoktitle", (4138028596, 2819375915) },
        { "selectretry", (1481534579, 2819375915) },
        { "selectstring", (4166427995, 2819375915) },
        { "selectstringcutscene", (2564692676, 2819375915) },
        { "selectstringdd", (2749260941, 2819375915) },
        { "selectstringeventgimmick", (3746125068, 2819375915) },
        { "selectyesno", (2215144674, 2819375915) },
        { "selectyesnocount", (2121356904, 2819375915) },
        { "selectyesnocountitem", (692754887, 2819375915) },
        { "selectyesnodialogue", (3387942888, 2819375915) },
        { "selectyesnotextscroll", (2675986454, 2819375915) },
        { "shop", (2439715451, 2819375915) },
        { "shopcard", (622587859, 2819375915) },
        { "shopcarddialog", (2949050926, 2819375915) },
        { "shopexchangecoin", (2253654788, 2819375915) },
        { "shopexchangecurrency", (1771083251, 2819375915) },
        { "shopexchangecurrencydialog", (1490045672, 2819375915) },
        { "shopexchangeitem", (1110402387, 2819375915) },
        { "shopexchangeitemdialog", (2029124758, 2819375915) },
        { "shopgrandcompany", (2978391621, 2819375915) },
        { "skyisland", (2857329587, 2819375915) },
        { "skyislandresult", (3702134435, 2819375915) },
        { "skyislandresult2", (1743658450, 2819375915) },
        { "skyislandsetting", (237887908, 2819375915) },
        { "skyislandspoiltrade", (3561662382, 2819375915) },
        { "snipehud", (2386700603, 2819375915) },
        { "snipehudbg", (3017027576, 2819375915) },
        { "snipetodo", (2974876165, 2819375915) },
        { "social", (3434216678, 2819375915) },
        { "socialdetaila", (195526509, 2819375915) },
        { "socialdetailb", (1275528637, 2819375915) },
        { "sociallist", (3894436488, 2819375915) },
        { "status", (3707738855, 2819375915) },
        { "statuscustom", (1780486232, 2819375915) },
        { "statuscustomproc", (176400961, 2819375915) },
        { "storagecheck", (3662757807, 2819375915) },
        { "storysupport", (1570588677, 2819375915) },
        { "storysupportmemberselect", (1842601317, 2819375915) },
        { "streak", (666216347, 2819375915) },
        { "subcommandsetting", (2902686414, 2819375915) },
        { "supplycollectableitems", (1815669948, 2819375915) },
        { "supplymasterpiece", (2834436015, 2819375915) },
        { "supportdesk", (748408443, 2819375915) },
        { "supportdeskeditor", (1979299626, 2819375915) },
        { "supportdesklist", (1334068319, 2819375915) },
        { "supportdesknews", (3820009296, 2819375915) },
        { "supportdesknotification", (28781760, 2819375915) },
        { "supportdeskquestion", (470261049, 2819375915) },
        { "supportdeskreport", (2446671657, 2819375915) },
        { "supportdeskviewer", (2415085090, 2819375915) },
        { "synthesis", (296745610, 2819375915) },
        { "synthesiscondition", (676305727, 2819375915) },
        { "synthesissimple", (1405255624, 2819375915) },
        { "synthesissimpledialog", (485606807, 2819375915) },
        { "synthesissimulator", (4281264479, 2819375915) },
        { "talk", (2677179020, 2819375915) },
        { "talkautomessageselector", (785151261, 2819375915) },
        { "talkautomessagesetting", (2647992534, 2819375915) },
        { "talkspeed", (1405339018, 2819375915) },
        { "talksubtitle", (1612697103, 2819375915) },
        { "targetcursor", (1035787382, 2819375915) },
        { "targetcursorgrand", (4193681590, 2819375915) },
        { "targetinfo", (1700727599, 2819375915) },
        { "targetinfobuffdebuff", (191223433, 2819375915) },
        { "targetinfocastbar", (335670956, 2819375915) },
        { "targetinfomaintarget", (2333346403, 2819375915) },
        { "teleport", (1436298609, 2819375915) },
        { "teleporthousingfriend", (1579548188, 2819375915) },
        { "teleportsetting", (1890387810, 2819375915) },
        { "teleporttown", (2420786956, 2819375915) },
        { "text0", (1407667478, 2819375915) },
        { "text1", (1854366886, 2819375915) },
        { "text2", (690429558, 2819375915) },
        { "textachievementunlocked", (1747382797, 2819375915) },
        { "textchain", (1391200255, 2819375915) },
        { "textclasschange", (3813222370, 2819375915) },
        { "textcontentsnotebook", (3178045777, 2819375915) },
        { "texterror", (3924970587, 2819375915) },
        { "textfishingnote", (1639390308, 2819375915) },
        { "textgimmickhint", (1203159328, 2819375915) },
        { "texthousinggardening", (3208472227, 2819375915) },
        { "textmonsternote0", (2451935056, 2819375915) },
        { "textmonsternote1", (2940577504, 2819375915) },
        { "textmonsternote2", (3907378224, 2819375915) },
        { "textrelicatma", (2771860689, 2819375915) },
        { "texttargetcircle", (3120589862, 2819375915) },
        { "thestarlightcelebration2020", (4286639942, 2819375915) },
        { "tips", (2298073672, 2819375915) },
        { "title", (3701404256, 2819375915) },
        { "title_bg400", (2021797420, 2819375915) },
        { "title_connect", (3226174914, 2819375915) },
        { "title_datacenter", (854969204, 2819375915) },
        { "title_logo", (3593416941, 2819375915) },
        { "title_logo300", (1423351708, 2819375915) },
        { "title_logo400", (2440143122, 2819375915) },
        { "title_logo500", (923183782, 2819375915) },
        { "title_logo600", (116305979, 2819375915) },
        { "title_logo700", (2694429583, 2819375915) },
        { "title_logoft", (3154907784, 2819375915) },
        { "title_logoonline", (3069768329, 2819375915) },
        { "title_menu", (3014308310, 2819375915) },
        { "title_movieselector", (1467996346, 2819375915) },
        { "title_progressgauge", (183931505, 2819375915) },
        { "title_revision", (4061876029, 2819375915) },
        { "title_rights", (3488127831, 2819375915) },
        { "title_version", (1068551473, 2819375915) },
        { "title_worldmap", (2894123310, 2819375915) },
        { "title_worldmapbg", (923242752, 2819375915) },
        { "titlelicenseviewer", (529253918, 2819375915) },
        { "todocontents", (2966751228, 2819375915) },
        { "todocruising", (522733460, 2819375915) },
        { "todolist", (2991750500, 2819375915) },
        { "todoquest", (3668117325, 2819375915) },
        { "tooltips", (3906289270, 2819375915) },
        { "tourismmenu", (2328394944, 2819375915) },
        { "trade", (620424729, 2819375915) },
        { "trademultiple", (3346451579, 2819375915) },
        { "transport", (3753608746, 2819375915) },
        { "transportporter", (243320621, 2819375915) },
        { "transportteleport", (2639232136, 2819375915) },
        { "transportteleporter", (3526905023, 2819375915) },
        { "treasurechallenge", (62500668, 2819375915) },
        { "treasuremap", (2139332862, 2819375915) },
        { "tripletriad", (208625050, 2819375915) },
        { "tripletriadapplication", (3015955402, 2819375915) },
        { "tripletriaddeckconfirmation", (3595569101, 2819375915) },
        { "tripletriaddeckselect", (455631364, 2819375915) },
        { "tripletriadpickupdeckselect", (1014503393, 2819375915) },
        { "tripletriadplayerinfo", (369724327, 2819375915) },
        { "tripletriadranking", (2544436793, 2819375915) },
        { "tripletriadresult", (2682243670, 2819375915) },
        { "tripletriadroundresult", (2139453209, 2819375915) },
        { "tripletriadrule", (4091788037, 2819375915) },
        { "tripletriadruleannounce", (3449662888, 2819375915) },
        { "tripletriadrulesetting", (2956408771, 2819375915) },
        { "tripletriadtournamentplayer", (3491065151, 2819375915) },
        { "tripletriadtournamentreport", (1573868699, 2819375915) },
        { "tripletriadtournamentresult", (691204883, 2819375915) },
        { "tripletriadtournamentreward", (1742057821, 2819375915) },
        { "tripletriadtournamentschedule", (3175711163, 2819375915) },
        { "turnbreak", (2787881736, 2819375915) },
        { "turnbreakgame", (2304179008, 2819375915) },
        { "turnbreakresult", (2550928252, 2819375915) },
        { "turnbreaktitle", (1198173426, 2819375915) },
        { "tutorialcontents", (2876625897, 2819375915) },
        { "userpolicyperformance", (1992424707, 2819375915) },
        { "vasesetting", (3914227235, 2819375915) },
        { "votekick", (122348380, 2819375915) },
        { "votekickdialogue", (3223050307, 2819375915) },
        { "votemvp", (3369035058, 2819375915) },
        { "votetreasure", (2386130948, 2819375915) },
        { "vvdactionselect", (2810556923, 2819375915) },
        { "vvdfinder", (489331751, 2819375915) },
        { "vvdnotebook", (1380193753, 2819375915) },
        { "warning", (4042391786, 2819375915) },
        { "weatherreport", (2507772652, 2819375915) },
        { "webguidance", (2774057683, 2819375915) },
        { "weblauncher", (1194696868, 2819375915) },
        { "weblink", (3524003997, 2819375915) },
        { "wedding", (3070461859, 2819375915) },
        { "weddingnotification", (1824414790, 2819375915) },
        { "weeklybingo", (1592445476, 2819375915) },
        { "weeklybingobonusinfo", (2109467371, 2819375915) },
        { "weeklybingoresult", (1770454916, 2819375915) },
        { "weeklypuzzle", (2403338391, 2819375915) },
        { "weeklypuzzleresult", (3364914050, 2819375915) },
        { "workshopsupply", (1511376508, 2819375915) },
        { "workshopsupply2", (1896115527, 2819375915) },
        { "workshopsupply3", (1281639671, 2819375915) },
        { "workshopsupplyresult", (2510763570, 2819375915) },
        { "worldtranslate", (276532555, 2819375915) },
        { "worldtranslateconfirm", (3550228394, 2819375915) },
        { "worldtranslatestatus", (1600548852, 2819375915) },
    };

    private int selectedUld;
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "uld" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "ULD";
    
    /// <inheritdoc/>
    public bool Ready { get; set; }
    
    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }
    
    /// <inheritdoc/>
    public void Draw()
    {
        ImGui.Combo("Select Uld", ref this.selectedUld, [.. UldHashLookup.Keys], UldHashLookup.Count);

        var dataManager = Service<DataManager>.Get();
        var textureManager = Service<TextureManager>.Get();

        var uld = this.GetUldFromIndex(dataManager);

        if (uld == null)
        {
            ImGui.Text("Failed to load ULD file.");
            return;
        }

        if (!ImGui.BeginTable("##uldTextureEntries", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
            return;
        ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();

        foreach (var textureEntry in uld.AssetData)
            this.DrawTextureEntry(textureEntry);

        ImGui.EndTable();

        if (ImGui.CollapsingHeader("Timelines"))
        {
            ImGui.Columns(2);
            foreach (var uldTimeline in uld.Timelines)
                this.DrawTimelines(uldTimeline);
            ImGui.Columns(1);
        }

        foreach (var partsData in uld.Parts)
            this.DrawParts(partsData, uld.AssetData, dataManager, textureManager);
    }

    private unsafe void DrawTextureEntry(UldRoot.TextureEntry textureEntry)
    {
        ImGui.TableNextColumn();
        fixed (char* p = textureEntry.Path)
            ImGui.TextUnformatted(new string(p));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(textureEntry.Id.ToString());
    }

    private void DrawTimelines(UldRoot.Timeline timeline)
    {
        if (ImGui.CollapsingHeader($"Uld Timeline {timeline.Id}"))
        {
            foreach (var frameData in timeline.FrameData)
            {
                ImGui.TextUnformatted($"FrameInfo: {frameData.StartFrame} -> {frameData.EndFrame}");
                ImGui.Indent();
                foreach (var frameDataKeyGroup in frameData.KeyGroups)
                {
                    ImGui.TextUnformatted($"{frameDataKeyGroup.Usage:G} {frameDataKeyGroup.Type:G}");
                    foreach (var keyframe in frameDataKeyGroup.Frames)
                        this.DrawTimelineKeyGroupFrame(keyframe);
                }

                ImGui.Unindent();
            }
        }

        ImGui.NextColumn();
    }

    private void DrawTimelineKeyGroupFrame(IKeyframe frame)
    {
        switch (frame)
        {
            case BaseKeyframeData baseKeyframeData:
                ImGui.TextUnformatted($"Time: {baseKeyframeData.Time} | Interpolation: {baseKeyframeData.Interpolation} | Acceleration: {baseKeyframeData.Acceleration} | Deceleration: {baseKeyframeData.Deceleration}");
                break;
            case Float1Keyframe float1Keyframe:
                this.DrawTimelineKeyGroupFrame(float1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {float1Keyframe.Value}");
                break;
            case Float2Keyframe float2Keyframe:
                this.DrawTimelineKeyGroupFrame(float2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {float2Keyframe.Value[0]} | Value2: {float2Keyframe.Value[1]}");
                break;
            case Float3Keyframe float3Keyframe:
                this.DrawTimelineKeyGroupFrame(float3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {float3Keyframe.Value[0]} | Value2: {float3Keyframe.Value[1]} | Value3: {float3Keyframe.Value[2]}");
                break;
            case SByte1Keyframe sbyte1Keyframe:
                this.DrawTimelineKeyGroupFrame(sbyte1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {sbyte1Keyframe.Value}");
                break;
            case SByte2Keyframe sbyte2Keyframe:
                this.DrawTimelineKeyGroupFrame(sbyte2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {sbyte2Keyframe.Value[0]} | Value2: {sbyte2Keyframe.Value[1]}");
                break;
            case SByte3Keyframe sbyte3Keyframe:
                this.DrawTimelineKeyGroupFrame(sbyte3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {sbyte3Keyframe.Value[0]} | Value2: {sbyte3Keyframe.Value[1]} | Value3: {sbyte3Keyframe.Value[2]}");
                break;
            case Byte1Keyframe byte1Keyframe:
                this.DrawTimelineKeyGroupFrame(byte1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {byte1Keyframe.Value}");
                break;
            case Byte2Keyframe byte2Keyframe:
                this.DrawTimelineKeyGroupFrame(byte2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {byte2Keyframe.Value[0]} | Value2: {byte2Keyframe.Value[1]}");
                break;
            case Byte3Keyframe byte3Keyframe:
                this.DrawTimelineKeyGroupFrame(byte3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {byte3Keyframe.Value[0]} | Value2: {byte3Keyframe.Value[1]} | Value3: {byte3Keyframe.Value[2]}");
                break;
            case Short1Keyframe short1Keyframe:
                this.DrawTimelineKeyGroupFrame(short1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {short1Keyframe.Value}");
                break;
            case Short2Keyframe short2Keyframe:
                this.DrawTimelineKeyGroupFrame(short2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {short2Keyframe.Value[0]} | Value2: {short2Keyframe.Value[1]}");
                break;
            case Short3Keyframe short3Keyframe:
                this.DrawTimelineKeyGroupFrame(short3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {short3Keyframe.Value[0]} | Value2: {short3Keyframe.Value[1]} | Value3: {short3Keyframe.Value[2]}");
                break;
            case UShort1Keyframe ushort1Keyframe:
                this.DrawTimelineKeyGroupFrame(ushort1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {ushort1Keyframe.Value}");
                break;
            case UShort2Keyframe ushort2Keyframe:
                this.DrawTimelineKeyGroupFrame(ushort2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {ushort2Keyframe.Value[0]} | Value2: {ushort2Keyframe.Value[1]}");
                break;
            case UShort3Keyframe ushort3Keyframe:
                this.DrawTimelineKeyGroupFrame(ushort3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {ushort3Keyframe.Value[0]} | Value2: {ushort3Keyframe.Value[1]} | Value3: {ushort3Keyframe.Value[2]}");
                break;
            case Int1Keyframe int1Keyframe:
                this.DrawTimelineKeyGroupFrame(int1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {int1Keyframe.Value}");
                break;
            case Int2Keyframe int2Keyframe:
                this.DrawTimelineKeyGroupFrame(int2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {int2Keyframe.Value[0]} | Value2: {int2Keyframe.Value[1]}");
                break;
            case Int3Keyframe int3Keyframe:
                this.DrawTimelineKeyGroupFrame(int3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {int3Keyframe.Value[0]} | Value2: {int3Keyframe.Value[1]} | Value3: {int3Keyframe.Value[2]}");
                break;
            case UInt1Keyframe uint1Keyframe:
                this.DrawTimelineKeyGroupFrame(uint1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {uint1Keyframe.Value}");
                break;
            case UInt2Keyframe uint2Keyframe:
                this.DrawTimelineKeyGroupFrame(uint2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {uint2Keyframe.Value[0]} | Value2: {uint2Keyframe.Value[1]}");
                break;
            case UInt3Keyframe uint3Keyframe:
                this.DrawTimelineKeyGroupFrame(uint3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {uint3Keyframe.Value[0]} | Value2: {uint3Keyframe.Value[1]} | Value3: {uint3Keyframe.Value[2]}");
                break;
            case Bool1Keyframe bool1Keyframe:
                this.DrawTimelineKeyGroupFrame(bool1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {bool1Keyframe.Value}");
                break;
            case Bool2Keyframe bool2Keyframe:
                this.DrawTimelineKeyGroupFrame(bool2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {bool2Keyframe.Value[0]} | Value2: {bool2Keyframe.Value[1]}");
                break;
            case Bool3Keyframe bool3Keyframe:
                this.DrawTimelineKeyGroupFrame(bool3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {bool3Keyframe.Value[0]} | Value2: {bool3Keyframe.Value[1]} | Value3: {bool3Keyframe.Value[2]}");
                break;
            case ColorKeyframe colorKeyframe:
                this.DrawTimelineKeyGroupFrame(colorKeyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Add: {colorKeyframe.AddRed} {colorKeyframe.AddGreen} {colorKeyframe.AddBlue} | Multiply: {colorKeyframe.MultiplyRed} {colorKeyframe.MultiplyGreen} {colorKeyframe.MultiplyBlue}");
                break;
            case LabelKeyframe labelKeyframe:
                this.DrawTimelineKeyGroupFrame(labelKeyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | LabelCommand: {labelKeyframe.LabelCommand} | JumpId: {labelKeyframe.JumpId} | LabelId: {labelKeyframe.LabelId}");
                break;
        }
    }

    private unsafe void DrawParts(UldRoot.PartsData partsData, UldRoot.TextureEntry[] textureEntries, DataManager dataManager, TextureManager textureManager)
    {
        if (ImGui.CollapsingHeader($"Parts {partsData.Id}"))
        {
            for (var index = 0; index < partsData.Parts.Length; index++)
            {
                var partsDataPart = partsData.Parts[index];
                var texturePathChars = textureEntries.First(t => t.Id == partsDataPart.TextureId).Path;
                string texturePath;
                fixed (char* p = texturePathChars)
                    texturePath = new string(p);
                var texFile = dataManager.GetFile<TexFile>(texturePath);
                if (texFile == null) continue;
                var wrap = textureManager.CreateFromTexFile(texFile);
                var texSize = new Vector2(texFile.Header.Width, texFile.Header.Height);
                var uv0 = new Vector2(partsDataPart.U, partsDataPart.V);
                var partSize = new Vector2(partsDataPart.W, partsDataPart.H);
                var uv1 = uv0 + partSize;
                ImGui.TextUnformatted($"Index: {index}");
                ImGui.SameLine();
                ImGui.Image(wrap.ImGuiHandle, partSize, uv0 / texSize, uv1 / texSize);
                wrap.Dispose();
            }
        }
    }

    private UldFile? GetUldFromIndex(DataManager dataManager)
    {
        if (dataManager.GameData.Repositories.TryGetValue("ffxiv", out Repository repo))
        {
            var uld = UldHashLookup.ElementAt(this.selectedUld);
            var hash = uld.Value.FileHash | ((ulong)uld.Value.FolderHash << 32);

            var categories = repo.Categories[Repository.CategoryNameToIdMap["ui"]];
            foreach (var category in categories)
            {
                var file = category.GetFile<UldFile>(hash);
                if (file != null)
                    return file;
            }
        }

        return null;
    }
}
