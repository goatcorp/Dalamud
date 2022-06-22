//
//  ucfdataout.cpp: version 2.100 (2020/05/13).
//
//  This is a program that generates srell_ucfdata.hpp from CaseFolding.txt
//  provided by the Unicode Consortium. The latese version is available at:
//  http://www.unicode.org/Public/UNIDATA/CaseFolding.txt
//

#include <cstdio>
#include <cstdlib>
#include <string>
#include <map>
#include "../srell.hpp"

#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(disable:4996)
#endif

namespace unishared
{
template <const std::size_t BufSize, typename Type>
std::string stringify(const Type value, const char *const fmt)
{
	char buffer[BufSize];
	std::sprintf(buffer, fmt, value);
	return std::string(buffer);
}

bool read_file(std::string &str, const char *const filename, const char *const dir)
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
			return true;
		}
	}
	std::fputs("failed...\n", stdout);
	return false;
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
}
//  namespace unishared

struct ucf_options
{
	const char *infilename;
	const char *outfilename;
	const char *indir;
	int version;
	int errorno;

	ucf_options(const int argc, const char *const *const argv)
		: infilename("CaseFolding.txt")
		, outfilename("srell_ucfdata2.hpp")
		, indir("")
		, version(2)
		, errorno(0)
	{
		bool outfile_specified = false;

		for (int index = 1; index < argc; ++index)
		{
			const char firstchar = argv[index][0];

			if (firstchar == '-' || firstchar == '/')
			{
				const char *const option = argv[index] + 1;

				++index;
				if (std::strcmp(option, "i") == 0)
				{
					if (index >= argc)
						goto NO_ARGUMENT;
					infilename = argv[index];
				}
				else if (std::strcmp(option, "o") == 0)
				{
					if (index >= argc)
						goto NO_ARGUMENT;
					outfilename = argv[index];
					outfile_specified = true;
				}
				else if (std::strcmp(option, "v") == 0)
				{
					if (index >= argc)
						goto NO_ARGUMENT;
					version = static_cast<int>(std::strtol(argv[index], NULL, 10));
					if (!outfile_specified && version < 2)
					{
						static const char *const v1name = "srell_ucfdata.hpp";
						outfilename = v1name;
					}
				}
				else if (std::strcmp(option, "id") == 0)
				{
					if (index >= argc)
						goto NO_ARGUMENT;
					indir = argv[index];
				}
				else
				{
					--index;
					goto UNKNOWN_OPTION;
				}

				continue;

				NO_ARGUMENT:
				std::fprintf(stdout, "[Error] no argument for \"%s\" specified.\n", argv[--index]);
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
//  struct ucf_options

class unicode_casefolding
{
public:

	unicode_casefolding()
		: maxdelta_(0L), maxdelta_cp_(0L), ucf_maxcodepoint_(0L), rev_maxcodepoint_(0L)
		, ucf_numofsegs_(1U), rev_numofsegs_(1U), numofcps_from_(0U), numofcps_to_(0U)
		, max_appearance_(0U), nextoffset_(0x100L), rev_charsets_(1, -1L)
	{
	}

	int create_ucfdata(std::string &outdata, const ucf_options &opts)
	{
		const std::string indent("\t\t\t");
		int errorno = opts.errorno;
		std::string buf;

		if (errorno)
			return errorno;

		if (unishared::read_file(buf, opts.infilename, opts.indir))
		{
			static const srell::regex re_line("^.*$", srell::regex::multiline);
			const srell::cregex_iterator eos;
			srell::cregex_iterator iter(buf.c_str(), buf.c_str() + buf.size(), re_line);
			srell::cmatch match;
			int colcount = 0;

			for (; iter != eos; ++iter)
			{
				if (iter->length(0))
				{
					static const srell::regex re_datainfo("^# (.*)$");

					if (!srell::regex_match((*iter)[0].first, (*iter)[0].second, match, re_datainfo))
					{
						outdata.append(1, '\n');
						break;
					}
					outdata += "//  " + match.str(1) + "\n";
				}
			}

			if (opts.version <= 1)
				outdata += "template <typename T1, typename T2, typename T3>\nstruct unicode_casefolding\n{\n\tstatic const T1 *table()\n\t{\n\t\tstatic const T1 ucftable[] =\n\t\t{\n";
			else
				outdata += "template <typename T2, typename T3>\nstruct unicode_casefolding\n{\n";

			for (; iter != eos; ++iter)
			{
				static const srell::regex re_cfdata("^\\s*([0-9A-Fa-f]+); ([CS]); ([0-9A-Fa-f]+);\\s*#\\s*(.*)$");
				const srell::cmatch &line = *iter;

				if (srell::regex_match(line[0].first, line[0].second, match, re_cfdata))
				{
					const std::string from(match[1]);
					const std::string to(match[3]);
					const std::string type(match[2]);
					const std::string name(match[4]);

					update(from, to);

					if (opts.version == 1)
						outdata += indent + "{ 0x" + from + ", 0x" + to + " },\t//  " + type + "; " + name + "\n";
					else if (opts.version <= 0)
					{
						if (colcount == 0)
							outdata += indent;
						outdata += "{ 0x" + from + ", 0x" + to + " },";
						if (++colcount == 4)
						{
							outdata.append(1, '\n');
							colcount = 0;
						}
					}
				}
				else if (opts.version == 1)
				{
					static const srell::regex re_comment_or_emptyline("^#.*|^$");

					if (!srell::regex_match(line[0].first, line[0].second, re_comment_or_emptyline))
						outdata += indent + "//  " + line.str(0) + "\n";
				}
			}
			if (colcount > 0)
				outdata.append(1, '\n');
			if (opts.version <= 1)
				outdata += indent + "{ 0, 0 }\n\t\t};\n\t\treturn ucftable;\n\t}\n";

			outdata += "\tstatic const T2 ucf_maxcodepoint = 0x" + unishared::stringify<16>(ucf_maxcodepoint_, "%.4lX") + ";\n";
			outdata += "\tstatic const T3 ucf_deltatablesize = 0x" + unishared::stringify<16>(ucf_numofsegs_ << 8, "%X") + ";\n";

			outdata += "\tstatic const T2 rev_maxcodepoint = 0x" + unishared::stringify<16>(rev_maxcodepoint_, "%.4lX") + ";\n";
			outdata += "\tstatic const T3 rev_indextablesize = 0x" + unishared::stringify<16>(rev_numofsegs_ << 8, "%X") + ";\n";
			outdata += "\tstatic const T3 rev_charsettablesize = " + unishared::stringify<16>(numofcps_to_ * 2 + numofcps_from_ + 1, "%u") + ";\t//  1 + " + unishared::stringify<16>(numofcps_to_, "%u") + " * 2 + " + unishared::stringify<16>(numofcps_from_, "%u") + "\n";
			outdata += "\tstatic const T3 rev_maxset = " + unishared::stringify<16>(maxset(), "%u") + ";\n";
			outdata += "\tstatic const T2 eos = 0;\n";

			if (opts.version >= 2)
			{
				outdata += "\n\tstatic const T2 ucf_deltatable[];\n\tstatic const T3 ucf_segmenttable[];\n\tstatic const T3 rev_indextable[];\n\tstatic const T3 rev_segmenttable[];\n\tstatic const T2 rev_charsettable[];\n\n\tstatic const T2 *ucf_deltatable_ptr()\n\t{\n\t\treturn ucf_deltatable;\n\t}\n\tstatic const T3 *ucf_segmenttable_ptr()\n\t{\n\t\treturn ucf_segmenttable;\n\t}\n\tstatic const T3 *rev_indextable_ptr()\n\t{\n\t\treturn rev_indextable;\n\t}\n\tstatic const T3 *rev_segmenttable_ptr()\n\t{\n\t\treturn rev_segmenttable;\n\t}\n\tstatic const T2 *rev_charsettable_ptr()\n\t{\n\t\treturn rev_charsettable;\n\t}\n};\n\n";
				out_v2tables(outdata);
				outdata += "#define SRELL_UCFDATA_VERSION 200\n";
			}
			else
				outdata += "};\n#define SRELL_UCFDATA_VER 201909L\n";

			std::fprintf(stdout, "MaxDelta: %+ld (U+%.4lX->U+%.4lX)\n", maxdelta_, maxdelta_cp_, maxdelta_cp_ + maxdelta_);
		}
		else
			errorno = 1;

		return errorno;
	}

private:

	void update(const std::string &from, const std::string &to)
	{
		const long cp_from = std::strtol(from.c_str(), NULL, 16);
		const long cp_to = std::strtol(to.c_str(), NULL, 16);
		const long delta = cp_to - cp_from;
		const long segno_from = cp_from >> 8;
		const long segno_to = cp_to >> 8;

		update_tables(cp_from, cp_to, segno_from);

		++numofcps_from_;
		if (std::abs(maxdelta_) < std::abs(delta))
		{
			maxdelta_cp_ = cp_from;
			maxdelta_ = delta;
		}

		if (ucf_maxcodepoint_ < cp_from)
			ucf_maxcodepoint_ = cp_from;

		if (rev_maxcodepoint_ < cp_to)
			rev_maxcodepoint_ = cp_to;

		if (rev_maxcodepoint_ < cp_from)
			rev_maxcodepoint_ = cp_from;

		if (!ucf_countedsegnos.count(segno_from))
		{
			ucf_countedsegnos[segno_from] = 1;
			++ucf_numofsegs_;
		}

		if (!rev_countedsegnos.count(segno_to))
		{
			rev_countedsegnos[segno_to] = 1;
			++rev_numofsegs_;
		}
		if (!rev_countedsegnos.count(segno_from))
		{
			rev_countedsegnos[segno_from] = 1;
			++rev_numofsegs_;
		}

		if (!cps_counted_as_foldedto.count(cp_to))
		{
			cps_counted_as_foldedto[cp_to] = 1;
			++numofcps_to_;
		}

		if (appearance_counts_.count(to))
			++appearance_counts_[to];
		else
			appearance_counts_[to] = 1;

		if (max_appearance_ < appearance_counts_[to])
			max_appearance_ = appearance_counts_[to];
	}

	unsigned int maxset() const
	{
		return max_appearance_ + 1;
	}

	void out_v2tables(std::string &outdata)
	{
		const char *const headers[] = {
			"template <typename T2, typename T3>\nconst ",
			" unicode_casefolding<T2, T3>::",
			"[] =\n{\n"
		};

		create_revtables();
		out_lowertable(outdata, headers, "T2", "ucf_deltatable", ucf_deltas_, ucf_segments_);
		outdata.append(1, '\n');
		out_uppertable(outdata, headers, "T3", "ucf_segmenttable", ucf_segments_);
		outdata.append(1, '\n');
		out_lowertable(outdata, headers, "T3", "rev_indextable", rev_indices_, rev_segments_);
		outdata.append(1, '\n');
		out_uppertable(outdata, headers, "T3", "rev_segmenttable", rev_segments_);
		outdata.append(1, '\n');
		out_cstable(outdata, headers, "T2", "rev_charsettable", rev_charsets_);
	}

	//  Updates ucf_segments_, ucf_deltas_, and rev_charsets_.
	void update_tables(const long cp_from, const long cp_to, const long segno_from)
	{
		if (segno_from >= static_cast<long>(ucf_segments_.size()))
			ucf_segments_.resize(segno_from + 1, 0L);

		long &offset_of_segment = ucf_segments_[segno_from];

		if (offset_of_segment == 0L)
		{
			offset_of_segment = nextoffset_;
			nextoffset_ += 0x100L;
			ucf_deltas_.resize(nextoffset_, 0L);
		}

		ucf_deltas_[offset_of_segment + (cp_from & 0xffL)] = cp_to - cp_from;

		for (long index = 0L;; ++index)
		{
			if (index == static_cast<long>(rev_charsets_.size()))
			{
				rev_charsets_.push_back(cp_to);
				rev_charsets_.push_back(cp_from);
				rev_charsets_.push_back(-1L);
				break;
			}
			if (rev_charsets_[index] == cp_to)
			{
				for (++index; rev_charsets_[index] != -1L; ++index);

				rev_charsets_.insert(index, 1, cp_from);
				break;
			}
		}
	}

	//  Creates rev_segments_ and rev_indices_ from rev_charsets_.
	void create_revtables()
	{
		long nextoffset = 0x100L;
		for (long index = 0L; index < static_cast<long>(rev_charsets_.size()); ++index)
		{
			const long bocs = index;	//  Beginning of charset.

			for (; rev_charsets_[index] != -1L; ++index)
			{
				const long &u21ch = rev_charsets_[index];
				const long segno = u21ch >> 8L;

				if (segno >= static_cast<long>(rev_segments_.size()))
					rev_segments_.resize(segno + 1, 0L);

				long &offset_of_segment = rev_segments_[segno];

				if (offset_of_segment == 0L)
				{
					offset_of_segment = nextoffset;
					nextoffset += 0x100L;
					rev_indices_.resize(nextoffset, 0L);
				}
				rev_indices_[offset_of_segment + (u21ch & 0xffL)] = bocs;
			}
		}
	}

	void out_lowertable(std::string &outdata, const char *const headers[], const char *const type, const char *const funcname, const std::basic_string<long> &table, const std::basic_string<long> &segtable) const
	{
		int end = static_cast<int>(table.size());

		outdata += headers[0];
		outdata += type;
		outdata += headers[1];
		outdata += funcname;
		outdata += headers[2];

		for (int i = 0; i < end;)
		{
			const int col = i & 15;

			if ((i & 255) == 0)
			{
				if (i)
				{
					for (int j = 0; j < static_cast<int>(segtable.size()); ++j)
					{
						if (segtable[j] == i)
						{
							outdata += "\n\t//  For u+" + unishared::stringify<16>(j, "%.2X") + "xx (" + unishared::stringify<16>(i, "%d") + ")\n";
							break;
						}
					}
				}
				else
					outdata += "\t//  For common (0)\n";
			}

			outdata += col == 0 ? "\t" : (col & 3) == 0 ? "  " : " ";
			if (table[i] >= 0L)
				outdata += unishared::stringify<16>(table[i], "%ld");
			else
				outdata += "static_cast<", outdata += type, outdata += ">(", outdata += unishared::stringify<16>(table[i], "%ld") + ")";

			if (++i == end)
				outdata.append(1, '\n');
			else if (col == 15)
				outdata += ",\n";
			else
				outdata.append(1, ',');
		}
		outdata += "};\n";
	}

	void out_uppertable(std::string &outdata, const char *const headers[], const char *const type, const char *const funcname, const std::basic_string<long> &table) const
	{
		int end = static_cast<int>(table.size());

		outdata += headers[0];
		outdata += type;
		outdata += headers[1];
		outdata += funcname;
		outdata += headers[2];

		for (int i = 0; i < end;)
		{
			const int col = i & 15;

			outdata += col == 0 ? "\t" : (col & 3) == 0 ? "  " : " ";
			if (table[i] >= 0)
				outdata += unishared::stringify<16>(table[i], "%ld");
			else
				outdata += "static_cast<", outdata += type, outdata += ">(", outdata += unishared::stringify<16>(table[i], "%ld") + ")";

			if (++i == end)
				outdata.append(1, '\n');
			else if (col == 15)
				outdata += ",\n";
			else
				outdata.append(1, ',');
		}
		outdata += "};\n";
	}

	void out_cstable(std::string &outdata, const char *const headers[], const char *const type, const char *const funcname, const std::basic_string<long> &table) const
	{
		int end = static_cast<int>(table.size());
		bool newline = true;
		int bos = 0;
		int prevprintedbos = -1;

		outdata += headers[0];
		outdata += type;
		outdata += headers[1];
		outdata += funcname;
		outdata += headers[2];

		for (int i = 0; i < end;)
		{
			const long val = table[i];

			outdata += newline ? "\t" : " ";
			newline = false;

			if (val == -1L)
				outdata += "eos";
			else
				outdata += "0x", outdata += unishared::stringify<16>(val, "%.4lX");

			if (++i != end)
				outdata.append(1, ',');

			if (val == -1L)
			{
				if (prevprintedbos != bos / 10 || i == end)
				{
					outdata += "\t//  ";
					outdata += unishared::stringify<16>(bos, "%d");
					prevprintedbos = bos / 10;
				}
				outdata.append(1, '\n');
				newline = true;
				bos = i;
			}
		}
		outdata += "};\n";
	}

	typedef std::map<long, char> flagset_type;

	long maxdelta_;	//  = 0L;
	long maxdelta_cp_;	//  = 0L;
	long ucf_maxcodepoint_;	//  = 0L;	//  The max code point for case-folding.
	long rev_maxcodepoint_;	//  = 0L;	//  The max code point for reverse lookup.
	unsigned int ucf_numofsegs_;	//  = 1U;	//  The number of segments in the delta table.
	unsigned int rev_numofsegs_;	//  = 1U;	//  The number of segments in the table for reverse lookup.
	unsigned int numofcps_from_;	//  = 0U;	//  The number of code points in "folded from"s.
	unsigned int numofcps_to_;	//  = 0U;	//  The number of code points in "folded to"s.

	flagset_type ucf_countedsegnos;	//  The set of segment nos marked as "counted" for case-folding.
	flagset_type rev_countedsegnos;	//  The set of segment nos marked as "counted" for reverse lookup.
	flagset_type cps_counted_as_foldedto;	//  The set of code points marked as "folded to".

	unsigned int max_appearance_;
	std::map<std::string, unsigned int> appearance_counts_;

	long nextoffset_;
	std::basic_string<long> ucf_deltas_;
	std::basic_string<long> ucf_segments_;
	std::basic_string<long> rev_indices_;
	std::basic_string<long> rev_segments_;
	std::basic_string<long> rev_deltas_;
	std::basic_string<long> rev_charsets_;
};
//  class unicode_casefolding

int main(const int argc, const char *const *const argv)
{
	ucf_options ucfopts(argc, argv);
	std::string outdata;
	unicode_casefolding ucf;
	int errorno = ucf.create_ucfdata(outdata, ucfopts);

	if (errorno == 0)
	{
		if (!unishared::write_file(ucfopts.outfilename, outdata))
			errorno = 2;
	}
	return errorno;
}
