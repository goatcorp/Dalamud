//
//  A sample program for SRELL (tests and benchmarks).
//  2021/06/24; version 1.104
//
//  Macro Options:
//    -DSTD_REGEX: std::regex used.
//    -DBOOST_REGEX: boost::regex used.
//    -DBOOST_XPRESSIVE: boost::xpressive used.
//    unspecified or others: SRELL used.
//

#include <cstdio>
#include <cstring>
#include <ctime>
#include <string>
#include <vector>

#if defined(STD_REGEX)
  #include <regex>
  #define RE_PREFIX std
  #pragma message("std::regex selected.")
#elif defined(BOOST_REGEX)
  #include <boost/regex.hpp>
  #define RE_PREFIX boost
  #pragma message("boost::regex selected.")
#elif defined(BOOST_XPRESSIVE)
  #include <boost/xpressive/xpressive.hpp>
  #define RE_PREFIX boost::xpressive
  #pragma message("boost::xpressive selected.")
#else
  #include "../srell.hpp"
  #define RE_PREFIX srell
  #pragma message("srell selected.")
#endif

bool test(const std::string &str, const std::string &exp, const unsigned int max, const std::vector<std::string> *const expected = NULL)
{
#if !defined(BOOST_XPRESSIVE)
	RE_PREFIX::regex re;
#else
	boost::xpressive::cregex re;
#endif
	RE_PREFIX::cmatch mr;
	bool b = false;
	unsigned int num_of_failures = 0;

	try
	{
		std::string matched;
		std::string msg;

#if !defined(BOOST_XPRESSIVE)
		re.assign(exp, RE_PREFIX::regex::ECMAScript);
#else
		re = boost::xpressive::cregex::compile(exp, boost::xpressive::cregex::ECMAScript | boost::xpressive::cregex::not_dot_newline);
#endif

		const clock_t st = std::clock();

		for (unsigned int i = 0; i < max; i++)
#if !defined(BOOST_REGEX)
			b = RE_PREFIX::regex_search(str.c_str(), str.c_str() + str.size(), mr, re);
#else
			b = RE_PREFIX::regex_search(str.c_str(), str.c_str() + str.size(), mr, re, boost::regex_constants::match_not_dot_newline);
#endif

		const clock_t ed = std::clock();

//		std::fprintf(stdout, "\t\"%s\" =~ /%s/\n", str.c_str(), exp.c_str());	//  Perl 5 style.
		std::fprintf(stdout, "\t/%s/.exec(\"%s\");\n", exp.c_str(), str.c_str());	//  ECMAScript style.
		if (max > 1)
			std::fprintf(stdout, "\t%u times\n", max);
		std::fprintf(stdout, "\t%s (%ld msec)\n", b ? "Found" : "Not Found", static_cast<long>(static_cast<double>(ed - st) * 1000 / CLOCKS_PER_SEC));

		for (RE_PREFIX::cmatch::size_type i = 0; i < mr.size(); ++i)
		{
			if (i)
				std::fprintf(stdout, "\t$%u = ", i);
			else
				std::fputs("\t$& = ", stdout);
			if (mr[i].matched)
			{
				matched = mr[i].str();
				msg = '"' + matched + '"' + " (%u+%u)";
			}
			else
				msg = matched = "(undefined)";

			if (expected)
			{
				if (i < expected->size())
				{
					if (matched == expected->operator[](i))
						msg += "; passed!";
					else
					{
						msg += "; failed... (expected: \"" + expected->operator[](i) + "\")";
						++num_of_failures;
					}
				}
				else
				{
					msg += "; failed...";	//  should not exist.
					++num_of_failures;
				}
			}
			msg += '\n';
			std::fprintf(stdout, msg.c_str(), mr.position(i), mr.length(i));
		}

		if (!num_of_failures && expected->size() != mr.size())
			++num_of_failures;

		std::fprintf(stdout, "Result: %s.\n\n", num_of_failures ? "failed" : "passed");
		return num_of_failures == 0;
	}
	catch (const RE_PREFIX::regex_error &e)
	{
		std::fprintf(stdout, "Error (regex_error): %d \"%s\"\n\n", e.code(), e.what());
	}
	catch (const std::exception &e)
	{
		std::fprintf(stdout, "Error (std::exception): \"%s\"\n\n", e.what());
	}
	return false;
}


int main()
{
	const unsigned int count = 100000;
	std::string exp;
	std::string str;
	std::vector<std::string> expected;
	unsigned int num_of_tests = 0;
	unsigned int num_of_tests_passed = 0;
	unsigned int num_of_benches = 0;
	unsigned int num_of_benches_passed = 0;

	std::fputs("Test 1 (ECMAScript 2021 Language Specification 22.2.2.3, NOTE)\n", stdout);
	str = "abc";
	exp = "((a)|(ab))((c)|(bc))";
	expected.resize(7);
	expected[0] = "abc";
	expected[1] = "a";
	expected[2] = "a";
	expected[3] = "(undefined)";
	expected[4] = "bc";
	expected[5] = "(undefined)";
	expected[6] = "bc";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 2a (ECMAScript 2021 Language Specification 22.2.2.5.1, NOTE 2)\n", stdout);
	str = "abcdefghi";
	exp = "a[a-z]{2,4}";
	expected.resize(1);
	expected[0] = "abcde";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 2b (ECMAScript 2021 Language Specification 22.2.2.5.1, NOTE 2)\n", stdout);
	str = "abcdefghi";
	exp = "a[a-z]{2,4}?";
	expected[0] = "abc";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 3 (ECMAScript 2021 Language Specification 22.2.2.5.1, NOTE 2)\n", stdout);
	str = "aabaac";
	exp = "(aa|aabaac|ba|b|c)*";
	expected.resize(2);
	expected[0] = "aaba";
	expected[1] = "ba";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 4 (ECMAScript 2021 Language Specification 22.2.2.5.1, NOTE 3)\n", stdout);
	str = "zaacbbbcac";
	exp = "(z)((a+)?(b+)?(c))*";
	expected.resize(6);
	expected[0] = "zaacbbbcac";
	expected[1] = "z";
	expected[2] = "ac";
	expected[3] = "a";
	expected[4] = "(undefined)";
	expected[5] = "c";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 5a (ECMAScript 2021 Language Specification 22.2.2.5.1, NOTE 4)\n", stdout);
	str = "b";
	exp = "(a*)*";
	expected.resize(2);
	expected[0] = "";
	expected[1] = "";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 5b (ECMAScript 2021 Language Specification 22.2.2.5.1, NOTE 4)\n", stdout);
	str = "baaaac";
	exp = "(a*)b\\1+";
	expected[0] = "b";
	expected[1] = "";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 6a (ECMAScript 2021 Language Specification 22.2.2.8.2, NOTE 2)\n", stdout);
	str = "baaabac";
	exp = "(?=(a+))";
	expected[0] = "";
	expected[1] = "aaa";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 6b (ECMAScript 2021 Language Specification 22.2.2.8.2, NOTE 2)\n", stdout);
	str = "baaabac";
	exp = "(?=(a+))a*b\\1";
	expected[0] = "aba";
	expected[1] = "a";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 7 (ECMAScript 2021 Language Specification 22.2.2.8.2, NOTE 3)\n", stdout);
	str = "baaabaac";
	exp = "(.*?)a(?!(a+)b\\2c)\\2(.*)";
	expected.resize(4);
	expected[0] = "baaabaac";
	expected[1] = "ba";
	expected[2] = "(undefined)";
	expected[3] = "abaac";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

	std::fputs("Test 8 (from https://github.com/tc39/test262/tree/master/test/built-ins/RegExp/lookBehind/misc.js)\n", stdout);
	str = "abc";
	exp = "(abc\\1)";
	expected.resize(2);
	expected[0] = "abc";
	expected[1] = "abc";
	if (test(str, exp, 1, &expected))
		++num_of_tests_passed;
	++num_of_tests;

#ifndef SKIP_BENCHMARK

	std::fputs("Benchmark 01\n", stdout);
	     //0123456
	str = "aaaabaa";
	exp = "^(.*)*b\\1$";
	expected.resize(2);
	expected[0] = "aaaabaa";
	expected[1] = "aa";
	if (test(str, exp, count, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 02\n", stdout);
	     //012345678
	str = "aaaabaaaa";
	exp = "^(.*)*b\\1\\1$";
	expected[0] = "aaaabaaaa";
	expected[1] = "aa";
	if (test(str, exp, count, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 03\n", stdout);
	     //01
	str = "ab";
	exp = "(.*?)*b\\1";
	expected[0] = "b";
	expected[1] = "";
	if (test(str, exp, count * 10, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 04\n", stdout);
	     //01234567
	str = "acaaabbb";
	exp = "(a(.)a|\\2(.)b){2}";
	expected.resize(4);
	expected[0] = "aaabb";
	expected[1] = "bb";
	expected[2] = "(undefined)";
	expected[3] = "b";
	if (test(str, exp, count * 10, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 05\n", stdout);
	str = "aabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbaaaaaa";
	exp = "(a*)(b)*\\1\\1\\1";
	expected.resize(3);
	expected[0] = "aabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbaaaaaa";
	expected[1] = "aa";
	expected[2] = "b";
	if (test(str, exp, count, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 06a\n", stdout);
	str = "aaaaaaaaaab";
	exp = "(.*)*b";
	expected.resize(2);
	expected[0] = "aaaaaaaaaab";
	expected[1] = "aaaaaaaaaa";
	if (test(str, exp, count * 10, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 06b\n", stdout);
	str = "aaaaaaaaaab";
	exp = "(.*)+b";
	if (test(str, exp, count * 10, &expected))	//  the same results expected.
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 06c\n", stdout);
	str = "aaaaaaaaaab";
	exp = "(.*){2,}b";
	expected[1] = "";
	if (test(str, exp, count * 10, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 07\n", stdout);
	str = "aaaaaaaaaabc";
	exp = "(?=(a+))(abc)";
	expected.resize(3);
	expected[0] = "abc";
	expected[1] = "a";
	expected[2] = "abc";
	if (test(str, exp, count, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 08\n", stdout);
	str = "1234-5678-1234-456";
	exp = "(\\d{4}[-]){3}\\d{3,4}";
	expected.resize(2);
	expected[0] = "1234-5678-1234-456";
	expected[1] = "1234-";
	if (test(str, exp, count * 5, &expected))
		++num_of_benches_passed;
	++num_of_benches;

	std::fputs("Benchmark 09\n", stdout);
	str = "aaaaaaaaaaaaaaaaaaaaa";
	exp = "(.*)*b";
	expected.resize(0);
	if (test(str, exp, 1, &expected))
		++num_of_benches_passed;
	++num_of_benches;

#endif	//  !defined(SKIP_BENCHMARK)

	std::fprintf(stdout, "Results of tests: %u/%u passed.\n", num_of_tests_passed, num_of_tests);
	std::fprintf(stdout, "Results of benchmarks: %u/%u passed.\n", num_of_benches_passed, num_of_benches);

return 0;

	std::fputs("Benchmark 10\n", stdout);
	str = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxz";
	exp = "(x+y*)+a";
	test(str, exp, 1);

	return 0;
}
