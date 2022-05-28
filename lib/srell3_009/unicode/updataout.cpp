//
//  updataout.cpp: version 1.400 (2021/10/10).
//
//  This is a program that generates srell_updata.hpp from:
//    DerivedCoreProperties.txt
//    DerivedNormalizationProps.txt
//    emoji-data.txt
//    PropList.txt
//    ScriptExtensions.txt
//    Scripts.txt
//    UnicodeData.txt
//  provided by the Unicode Consortium. The latese versions of them
//  except emoji-data.txt are available at: http://www.unicode.org/Public/UNIDATA/
//  emoji-data.txt is available at: http://www.unicode.org/Public/UNIDATA/emoji/
//

#include <cstdio>
#include <cstdlib>
#include <cstdarg>
#include <string>
#include <vector>
#include <map>
#include <stdexcept>
#include "../srell.hpp"

#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(disable:4996)
#endif

namespace updata
{
static const char *const property_names[] = {	//  3
	"General_Category:gc", "Script:sc", "Script_Extensions:scx", ""
};
static const char *const binary_property_names[] = {	//  53 (52+1)
	//  *1: http://unicode.org/reports/tr18/#General_Category_Property
	//  *2: 9th field in UnicodeData.txt
	"ASCII",								//  *1
	"ASCII_Hex_Digit:AHex",					//  PropList.txt
	"Alphabetic:Alpha",						//  DerivedCoreProperties.txt
	"Any",									//  *1
	"Assigned",								//  *1
	"Bidi_Control:Bidi_C",					//  PropList.txt
	"Bidi_Mirrored:Bidi_M",					//  *2
	"Case_Ignorable:CI",					//  DerivedCoreProperties.txt
	"Cased",								//  DerivedCoreProperties.txt
	"Changes_When_Casefolded:CWCF",			//  DerivedCoreProperties.txt
	"Changes_When_Casemapped:CWCM",			//  DerivedCoreProperties.txt
	"Changes_When_Lowercased:CWL",			//  DerivedCoreProperties.txt
	"Changes_When_NFKC_Casefolded:CWKCF",	//  DerivedNormalizationProps.txt
	"Changes_When_Titlecased:CWT",			//  DerivedCoreProperties.txt
	"Changes_When_Uppercased:CWU",			//  DerivedCoreProperties.txt
	"Dash",									//  PropList.txt
	"Default_Ignorable_Code_Point:DI",		//  DerivedCoreProperties.txt
	"Deprecated:Dep",						//  PropList.txt
	"Diacritic:Dia",						//  PropList.txt
	"Emoji",								//  emoji-data.txt
	"Emoji_Component:EComp",				//  emoji-data.txt
	"Emoji_Modifier:EMod",					//  emoji-data.txt
	"Emoji_Modifier_Base:EBase",			//  emoji-data.txt
	"Emoji_Presentation:EPres",				//  emoji-data.txt
	"Extended_Pictographic:ExtPict",		//  emoji-data.txt
	"Extender:Ext",							//  PropList.txt
	"Grapheme_Base:Gr_Base",				//  DerivedCoreProperties.txt
	"Grapheme_Extend:Gr_Ext",				//  DerivedCoreProperties.txt
	"Hex_Digit:Hex",						//  PropList.txt
	"IDS_Binary_Operator:IDSB",				//  PropList.txt
	"IDS_Trinary_Operator:IDST",			//  PropList.txt
	"ID_Continue:IDC",						//  DerivedCoreProperties.txt
	"ID_Start:IDS",							//  DerivedCoreProperties.txt
	"Ideographic:Ideo",						//  PropList.txt
	"Join_Control:Join_C",					//  PropList.txt
	"Logical_Order_Exception:LOE",			//  PropList.txt
	"Lowercase:Lower",						//  DerivedCoreProperties.txt
	"Math",									//  DerivedCoreProperties.txt
	"Noncharacter_Code_Point:NChar",		//  PropList.txt
	"Pattern_Syntax:Pat_Syn",				//  PropList.txt
	"Pattern_White_Space:Pat_WS",			//  PropList.txt
	"Quotation_Mark:QMark",					//  PropList.txt
	"Radical",								//  PropList.txt
	"Regional_Indicator:RI",				//  PropList.txt
	"Sentence_Terminal:STerm",				//  PropList.txt
	"Soft_Dotted:SD",						//  PropList.txt
	"Terminal_Punctuation:Term",			//  PropList.txt
	"Unified_Ideograph:UIdeo",				//  PropList.txt
	"Uppercase:Upper",						//  DerivedCoreProperties.txt
	"Variation_Selector:VS",				//  PropList.txt
	"White_Space:space",					//  PropList.txt
	"XID_Continue:XIDC",					//  DerivedCoreProperties.txt
	"XID_Start:XIDS",						//  DerivedCoreProperties.txt
	//  ECMAScript 2019/Unicode 11:
	//    "Extended_Pictographic:ExtPict",
	//  ECMAScript 2021/Unicode 13:
	//    Aliases: EComp, EMod, EBase, EPres, and ExtPict
	""
};
static const char *const gc_values[] = {	//  38
	"Other:C", "Control:Cc:cntrl", "Format:Cf", "Unassigned:Cn",
	"Private_Use:Co", "Surrogate:Cs", "Letter:L", "Cased_Letter:LC",
	"Lowercase_Letter:Ll", "Titlecase_Letter:Lt", "Uppercase_Letter:Lu", "Modifier_Letter:Lm",
	"Other_Letter:Lo", "Mark:M:Combining_Mark", "Spacing_Mark:Mc", "Enclosing_Mark:Me",
	"Nonspacing_Mark:Mn", "Number:N", "Decimal_Number:Nd:digit", "Letter_Number:Nl",
	"Other_Number:No", "Punctuation:P:punct", "Connector_Punctuation:Pc", "Dash_Punctuation:Pd",
	"Close_Punctuation:Pe", "Final_Punctuation:Pf", "Initial_Punctuation:Pi", "Other_Punctuation:Po",
	"Open_Punctuation:Ps", "Symbol:S", "Currency_Symbol:Sc", "Modifier_Symbol:Sk",
	"Math_Symbol:Sm", "Other_Symbol:So", "Separator:Z", "Line_Separator:Zl",
	"Paragraph_Separator:Zp", "Space_Separator:Zs", ""
};
static const char *const script_names[] = {	//  161 (141+7+4+4+5)
	"Adlam:Adlm", "Ahom:Ahom", "Anatolian_Hieroglyphs:Hluw", "Arabic:Arab",
	"Armenian:Armn", "Avestan:Avst", "Balinese:Bali", "Bamum:Bamu",
	"Bassa_Vah:Bass", "Batak:Batk", "Bengali:Beng", "Bhaiksuki:Bhks",
	"Bopomofo:Bopo", "Brahmi:Brah", "Braille:Brai", "Buginese:Bugi",
	"Buhid:Buhd", "Canadian_Aboriginal:Cans", "Carian:Cari", "Caucasian_Albanian:Aghb",
	"Chakma:Cakm", "Cham:Cham", "Cherokee:Cher", "Chorasmian:Chrs",
	"Common:Zyyy", "Coptic:Copt:Qaac", "Cypro_Minoan:Cpmn", "Cuneiform:Xsux",
	"Cypriot:Cprt", "Cyrillic:Cyrl", "Deseret:Dsrt", "Devanagari:Deva",
	"Dives_Akuru:Diak", "Dogra:Dogr", "Duployan:Dupl", "Egyptian_Hieroglyphs:Egyp",
	"Elbasan:Elba", "Elymaic:Elym", "Ethiopic:Ethi", "Georgian:Geor",
	"Glagolitic:Glag", "Gothic:Goth", "Grantha:Gran", "Greek:Grek",
	"Gujarati:Gujr", "Gunjala_Gondi:Gong", "Gurmukhi:Guru", "Han:Hani",
	"Hangul:Hang", "Hanifi_Rohingya:Rohg", "Hanunoo:Hano", "Hatran:Hatr",
	"Hebrew:Hebr", "Hiragana:Hira", "Imperial_Aramaic:Armi", "Inherited:Zinh:Qaai",
	"Inscriptional_Pahlavi:Phli", "Inscriptional_Parthian:Prti", "Javanese:Java", "Kaithi:Kthi",
	"Kannada:Knda", "Katakana:Kana", "Kayah_Li:Kali", "Kharoshthi:Khar",
	"Khitan_Small_Script:Kits", "Khmer:Khmr", "Khojki:Khoj", "Khudawadi:Sind",
	"Lao:Laoo", "Latin:Latn", "Lepcha:Lepc", "Limbu:Limb",
	"Linear_A:Lina", "Linear_B:Linb", "Lisu:Lisu", "Lycian:Lyci",
	"Lydian:Lydi", "Mahajani:Mahj", "Makasar:Maka", "Malayalam:Mlym",
	"Mandaic:Mand", "Manichaean:Mani", "Marchen:Marc", "Masaram_Gondi:Gonm",
	"Medefaidrin:Medf", "Meetei_Mayek:Mtei", "Mende_Kikakui:Mend", "Meroitic_Cursive:Merc",
	"Meroitic_Hieroglyphs:Mero", "Miao:Plrd", "Modi:Modi", "Mongolian:Mong",
	"Mro:Mroo", "Multani:Mult", "Myanmar:Mymr", "Nabataean:Nbat",
	"Nandinagari:Nand", "New_Tai_Lue:Talu", "Newa:Newa", "Nko:Nkoo",
	"Nushu:Nshu", "Nyiakeng_Puachue_Hmong:Hmnp", "Ogham:Ogam", "Ol_Chiki:Olck",
	"Old_Hungarian:Hung", "Old_Italic:Ital", "Old_North_Arabian:Narb", "Old_Permic:Perm",
	"Old_Persian:Xpeo", "Old_Sogdian:Sogo", "Old_South_Arabian:Sarb", "Old_Turkic:Orkh",
	"Old_Uyghur:Ougr", "Oriya:Orya", "Osage:Osge", "Osmanya:Osma",
	"Pahawh_Hmong:Hmng", "Palmyrene:Palm", "Pau_Cin_Hau:Pauc", "Phags_Pa:Phag",
	"Phoenician:Phnx", "Psalter_Pahlavi:Phlp", "Rejang:Rjng", "Runic:Runr",
	"Samaritan:Samr", "Saurashtra:Saur", "Sharada:Shrd", "Shavian:Shaw",
	"Siddham:Sidd", "SignWriting:Sgnw", "Sinhala:Sinh", "Sogdian:Sogd",
	"Sora_Sompeng:Sora", "Soyombo:Soyo", "Sundanese:Sund", "Syloti_Nagri:Sylo",
	"Syriac:Syrc", "Tagalog:Tglg", "Tagbanwa:Tagb", "Tai_Le:Tale",
	"Tai_Tham:Lana", "Tai_Viet:Tavt", "Takri:Takr", "Tamil:Taml",
	"Tangsa:Tnsa", "Tangut:Tang", "Telugu:Telu", "Thaana:Thaa",
	"Thai:Thai", "Tibetan:Tibt", "Tifinagh:Tfng", "Tirhuta:Tirh",
	"Toto", "Ugaritic:Ugar", "Vai:Vaii", "Vithkuqi:Vith",
	"Wancho:Wcho", "Warang_Citi:Wara", "Yezidi:Yezi", "Yi:Yiii",
	"Zanabazar_Square:Zanb",
	//  ECMAScript 2019/Unicode 11:
	//    "Dogra:Dogr", "Gunjala_Gondi:Gong", "Hanifi_Rohingya:Rohg",
	//    "Makasar:Maka", "Medefaidrin:Medf", "Old_Sogdian:Sogo", "Sogdian:Sogd",
	//  ECMAScript 2020/Unicode 12
	//    "Elymaic:Elym", "Nandinagari:Nand", "Nyiakeng_Puachue_Hmong:Hmnp", "Wancho:Wcho",
	//  ECMAScript 2021/Unicode 13
	//    "Chorasmian:Chrs", "Dives_Akuru:Diak", "Khitan_Small_Script:Kits", "Yezidi:Yezi",
	//  ECMAScript 2022/Unicode 14:
	//    "Cypro_Minoan:Cpmn", "Old_Uyghur:Ougr", "Tangsa:Tnsa", "Toto",
	//    "Vithkuqi:Vith"
	""
};
}	//  namespace updata

namespace unishared
{
template <const std::size_t BufSize, typename Type>
std::string stringify(const Type value, const char *const fmt)
{
	char buffer[BufSize];
	std::sprintf(buffer, fmt, value);
	return std::string(buffer);
}

void throw_error(const char *const s, ...)
{
	char buffer[256];

	va_list va;
	va_start(va, s);
	std::vsprintf(buffer, s, va);
	va_end(va);
	throw std::runtime_error(buffer);
}

void read_file(std::string &str, const char *const filename, const char *const dir)
{
	const std::string path(std::string(dir ? dir : "") + filename);
	FILE *const fp = std::fopen(path.c_str(), "r");

	std::fprintf(stdout, "Reading '%s'... ", path.c_str());

	if (fp)
	{
		static const std::size_t bufsize = 4096;
		char *const buffer = static_cast<char *>(std::malloc(bufsize));

		if (buffer)
		{
			for (;;)
			{
				const std::size_t size = std::fread(buffer, 1, bufsize, fp);

				if (!size)
					break;

				str.append(buffer, size);
			}
			std::fclose(fp);
			std::fputs("done.\n", stdout);
			std::free(buffer);
			return;
		}
	}
	std::fputs("failed...", stdout);
	throw_error("could not open!");
}

bool write_file(const char *const filename, const std::string &str)
{
	FILE *const fp = std::fopen(filename, "wb");

	std::fprintf(stdout, "Writing '%s'... ", filename);

	if (fp)
	{
		const bool success = std::fwrite(str.c_str(), 1, str.size(), fp) == str.size();
		std::fclose(fp);
		if (success)
		{
			std::fputs("done.\n", stdout);
			return true;
		}
	}
	std::fputs("failed...\n", stdout);
	return false;
}
}	//  namespace unishared

struct up_options
{
	const char *outfilename;
	const char *indir;
	int version;
	int errorno;

	up_options(const int argc, const char *const *const argv)
		: outfilename("srell_updata.hpp")
		, indir("")
		, version(110)
		, errorno(0)
	{
		for (int index = 1; index < argc; ++index)
		{
			const char firstchar = argv[index][0];

			if (firstchar == '-' || firstchar == '/')
			{
				const char *const option = argv[index] + 1;

				if (std::strcmp(option, "o") == 0)
				{
					if (index + 1 >= argc)
						goto NO_ARGUMENT;
					outfilename = argv[++index];
				}
				else if (std::strcmp(option, "v") == 0)
				{
					if (index + 1 >= argc)
						goto NO_ARGUMENT;
					version = static_cast<int>(std::strtod(argv[++index], NULL) * 100.0);
				}
				else if (std::strcmp(option, "id") == 0)
				{
					if (index + 1 >= argc)
						goto NO_ARGUMENT;
					indir = argv[++index];
				}
				else
					goto UNKNOWN_OPTION;

				continue;

				NO_ARGUMENT:
				std::fprintf(stdout, "[Error] no argument for \"%s\" specified.\n", argv[index]);
				errorno = -2;
			}
			else
			{
				UNKNOWN_OPTION:
				std::fprintf(stdout, "[Error] unknown option \"%s\" found.\n", argv[index]);
				errorno = -1;
			}
		}
	}
};
//  struct up_options

class unicode_property
{
public:

	unicode_property()
		: re_licenseline_("^#\\s*(.*)$")
		, re_licenseend_("^#\\s*$")
	{
	}

	int create_updata(std::string &outdata, const up_options &opts)
	{
		int errorno = opts.errorno;
		const char *const unidatafilename = "UnicodeData.txt";
		const char *const propdatafiles[] = { "PropList.txt", "DerivedCoreProperties.txt", "emoji-data.txt", "DerivedNormalizationProps.txt", "" };
		const char *const scfilename = "Scripts.txt";
		const char *const scxfilename = "ScriptExtensions.txt";
		std::string licensetext;
		rangeholder general_category_values;
		rangeholder binary_properties;
		rangeholder scripts;
		rangeholder scriptextensions;
		sortedrangeholder combined_properties;
//		scriptnameholder ucs_to_scriptname;	//  codepoint->scriptname.

		if (errorno)
			return errorno;

		try
		{
			licensetext = "//  ";
			licensetext += unidatafilename;
			licensetext += "\n//\n";

			read_unidata(general_category_values, binary_properties, unidatafilename, opts.indir);
			set_additionalbinprops(binary_properties, general_category_values);	//  for ASCII, Any, Cn.
			create_compositecategories(general_category_values);	//  This needs "Cn".

			read_binprops(binary_properties, licensetext, propdatafiles, opts.indir);

			read_scripts(scripts, /* ucs_to_scriptname, */ licensetext, scfilename, opts.indir);

			scriptextensions = scripts;
			modify_for_scx(scriptextensions, /* ucs_to_scriptname, */ licensetext, scxfilename, opts.indir);

			combine_properties(combined_properties, general_category_values, "gc", updata::gc_values);
			combine_properties(combined_properties, binary_properties, "bp", updata::binary_property_names);
			combine_properties(combined_properties, scripts, "sc", updata::script_names);
			combine_properties(combined_properties, scriptextensions, "scx", updata::script_names);
			do_formatting(outdata, combined_properties, opts.version);

			licensetext.append(1, '\n');
			outdata.insert(0, licensetext);
		}
		catch (std::runtime_error &e)
		{
			std::printf("\nError: %s\n", e.what());
			errorno = 1;
		}
		return errorno;
	}

private:

	typedef srell::regex_internal::uchar32 uchar32;
	typedef srell::regex_internal::range_pairs ucprange_array;
	typedef srell::regex_internal::range_pair ucprange;
	typedef srell::regex_internal::range_pair_helper ucprange_helper;
	typedef std::map<std::string, ucprange_array> rangeholder;
//	typedef srell::regex_internal::bitset<0x110000> ucsset;
	typedef std::vector<std::string> strings_type;
	typedef std::map<uchar32, std::string> scriptnameholder;
	typedef std::map<std::string, std::string> name_mapper;
	typedef name_mapper canonicalname_mapper;
	static const uchar32 invalid_u32value = srell::regex_internal::constants::invalid_u32value;
	static const uchar32 compositeclass = invalid_u32value;

	struct sorted_name_and_ranges
	{
		std::string ptype;
		std::string canonicalname;
		std::string namealiases;
		ucprange_array ucpranges;
	};
	typedef std::vector<sorted_name_and_ranges> sortedrangeholder;

	void split(strings_type &parts, const std::string &data, const char splitter)
	{
		std::string::size_type readpos = 0;

		for (;;)
		{
			std::string::size_type lineend = data.find(splitter, readpos);

			if (lineend == std::string::npos)
			{
//				if (readpos == data.size())
//					break;

				parts.push_back(data.substr(readpos, data.size() - readpos));
				break;
			}

			parts.push_back(data.substr(readpos, lineend - readpos));
			++lineend;
			readpos = lineend;
		}
	}

	std::string join(const char c, const strings_type &parts, const bool add_final_also = false)
	{
		std::string out;

		for (strings_type::size_type i = 0; i < parts.size(); ++i)
			out.append(parts[i] + c);

		if (!add_final_also && out.size())
			out.resize(out.size() - 1);

		return out;
	}

	void read_unidata(rangeholder &gc, rangeholder &bp, const char *const unidatafilename, const char *const indir)
	{
		const srell::regex re_dataline("^([0-9A-F]+);([^;]*);(([^;]*);(?:[^;]*;){6}([^;]*)(?:;[^;]*){5})$");
		const srell::regex re_rangefirst("^<(.*), First>$");

		const std::string stringY("Y");
		const std::string stringN("N");
		uchar32 prevucp = invalid_u32value;
		std::string data;
		strings_type lines;
		srell::cmatch cmatch;
//		strings_type parts;
		std::string rangename;
		std::string rangefirstproperty;
		uchar32 rangefirstcp = 0;
		ucprange range;
		ucprange_array bidi_mirrored_ranges;

		unishared::read_file(data, unidatafilename, indir);
		split(lines, data, '\n');

		for (strings_type::size_type i = 0; i < lines.size(); ++i)
		{
			const std::string &line = lines[i];
			const char *const lineend = line.c_str() + line.size();

			if (srell::regex_match(line.c_str(), lineend, cmatch, re_dataline))
			{
				const srell::cmatch::value_type &codepoint = cmatch[1];
				const srell::cmatch::value_type &name = cmatch[2];
				const std::string name_string(name.str());
				const std::string property(cmatch[3].str());

				range.first = range.second = static_cast<uchar32>(std::strtol(codepoint.first, NULL, 16));

				if (prevucp >= range.first && prevucp != invalid_u32value)
					unishared::throw_error("out of order: %.4lX >= %.4lX", prevucp, range.first);

//				parts.clear();
//				split(parts, property, ';');
//				if (parts.size() != 13)
//					unishared::throw_error("number of fields is not 13, but %u\n\t[%s]", parts.size(), line.c_str());

//				const std::string &general_category = parts[0];
//				const std::string &bidi_mirrored = parts[7];
				const std::string general_category(cmatch[4].str());
				const std::string bidi_mirrored(cmatch[5].str());

				prevucp = range.first;

				if (rangename.size())
				{
					if (name_string.compare("<" + rangename + ", Last>") != 0)
						unishared::throw_error("<%s, Last> does not follow its First line.\n\t%s follows insteadly.", rangename.c_str(), name_string.c_str());

					if (property != rangefirstproperty)
					{
						unishared::throw_error("\"%s\": properties of First and Last are different.\n\tFirst: %s\n\tLast:  %s", rangename.c_str(), rangefirstproperty.c_str(), property.c_str());
					}

					range.first = rangefirstcp;
					rangename.clear();
				}
				else if (srell::regex_match(name.first, name.second, cmatch, re_rangefirst))
				{
					rangename = cmatch[1];
					rangefirstproperty = property;
					rangefirstcp = range.first;
					continue;
				}

				//  register "general_category" value.
				gc[general_category].join(range);

				//  register "bidi_mirrored" value.
				if (bidi_mirrored == stringY)
				{
					bidi_mirrored_ranges.join(range);
				}
				else if (bidi_mirrored != stringN)
					unishared::throw_error("unknown Bidi_Mirrored value [%s] in %s.", bidi_mirrored.c_str(), line.c_str());
			}
			else if (line.size())
				unishared::throw_error("unknown format [%s]", line.c_str());
		}
		bp["Bidi_Mirrored"] = bidi_mirrored_ranges;
	}

	//  binary properties created from UnicodeData.txt.
	void set_additionalbinprops(rangeholder &bp, rangeholder &gc)
	{
		ucprange_array assigned_ranges;

		for (rangeholder::iterator it = gc.begin(); it != gc.end(); ++it)
			assigned_ranges.merge(it->second);

		bp["Any"].join(ucprange_helper(0x0000, 0x10ffff));
		bp["ASCII"].join(ucprange_helper(0x0000, 0x007f));
		bp["Assigned"];	//  Only creates. No data.

//		bp["Assigned"] = assigned_ranges;
		assigned_ranges.negation();
		gc["Cn"] = assigned_ranges;
	}

	void create_compositecategory(rangeholder &gc, const char *const newname, const char *const *categories)
	{
		ucprange_array array;
		uchar32 total = 0;

		array.append_newpair(ucprange_helper(compositeclass, 0));

		for (; **categories; ++categories)
		{
			const char *const c = *categories;
			const uchar32 count = static_cast<uchar32>(gc[*categories].size());

			array.append_newpair(ucprange_helper(c[0], c[1]));
			array.append_newpair(ucprange_helper(count, 0));
			total += count;
		}
		array[0].second = total;
		gc[newname] = array;
	}

	void create_compositecategories(rangeholder &gc)
	{
		const char *const categoryLC[] = { "Ll", "Lt", "Lu", "" };
		const char *const categoryL[] = { "Ll", "Lt", "Lu", "Lm", "Lo", "" };
		const char *const categoryM[] = { "Mc", "Me", "Mn", "" };
		const char *const categoryN[] = { "Nd", "Nl", "No", "" };
		const char *const categoryC[] = { "Cc", "Cf", "Cn", "Co", "Cs", "" };
		const char *const categoryP[] = { "Pc", "Pd", "Pe", "Pf", "Pi", "Po", "Ps", "" };
		const char *const categoryZ[] = { "Zl", "Zp", "Zs", "" };
		const char *const categoryS[] = { "Sc", "Sk", "Sm", "So", "" };

		create_compositecategory(gc, "LC", categoryLC);
		create_compositecategory(gc, "L", categoryL);
		create_compositecategory(gc, "M", categoryM);
		create_compositecategory(gc, "N", categoryN);
		create_compositecategory(gc, "C", categoryC);
		create_compositecategory(gc, "P", categoryP);
		create_compositecategory(gc, "Z", categoryZ);
		create_compositecategory(gc, "S", categoryS);
	}

	void read_files(std::string &out, const char *const *propdatafiles, const char separator, const char *const indir)
	{
		for (; **propdatafiles; ++propdatafiles)
		{
			std::string data;
			unishared::read_file(data, *propdatafiles, indir);
			out.append(data);
			out.append(1, separator);
			out.append(1, '\n');
		}
	}

	void read_binprops(rangeholder &bp, std::string &licensetext, const char *const *propdatafiles, const char *const indir)
	{
		const srell::regex re_propfmt("^\\s*([0-9A-Fa-f]{4,})(?:\\.\\.([0-9A-Fa-f]{4,}))?\\s*;\\s*(\\S+)\\s*(#.*)?$");
		const char separator = '\0';
		bool licenseend_found = false;
		ucprange range;
		strings_type lines;
		srell::cmatch cmatch;

		{
			std::string alldata;
			read_files(alldata, propdatafiles, separator, indir);
			split(lines, alldata, '\n');
		}

		for (std::string::size_type i = 0; i < lines.size(); ++i)
		{
			const std::string &line = lines[i];
			const char *const lineend = line.c_str() + line.size();

			if (!licenseend_found)
			{
				if (srell::regex_match(line.c_str(), lineend, cmatch, re_licenseend_))
				{
					licensetext += "//\n";
					licenseend_found = true;
				}
				else if (srell::regex_match(line.c_str(), lineend, cmatch, re_licenseline_))
				{
					licensetext += "//  " + cmatch[1].str() + '\n';
				}
			}
			else if (line.size() == 1 && line[0] == separator)
			{
				licenseend_found = false;
			}
			else if (srell::regex_match(line.c_str(), lineend, cmatch, re_propfmt))
			{
				const srell::cmatch::value_type &begin = cmatch[1];
				const srell::cmatch::value_type &end = cmatch[2];
				const srell::cmatch::value_type &property = cmatch[3];
//				const srell::cmatch::value_type &comment = cmatch[4];

				range.first = static_cast<uchar32>(std::strtol(begin.first, NULL, 16));
				if (end.matched)
					range.second = static_cast<uchar32>(std::strtol(end.first, NULL, 16));
				else
					range.second = range.first;

				bp[property].join(range);
			}
		}
	}

	void read_scripts(rangeholder &sc, /* scriptnameholder &ucs_to_scriptname, */ std::string &licensetext, const char *const filename, const char *const indir)
	{
		const srell::regex re_scriptdata("^\\s*([0-9A-Fa-f]{4,})(?:\\.\\.([0-9A-Fa-f]{4,}))?\\s*;\\s*(\\S+)\\s*(#.*)?$");
		bool licenseend_found = false;
		ucprange range;
		strings_type lines;
		srell::cmatch cmatch;

		{
			std::string data;

			unishared::read_file(data, filename, indir);
			split(lines, data, '\n');
		}

		for (std::string::size_type i = 0; i < lines.size(); ++i)
		{
			const std::string &line = lines[i];
			const char *const lineend = line.c_str() + line.size();

			if (!licenseend_found)
			{
				if (srell::regex_match(line.c_str(), lineend, cmatch, re_licenseend_))
				{
					licensetext += "//\n";
					licenseend_found = true;
				}
				else if (srell::regex_match(line.c_str(), lineend, cmatch, re_licenseline_))
				{
					licensetext += "//  " + cmatch[1].str() + '\n';
				}
			}
			else if (srell::regex_match(line.c_str(), lineend, cmatch, re_scriptdata))
			{
				const srell::cmatch::value_type &begin = cmatch[1];
				const srell::cmatch::value_type &end = cmatch[2];
				const srell::cmatch::value_type &scriptname = cmatch[3];
//				const srell::cmatch::value_type &comment = cmatch[4];

				range.first = static_cast<uchar32>(std::strtol(begin.first, NULL, 16));
				if (end.matched)
					range.second = static_cast<uchar32>(std::strtol(end.first, NULL, 16));
				else
					range.second = range.first;

				sc[scriptname].join(range);
			}
		}
	}

	canonicalname_mapper load_canonicalnames(const char *const *names)
	{
		canonicalname_mapper canonicalnames;
		strings_type parts;

		for (; **names; ++names)
		{
			parts.clear();
			split(parts, *names, ':');
			const std::string canonicalname(parts[0]);
			for (strings_type::size_type i = 0; i < parts.size(); ++i)
			{
				canonicalnames[parts[i]] = canonicalname;
			}
		}
		return canonicalnames;
	}

	void modify_for_scx(rangeholder &scx, /* scriptnameholder &ucs_to_scriptname, */ std::string &licensetext, const char *const filename, const char *const indir)
	{
		const srell::regex re_scxdata("^\\s*([0-9A-Fa-f]{4,})(?:\\.\\.([0-9A-Fa-f]{4,}))?\\s*;\\s*(\\S.*?\\S)\\s*(#.*)?$");
		const canonicalname_mapper canonicalnames(load_canonicalnames(updata::script_names));
		const std::string name_common("Common");
		const std::string name_inherited("Inherited");
		bool licenseend_found = false;
		ucprange_array common = scx[name_common];
		ucprange_array inherited = scx[name_inherited];
		ucprange range;
		strings_type lines;
		srell::cmatch cmatch;
		strings_type scxparts;
		std::map<std::string, bool> warning_out;

		{
			std::string data;

			unishared::read_file(data, filename, indir);
			split(lines, data, '\n');
		}

		for (std::string::size_type i = 0; i < lines.size(); ++i)
		{
			const std::string &line = lines[i];
			const char *const lineend = line.c_str() + line.size();

			if (!licenseend_found)
			{
				if (srell::regex_match(line.c_str(), lineend, cmatch, re_licenseend_))
				{
					licensetext += "//\n";
					licenseend_found = true;
				}
				else if (srell::regex_match(line.c_str(), lineend, cmatch, re_licenseline_))
				{
					licensetext += "//  " + cmatch[1].str() + '\n';
				}
			}
			else if (srell::regex_match(line.c_str(), lineend, cmatch, re_scxdata))
			{
				const srell::cmatch::value_type &begin = cmatch[1];
				const srell::cmatch::value_type &end = cmatch[2];
				const srell::cmatch::value_type &scxnames = cmatch[3];
//				const srell::cmatch::value_type &comment = cmatch[4];

				range.first = static_cast<uchar32>(std::strtol(begin.first, NULL, 16));
				if (end.matched)
					range.second = static_cast<uchar32>(std::strtol(end.first, NULL, 16));
				else
					range.second = range.first;

				common.remove_range(range);
				inherited.remove_range(range);

				{
					scxparts.clear();
					split(scxparts, scxnames, ' ');
					for (strings_type::size_type i = 0; i < scxparts.size(); ++i)
					{
						const std::string &scriptname = scxparts[i];

						if (scriptname.size())
						{
							const canonicalname_mapper::const_iterator it = canonicalnames.find(scriptname);

							if (it != canonicalnames.end())
								scx[it->second].join(range);
							else
							{
//								unishared::throw_error("canonical name for \"%s\" is not found.", scriptname.c_str());
								if (!warning_out.count(scriptname))
								{
									std::printf("[Info] canonical name for \"%s\" is not found. New script?\n", scriptname.c_str());
									warning_out[scriptname] = true;
								}
							}
						}
					}
				}
			}
		}
		scx[name_common] = common;
		scx[name_inherited] = inherited;
	}

	void combine_properties(sortedrangeholder &base, const rangeholder &addition, const char *const ptype, const char *const *aliasnames)
	{
		const canonicalname_mapper canonicalnames(load_canonicalnames(aliasnames));
		sorted_name_and_ranges elem;
		strings_type names;

		for (; **aliasnames; ++aliasnames)
		{
			const std::string aliases(*aliasnames);
			bool pdata_found = false;

			names.clear();
			split(names, aliases, ':');

			const std::string canonicalname(names[0]);

			for (strings_type::size_type i = 0; i < names.size(); ++i)
			{
				const rangeholder::const_iterator it = addition.find(names[i]);

				if (it != addition.end())
				{
					elem.ucpranges = it->second;
					pdata_found = true;
					break;
				}
			}

			if (!pdata_found)
				unishared::throw_error("no property value for \"%s\" found.", aliases.c_str());

			elem.ptype = ptype;
			elem.canonicalname = canonicalname;
			elem.namealiases = aliases;
			base.push_back(elem);
		}
	}

	name_mapper create_ptype_mappings()
	{
		name_mapper categories;

		categories["gc"] = "general_category";
		categories["bp"] = "binary";
		categories["sc"] = "script";
		categories["scx"] = "script_extensions";
		return categories;
	}

	std::string create_ptypes(const name_mapper &ptypes)
	{
		std::string ptypedef("\tstruct ptype\n\t{\n\t\tstatic const T2 unknown = 0;\n");
		const char *names[] = { "bp", "gc", "sc", "scx", "" };

		for (unsigned int i = 0; *names[i];)
		{
			const name_mapper::const_iterator it = ptypes.find(names[i]);

			if (it == ptypes.end())
				unishared::throw_error("name for ptype \"%s\" is not found.", names[i]);

			ptypedef += "\t\tstatic const T2 " + it->second + " = " + unishared::stringify<16>(++i, "%u") + ";\n";
		}

		ptypedef += "\t};\n";
		return ptypedef;
	}

	std::string ranges_to_string(const ucprange_array &array, const std::string &indent)
	{
		std::string rangestring(indent);
		unsigned count = 0;

		for (ucprange_array::size_type i = 0; i < array.size(); ++i)
		{
			const ucprange &range = array[i];
			if (count == 4)
			{
				count = 0;
				rangestring += '\n' + indent;
			}
			else if (count)
			{
				rangestring += ' ';
			}
			rangestring += "0x" + unishared::stringify<16>(range.first, "%.4lX") + ", 0x" + unishared::stringify<16>(range.second, "%.4lX") + ',';
			++count;
		}
		return rangestring;
	}

	void drop_finalcomma(std::string &data)
	{
		std::string::size_type commapos = data.rfind(',');
		if (commapos != std::string::npos)
			data.erase(commapos, 1);
	}

	std::string create_pnametable(const std::string &return_table, const int version, const std::string &template1, const std::string &template2, const std::string &indent)
	{
		std::string out(version == 100 ? "\tstatic const T3 *propertyname_table()\n\t{\n\t\tstatic const T3 table[] =\n\t\t{\n" : template1 + "const T3 " + template2 + "propertynametable[] =\n{\n");
		const char *const *pnames = updata::property_names;

		out.append(indent + "\"*\",\t//  #0:unknown\n");
		out.append(indent + "\"*\",\t//  #1:binary\n");

		for (unsigned int i = 2; **pnames; ++pnames, ++i)
		{
			out.append(indent);
			out.append(1, '"');
			out.append(*pnames);
			out.append("\",\t//  #" + unishared::stringify<16>(i, "%u") + '\n');
		}
		out.append(indent + "\"\"\n");
		out.append(return_table);

		return out;
	}

	std::string join_dropcomma_append(const strings_type &s, const std::string &return_table)
	{
		std::string tmp(join('\n', s, true));

		drop_finalcomma(tmp);
		tmp.append(return_table);
		return tmp;
	}

	void do_formatting(std::string &out, const sortedrangeholder &alldata, const int version)
	{
		typedef std::map<std::string, unsigned int> registered_sequence;
		const std::string template1("template <typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>\n");
		const std::string template2("unicode_property_data<T1, T2, T3, T4, T5, T6>::");
		const std::string return_table(version == 100 ? "\t\t};\n\t\treturn table;\n\t}\n" : "};\n");
		const std::string indent(version == 100 ? "\t\t\t" : "\t");
		name_mapper ptype_mappings(create_ptype_mappings());
		const std::string ptypes(create_ptypes(ptype_mappings));
		const std::string pnames(create_pnametable(return_table, version, template1, template2, indent));
		std::string pnumbers("\tstatic const T1 unknown = 0;\n");	//  property numbers.
		strings_type rangetable;
		strings_type lookup_ranges;
		unsigned int property_id_number = 1;
		unsigned int property_number = 1;
		registered_sequence registered;
		std::string lookup_numbers;
		unsigned int offset = 0;

		out.append(template1 + "struct unicode_property_data\n{\n");
		if (version == 100)
		{
			rangetable.push_back(std::string("\tstatic const T4 *ranges()\n\t{\n\t\tstatic const T4 table[] =\n\t\t{"));
			lookup_ranges.push_back(std::string("\tstatic const T6 *position_table()\n\t{\n\t\tstatic const T6 table[] =\n\t\t{\n\t\t\t{ 0, 0 },\t//  #0 unknown"));
		}
		else
		{
			rangetable.push_back(template1 + "const T4 " + template2 + "rangetable[] =\n{");
			lookup_ranges.push_back(template1 + "const T6 " + template2 + "positiontable[] =\n{\n\t{ 0, 0 },\t//  #0 unknown");
		}

		for (sortedrangeholder::size_type i = 0; i < alldata.size(); ++i)
		{
			const sorted_name_and_ranges &elem = alldata[i];
			const std::string ptype = elem.ptype;
			const std::string name = elem.canonicalname;
			const std::string aliases = elem.namealiases;
			const ucprange_array &array = elem.ucpranges;
			const std::string pnumber_keyname(ptype + '_' + name);
			const std::string position_comment(' ' + ptype + '=' + aliases);
			std::string rangestring(ranges_to_string(array, indent));
			unsigned int numofranges = static_cast<unsigned int>(array.size());
			unsigned int pno = property_number;
			const registered_sequence::const_iterator rit = registered.find(rangestring);

			if (rit != registered.end())
			{
				pno = rit->second;
				lookup_ranges[pno] += position_comment;
				rangetable[pno * 2 - 1] += position_comment;
				lookup_numbers.append(indent + "{ ptype::" + ptype_mappings[ptype] + ", \"" + aliases + "\", " + unishared::stringify<16>(pno, "%u") + " },\t//  #" + unishared::stringify<16>(property_id_number, "%u") + "\n");
			}
			else
			{
				//  ucpranges of "Assigned" is empty.
				const bool compositeclass_found = array.size() && array[0].first == compositeclass;

				if (compositeclass_found)
				{
					std::printf("[Info] composite property \"%s\" found.\n", aliases.c_str());
					numofranges = array[0].second;
					rangestring = indent + "//  ";

					for (ucprange_array::size_type i = 1; i < array.size(); ++i)
					{
						const ucprange &range = array[i];

						if (i > 1)
							rangestring += " + ";
						rangestring += static_cast<char>(range.first);
						rangestring += static_cast<char>(range.second);
						rangestring += ':' + unishared::stringify<16>(array[++i].first, "%u");
					}
				}
				else
				{
					registered[rangestring] = property_number;
				}

				lookup_numbers.append(indent + "{ ptype::" + ptype_mappings[ptype] + ", \"" + aliases + "\", " + unishared::stringify<16>(property_number, "%u") + " },\t//  #" + unishared::stringify<16>(property_id_number, "%u") + "\n");
				lookup_ranges.push_back(indent + "{ " + unishared::stringify<16>(offset, "%u") + ", " + unishared::stringify<16>(numofranges, "%u") + " },\t//  #" + unishared::stringify<16>(pno, "%u") + position_comment);
				rangetable.push_back(indent + "//  #" + unishared::stringify<16>(pno, "%u") + " (" + unishared::stringify<16>(offset, "%u") + '+' + unishared::stringify<16>(numofranges, "%u") + "):" + position_comment);
				rangetable.push_back(rangestring);

				if (!compositeclass_found)
					offset += numofranges;

				++property_number;
			}
			pnumbers.append("\tstatic const T1 " + pnumber_keyname + " = " + unishared::stringify<16>(property_id_number, "%u") + ";\t//  #" + unishared::stringify<16>(pno, "%u") + '\n');
			++property_id_number;
		}

		pnumbers.append("\tstatic const T1 last_property_number = " + unishared::stringify<16>(--property_number, "%u") + ";\n");

		lookup_numbers.append(indent + "{ ptype::unknown, \"\", 0 }\n");
		lookup_numbers.append(return_table);
		lookup_numbers.insert(0, version == 100 ? "\tstatic const T5 *rangenumber_table()\n\t{\n\t\tstatic const T5 table[] =\n\t\t{\n\t\t\t{ ptype::unknown, \"*\", 0 },\t//  #0\n" : template1 + "const T5 " + template2 + "rangenumbertable[] =\n{\n\t{ ptype::unknown, \"*\", 0 },\t//  #0\n");

		out.append(pnumbers);
		out.append(ptypes);
		if (version == 100)
		{
			out.append(pnames);
			out.append(join_dropcomma_append(rangetable, return_table));
			out.append(lookup_numbers);
			out.append(join_dropcomma_append(lookup_ranges, return_table));
			out.append("};\n");
		}
		else
		{
			out.append("\tstatic const T3 propertynametable[];\n");
			out.append("\tstatic const T4 rangetable[];\n");
			out.append("\tstatic const T5 rangenumbertable[];\n");
			out.append("\tstatic const T6 positiontable[];\n\n");

			out.append("\tstatic const T3 *propertyname_table()\n\t{\n\t\treturn propertynametable;\n\t}\n");
			out.append("\tstatic const T4 *ranges()\n\t{\n\t\treturn rangetable;\n\t}\n");
			out.append("\tstatic const T5 *rangenumber_table()\n\t{\n\t\treturn rangenumbertable;\n\t}\n");
			out.append("\tstatic const T6 *position_table()\n\t{\n\t\treturn positiontable;\n\t}\n");
			out.append("};\n\n");
			out.append(pnames);
			out.append("\n");
			out.append(join_dropcomma_append(rangetable, return_table));
			out.append("\n");
			out.append(lookup_numbers);
			out.append("\n");
			out.append(join_dropcomma_append(lookup_ranges, return_table));
		}
		if (version > 100)
			out.append("#define SRELL_UPDATA_VERSION 110\n");
	}

	srell::regex re_licenseline_;
	srell::regex re_licenseend_;
};
//  class unicode_property

int main(const int argc, const char *const *const argv)
{
	up_options upopts(argc, argv);
	std::string outdata;
	unicode_property up;
	int errorno = up.create_updata(outdata, upopts);

	if (errorno == 0)
	{
		if (!unishared::write_file(upopts.outfilename, outdata))
			errorno = 2;
	}
	return errorno;
}
