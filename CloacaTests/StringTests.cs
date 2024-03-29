﻿using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using LanguageImplementation;
using LanguageImplementation.DataTypes;
using CloacaInterpreter;

namespace CloacaTests
{
    [TestFixture]
    public class StringTests : RunCodeTest
    {
        [Test]
        public async Task StringConcatenation()
        {
            // Making sure that we're properly parsing and generating all of these when there's multiples of the operator.
            await runBasicTest(
                "a = 'Hello'\n" +
                "a = a + ', World!'\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("Hello, World!") }
                }), 1);
        }

        [Test]
        [Ignore("Currently fails from mixing .NET with PyString. Winds up in DynamicDispatchOperation thinking it's working with numbers")]
        public async Task StringConcatenateDotNetType()
        {
            // Making sure that we're properly parsing and generating all of these when there's multiples of the operator.
            await runBasicTest(
                "a = a + ', World!'\n",
                new Dictionary<string, object>
                {
                    { "a", "Hello" }
                },
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", "Hello, World!" }
                }), 1);
        }

        [Test]
        public async Task StringSubscriptNormal()
        {
            await runBasicTest(
                "a = 'Hello'\n" +
                "b = a[0]\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyString.Create("H") },
                }), 1);
        }

        [Test]
        public async Task StringSubscriptNegative()
        {
            await runBasicTest(
                "a = 'Hello'\n" +
                "b = a[-1]\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyString.Create("o") },
                }), 1);
        }

        [Test]
        public async Task StringSubscriptSlice()
        {
            await runBasicTest(
                "a = 'Hello'\n" +
                "b = a[1:3]\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyString.Create("el") },
                }), 1);
        }

        [Test]
        public async Task Contains()
        {
            await runBasicTest(
                "a = 'e' in 'Hello'\n" +
                "b = 'F' in 'Hello'\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.True },
                    { "b", PyBool.False },
                }), 1);
        }

        [Test]
        public async Task Capitalize()
        {
            await runBasicTest(
                "a = 'hello'.capitalize()\n" +
                "b = 'hello world'.capitalize()\n" +
                "c = ''.capitalize()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("Hello")},
                    { "b", PyString.Create("Hello world")},
                    { "c", PyString.Create()},
                }), 1);
        }

        [Test]
        [Ignore(".NET's string ToLower() doesn't do this and I'm looking up the deal with this.")]
        public async Task Casefold()
        {
            await runBasicTest(
                "a = 'HELLO'.casefold()\n" +
                "b = 'der Fluß'.casefold()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("hello")},
                    { "b", PyString.Create("der Fluss")},
                }), 1);
        }

        [Test]
        public async Task Count()
        {
            await runBasicTest(
                "searchies = 'erererrerere'\n" +
                "a = searchies.count('e')\n" +
                "b = searchies.count('e', 11)\n" +
                "c = searchies.count('e', 11, 12)\n" +
                "d = searchies.count('e', -1)\n" +
                "e = searchies.count('r', 11)\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(6)},
                    { "b", PyInteger.Create(1)},
                    { "c", PyInteger.Create(1)},
                    { "d", PyInteger.Create(1)},
                    { "e", PyInteger.Create(0)},
                }), 1);
        }

        [Test]
        public async Task Find()
        {
            await runBasicTest(
                "meowbeep = 'meowbeep'\n" +
                "b = meowbeep.find('ow')\n" +
                "c = meowbeep.find('e', 4)\n" +
                "d = meowbeep.find('e', -2)\n" +
                "e = meowbeep.find('beep', 0, 3)\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyInteger.Create(2)},
                    { "c", PyInteger.Create(5)},
                    { "d", PyInteger.Create(6)},
                    { "e", PyInteger.Create(-1)},
                }), 1);
        }

        [Test]
        public async Task Index()
        {
            await runBasicTest(
                "meowbeep = 'meowbeep'\n" +
                "b = meowbeep.index('ow')\n" +
                "c = meowbeep.index('e', 4)\n" +
                "d = meowbeep.index('e', -2)\n" +
                "e_err = False\n" +
                "try:\n" +
                "   e = meowbeep.index('beep', 0, 3)\n" +
                "except ValueError:\n" +
                "   e_err = True\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "b", PyInteger.Create(2)},
                    { "c", PyInteger.Create(5)},
                    { "d", PyInteger.Create(6)},
                    { "e_err", PyBool.Create(true)},
                }), 1);
        }

        [Test]
        public async Task IsDigit()
        {
            await runBasicTest(
                "a = '22'.isdigit()\n" +
                "b = '-22'.isdigit()\n" +
                "c = '2.2'.isdigit()\n" +
                "d = ''.isdigit()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(true)},
                    { "b", PyBool.Create(false)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        public async Task IsAlpha()
        {
            await runBasicTest(
                "a = '22'.isalpha()\n" +
                "b = 'ae'.isalpha()\n" +
                "c = 'a-'.isalpha()\n" +
                "d = ''.isalpha()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(false)},
                    { "b", PyBool.Create(true)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        public async Task IsAscii()
        {
            await runBasicTest(
                "a = '22'.isascii()\n" +
                "b = 'a '.isascii()\n" +
                "c = ''.isascii()\n" +
                "d = 'ắẮ'.isascii()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(true)},
                    { "b", PyBool.Create(true)},
                    { "c", PyBool.Create(true)},            // Empty string is consider ASCII
                    { "d", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        [Ignore("Doesn't work yet. The .NET regex I got doesn't even work with itself.")]
        public async Task IsAlnumUnicode()
        {
            await runBasicTest(
                "a = '22'.isalnum()\n" +
                "b = 'ae'.isalnum()\n" +
                "c = 'a-'.isalnum()\n" +
                "d = ''.isalnum()\n" +
                "e = 'ắẮằẰẵẴẳẲấẤầẦẫẪẩẨảẢạẠặẶậẬḁḀẚḃḂḅḄḇḆḉḈḋḊḑḐḍḌḓḒḏḎẟếẾềỀễỄểỂẽẼḝḜḗḖḕḔẻẺẹẸệỆḙḘḛḚḟḞḡḠḧḦḣḢḩḨḥḤḫḪẖḯḮỉỈịỊḭḬḱḰḳḲḵḴḷḶḹḸḽḼḻḺỻỺḿḾṁṀṃṂṅṄṇṆṋṊṉṈốỐồỒỗỖổỔṍṌṏṎṓṒṑṐỏỎớỚờỜỡỠởỞợỢọỌộỘṕṔṗṖṙṘṛṚṝṜṟṞṥṤṧṦṡṠṣṢṩṨẛẞẜẝẗṫṪṭṬṱṰṯṮṹṸṻṺủỦứỨừỪữỮửỬựỰụỤṳṲṷṶṵṴṽṼṿṾỽỼẃẂẁẀẘẅẄẇẆẉẈẍẌẋẊỳỲẙỹỸẏẎỷỶỵỴỿỾẑẐẓẒẕẔ'.isalnum()\n" +
                "f = 'a1'.isalnum()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(true)},
                    { "b", PyBool.Create(true)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                    { "e", PyBool.Create(true)},
                    { "f", PyBool.Create(true)},
                }), 1);
        }

        [Test]
        public async Task IsAlnum()
        {
            await runBasicTest(
                "a = '22'.isalnum()\n" +
                "b = 'ae'.isalnum()\n" +
                "c = 'a-'.isalnum()\n" +
                "d = ''.isalnum()\n" +
                "f = 'a1'.isalnum()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(true)},
                    { "b", PyBool.Create(true)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                    { "f", PyBool.Create(true)},
                }), 1);
        }

        [Test]
        public async Task IsDecimal()
        {
            await runBasicTest(
                "a = '22'.isdecimal()\n" +
                "b = '-2'.isdecimal()\n" +
                "c = '2.0'.isdecimal()\n" +
                "d = ''.isdecimal()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(true)},
                    { "b", PyBool.Create(false)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        public async Task IsLower()
        {
            await runBasicTest(
                "a = '22'.islower()\n" +
                "b = 'ae'.islower()\n" +
                "c = 'AE'.islower()\n" +
                "d = ''.islower()\n" +
                "f = 'a-'.islower()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(false)},
                    { "b", PyBool.Create(true)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                    { "f", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        public async Task IsNumeric()
        {
            await runBasicTest(
                "a = '22'.isnumeric()\n" +
                "b = '-2'.isnumeric()\n" +
                "c = '2.0'.isnumeric()\n" +
                "d = ''.isnumeric()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(true)},
                    { "b", PyBool.Create(false)},
                    { "c", PyBool.Create(false)},
                    { "d", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        public async Task IsSpace()
        {
            await runBasicTest(
                "a = '22'.isspace()\n" +
                "b = ' '.isspace()\n" +
                "c = ''.isspace()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(false)},
                    { "b", PyBool.Create(true)},
                    { "c", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        public async Task IsUpper()
        {
            await runBasicTest(
                "a = '22'.isupper()\n" +
                "b = 'ae'.isupper()\n" +
                "c = 'AE'.isupper()\n" +
                "d = ''.isupper()\n" +
                "f = 'a-'.isupper()\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyBool.Create(false)},
                    { "b", PyBool.Create(false)},
                    { "c", PyBool.Create(true)},
                    { "d", PyBool.Create(false)},
                    { "f", PyBool.Create(false)},
                }), 1);
        }

        [Test]
        public async Task Join()
        {
            await runBasicTest(
                "a = ', '.join(['hello', 'world!'])\n" +
                "b = ', '.join(['hello'])\n" +
                "c = ''.join(['hello', 'world!'])\n" +
                "d = ', '.join(['', ''])\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("hello, world!")},
                    { "b", PyString.Create("hello")},
                    { "c", PyString.Create("helloworld!")},
                    { "d", PyString.Create(", ")},
                }), 1);
        }

        [Test]
        public async Task ModBasicSingle()
        {
            await runBasicTest(
                "a = 'Hello, %s' % 'World!'\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("Hello, World!")},
                }), 1);
        }

        [Test]
        public async Task ModBasicTuple()
        {
            await runBasicTest(
                "a = '%s, %s' % ('Hello', 'World!')\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("Hello, World!")},
                }), 1);
        }

        [Test]
        public async Task Replace()
        {
            await runBasicTest(
                "a = 'dont replace anything'.replace('nothing', 'meow')\n" +
                "b = 'replace this'.replace('this', 'thatter')\n" +
                "c = 'replace this this'.replace('this', 'that', count=1)\n" +
                "d = 'first second'.replace('first', 'that')\n" +
                "e = 'snippity snip cut'.replace('snip', '')\n" +
                "f = ''.replace('this', 'that')\n",
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyString.Create("dont replace anything")},
                    { "b", PyString.Create("replace thatter")},
                    { "c", PyString.Create("replace that this")},
                    { "d", PyString.Create("that second")},
                    { "e", PyString.Create("pity  cut")},
                    { "f", PyString.Create("")},
                }), 1);
        }

    }

    [TestFixture]
    public class ConversionSpecifierTests
    {
        public static void TestSpecifier(string in_str, int start_idx, int end_idx, PyObject error, 
            string mapping_key,
            int width,
            int precision,
            bool use_alternate_form,
            bool left_adjusted,
            bool sign_pos_and_neg,
            bool zero_padded,
            bool space_before_pos)
        {
            var conv = new ConversionSpecifier();
            PyObject err;
            int ret_end_idx = conv.ParseFromString(in_str, start_idx, out err);
            Assert.That(error, Is.EqualTo(error));
            Assert.That(end_idx, Is.EqualTo(ret_end_idx));
            Assert.That(mapping_key, Is.EqualTo(conv.MappingKey));
            Assert.That(width, Is.EqualTo(conv.Width));
            Assert.That(precision, Is.EqualTo(conv.Precision));
            Assert.That(use_alternate_form, Is.EqualTo(conv.AlternateForm));
            Assert.That(left_adjusted, Is.EqualTo(conv.LeftAdjusted));
            Assert.That(sign_pos_and_neg, Is.EqualTo(conv.SignPosAndNeg));
            Assert.That(zero_padded, Is.EqualTo(conv.ZeroPadded));
            Assert.That(space_before_pos, Is.EqualTo(conv.SpaceBeforePos));
        }

        [Test]
        public void NoSpecifier()
        {
            TestSpecifier("%s", 1, 1, null, null, 0, 0, false, false, false, false, false);
        }

        [Test]
        public void WidthsAndPrecisions()
        {
            TestSpecifier("%123d", 1, 4, null, null, 123, 0, false, false, false, false, false);
            TestSpecifier("%12.3d", 1, 5, null, null, 12, 3, false, false, false, false, false);
            TestSpecifier("%.1d", 1, 3, null, null, 0, 1, false, false, false, false, false);
        }

        [Test]
        public void AllConversionOperators()
        {
            // CPython doesn't really care about orders and duplicates so we'll try some
            // funny combinations
            // >>> "Hey %# +-f" % 10.4
            // 'Hey +10.400000'
            // >>> "Hey %# +---+f" % 10.4
            // 'Hey +10.400000'
            // >>> "Hey %# +---+- +f" % 10.4
            TestSpecifier("%# +-0f", 1, 6, null, null, 0, 0, true, true, true, true, true);
            TestSpecifier("%# +-0--+f", 1, 9, null, null, 0, 0, true, true, true, true, true);
            TestSpecifier("%# +---+-0 +f", 1, 12, null, null, 0, 0, true, true, true, true, true);
        }
    }

    [TestFixture]
    public class PrintfStringTests
    {
        public async Task PrintfTest(string in_string, string expected_out, params object[] instances)
        {
            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            scheduler.SetInterpreter(interpreter);

            var format_out = await PrintfHelper.Format(interpreter, null, in_string, instances);
            Assert.That(format_out.Error, Is.Null);
            Assert.That(format_out.Formatted, Is.EqualTo(expected_out));
        }

        [Test]
        public async Task Passthrough()
        {
            await PrintfTest(
                "experiment",
                "experiment",
                new object[0]);
        }

        [Test]
        public async Task BasicDecimal()
        {
            await PrintfTest(
                "decimal %d here",
                "decimal 1337 here",
                new object[] { PyInteger.Create(1337) });
        }

        [Test]
        public async Task BasicString()
        {
            await PrintfTest(
                "string %s here",
                "string butt here",
                new object[] { PyString.Create("butt") });
        }

        [Test]
        public async Task InsertAtEnd()
        {
            await PrintfTest(
                "Hello, %s",
                "Hello, World!",
                new object[] { PyString.Create("World!") });
        }

        [Test]
        public async Task BasicReprString()
        {
            await PrintfTest(
                "string %r here",
                "string butt here",
                new object[] { PyString.Create("butt") });
        }

        [Test]
        public async Task EscapedDelimiter()
        {
            await PrintfTest(
                "what %%100",
                "what %100",
                new object[0]);
        }

        [Test]
        public async Task StringNumberPositiveNumber()
        {
            await PrintfTest(
                "what %10s",
                "what       butt",
                new object[] { PyString.Create("butt") });
        }

        [Test]
        public async Task StringNumberNegativeNumber()
        {
            await PrintfTest(
                "what %-10s",
                "what butt      ",
                new object[] { PyString.Create("butt") });
        }

        [Test]
        public async Task DecimalFormatting()
        {
            await PrintfTest(
                "%5d",
                " 1234",
                new object[] { PyInteger.Create(1234) });
            await PrintfTest(
                "%-5d",
                "1234 ",
                new object[] { PyInteger.Create(1234) });
            await PrintfTest(
                "%05d",
                "01234",
                new object[] { PyInteger.Create(1234) });
            await PrintfTest(
                "%-05d",
                "1234 ",
                new object[] { PyInteger.Create(1234) });
        }

        [Test]
        public async Task FloatFormattingPositive()
        {
            await PrintfTest(
                "%2.1f",
                "123.5",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%02.1f",
                "123.5",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%1.5f",
                "123.45000",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%05.1f",
                "123.5",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%010.1f",
                "00000123.5",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%3.0f",
                "123",
                new object[] { PyFloat.Create(123.45) });
        }

        // FYI: We're kind of phoning this one in. We'll lean on the .NET conversion
        // and even use its three digit output instead of Python's default two-digit output.
        [Test]
        public async Task ExponentFormatting()
        {
            await PrintfTest(
                "%1.1e",
                "1.2e+002",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%1.4e",
                "1.2345e+002",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%1.8e",
                "1.23450000e+002",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%1.8e",
                "-1.23450000e+002",
                new object[] { PyFloat.Create(-123.45) });
            await PrintfTest(
                "%+1.8e",
                "+1.23450000e+002",
                new object[] { PyFloat.Create(123.45) });
        }

        [Test]
        public async Task ExponentFormattingCapitalE()
        {
            await PrintfTest(
                "%1.1E",
                "1.2E+002",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%1.4E",
                "1.2345E+002",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%1.8E",
                "1.23450000E+002",
                new object[] { PyFloat.Create(123.45) });
            await PrintfTest(
                "%1.8E",
                "-1.23450000E+002",
                new object[] { PyFloat.Create(-123.45) });
            await PrintfTest(
                "%+1.8E",
                "+1.23450000E+002",
                new object[] { PyFloat.Create(123.45) });
        }

        [Test]
        public async Task FloatFormattingNegative()
        {
            await PrintfTest(
                "%-2.1f",
                "-123.5",
                new object[] { PyFloat.Create(-123.45) });
            await PrintfTest(
                "%-02.1f",
                "-123.5",
                new object[] { PyFloat.Create(-123.45) });
            await PrintfTest(
                "%-1.5f",
                "-123.45000",
                new object[] { PyFloat.Create(-123.45) });
            await PrintfTest(
                "%-05.1f",
                "-123.5",
                new object[] { PyFloat.Create(-123.45) });
            await PrintfTest(
                "%-010.1f",
                "-123.5    ",
                new object[] { PyFloat.Create(-123.45) });
            await PrintfTest(
                "%-3.0f",
                "-123",
                new object[] { PyFloat.Create(-123.45) });
        }
    }
}
