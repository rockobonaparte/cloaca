using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;
using LanguageImplementation;
using CloacaInterpreter;
using System;
using System.Threading.Tasks;

namespace CloacaTests
{
    /// <summary>
    /// Tests our helper for matching positional, keyword, defaults, vargs, and kwargs
    /// between parameters and their arguments. The different ways this could get done
    /// had gotten out of hand and was become cumbersome to hit in code tests. Also,
    /// the implementation of it was also getting out of hand and we really needed a helper.
    /// </summary>
    [TestFixture]
    public class ArgParamMatchTests
    {
        public void InOutTest(CodeObject co, object[][] ins, Dictionary<string, object>[] keywordsIn, object[][] outs)
        {
            for (int i = 0; i < ins.Length; ++i)
            {
                var outParams = ArgParamMatcher.Resolve(co, ins[i], keywordsIn[i]);
                Assert.That(outParams, Is.EqualTo(outs[i]), "Failed Test #" + (i + 1));
            }
        }

        [Test]
        public void OneToOne()
        {
            var co = new CodeObject(new byte[0]);
            co.ArgCount = 1;
            co.Defaults = new List<object>();
            co.VarNames.Add("onevar");

            var inParams = new object[1];
            var outParams = ArgParamMatcher.Resolve(co, inParams);
            Assert.That(outParams, Is.EqualTo(inParams));
        }

        [Test]
        public void OneDefault()
        {
            var co = new CodeObject(new byte[0]);
            co.ArgCount = 1;
            co.Defaults = new List<object>();
            co.Defaults.Add(-1);
            co.VarNames.Add("has_default");

            var inParams = new object[0];
            var outParams = ArgParamMatcher.Resolve(co, inParams);
            Assert.That(outParams, Is.EqualTo(new object[] { -1 }));
        }

        [Test]
        public void TwoParams()
        {
            var co = new CodeObject(new byte[0]);
            co.ArgCount = 2;
            co.Defaults = new List<object>();
            co.VarNames.Add("one");
            co.VarNames.Add("two");

            var inputs = new object[][]
            {
                new object[] { 1, 2 },
                new object[] { 3 },
                new object[] { },
            };
            var keywordsIn = new Dictionary<string, object>[]
            {
                null,
                new Dictionary<string, object> { { "two", 4 } },
                new Dictionary<string, object> { { "one", 5 }, { "two", 6 } },
            };
            var outputs = new object[][]
            {
                new object[] { 1, 2 },
                new object[] { 3, 4 },
                new object[] { 5, 6 },
            };

            InOutTest(co, inputs, keywordsIn, outputs);
        }

        [Test]
        public void TwoParamsOneDefault()
        {
            var co = new CodeObject(new byte[0]);
            co.ArgCount = 2;
            co.Defaults = new List<object>();
            co.Defaults.Add(-1);
            co.VarNames.Add("onevar");
            co.VarNames.Add("has_default");

            var inputs = new object[][]
            {
                new object[] { 1, 2 },
                new object[] { 3 },
                new object[] { },
                new object[] { },
                new object[] { 7 },
            };
            var keywordsIn = new Dictionary<string, object>[]
            {
                null,
                null,
                new Dictionary<string, object> { { "onevar", 4 }, { "has_default", 5 } },
                new Dictionary<string, object> { { "onevar", 6 } },
                new Dictionary<string, object> { { "has_default", 8 } },
            };
            var outputs = new object[][]
            {
                new object[] { 1, 2 },
                new object[] { 3, -1 },
                new object[] { 4, 5 },
                new object[] { 6, -1 },
                new object[] { 7, 8 },
            };

            InOutTest(co, inputs, keywordsIn, outputs);
        }

        [Test]
        public void VargsOnly()
        {
            var co = new CodeObject(new byte[0]);
            co.ArgCount = 0;
            co.Defaults = new List<object>();
            co.VarNames.Add("args");
            co.Flags |= CodeObject.CO_FLAGS_VARGS;

            var inputs = new object[][]
            {
                new object[] { 1 },
                new object[] { },
            };
            var keywordsIn = new Dictionary<string, object>[]
            {
                null,
                null,
            };
            var outputs = new object[][]
            {
                new object[] { PyTuple.Create(new object[] { 1 }) },
                new object[] { PyTuple.Create(new object[0]) }
            };

            InOutTest(co, inputs, keywordsIn, outputs);
        }

        [Test]
        public void ArgsDefaultsVargs()
        {
            var co = new CodeObject(new byte[0]);
            co.ArgCount = 4;
            co.Defaults = new List<object>();
            co.VarNames.Add("a");
            co.VarNames.Add("b");
            co.VarNames.Add("c");
            co.VarNames.Add("d");
            co.VarNames.Add("e");
            co.Defaults = new List<object>();
            co.Defaults.Add(-1);
            co.Defaults.Add(-2);
            co.Flags |= CodeObject.CO_FLAGS_VARGS;

            // The 5th case is commented out because it's actually illegal Python. I stubbed my toe on it when I created the arg param
            // matcher state machine. It choked there and I verified in Python that it wouldn't have worked.
            var inputs = new object[][]
            {
                new object[] { 1, 2, 3, 4, 5 },
                new object[] { 6, 7, 8, 9 },
                new object[] { 10, 11, 12 },
                new object[] { 13, 14 },
                //new object[] { },
                new object[] { },
            };
            var keywordsIn = new Dictionary<string, object>[]
            {
                null,
                null,
                null,
                null,
                //new Dictionary<string, object> { { "a", 15 }, { "b", 16 }, { "c", 17 }, { "d", 18 }, { "e", PyTuple.Create(new object[] { 19 }) } },
                new Dictionary<string, object> { { "a", 15 }, { "b", 16 }, { "c", 17 }, { "d", 18 } },
            };
            var outputs = new object[][]
            {
                new object[] { 1, 2, 3, 4, PyTuple.Create(new object[] { 5 }) },
                new object[] { 6, 7, 8, 9, PyTuple.Create(new object[0]) },
                new object[] { 10, 11, 12, -2, PyTuple.Create(new object[0]) },
                new object[] { 13, 14, -1, -2, PyTuple.Create(new object[0]) },
                //new object[] { 15, 16, 17, 18, PyTuple.Create(new object[] { 19 }) },
                new object[] { 15, 16, 17, 18, PyTuple.Create(new object[0]) },
            };

            InOutTest(co, inputs, keywordsIn, outputs);

        }

        /// <summary>
        /// Testing variable arguments followed by keyword-only defaults. The function has a definition like:
        /// def foo(*args, a=1, b=2):
        /// 
        /// a and b are actually keyword-only defaults.
        /// </summary>
        [Test]
        public void VargsKeywordDefaults()
        {
            var co = new CodeObject(new byte[0]);
            co.ArgCount = 0;
            co.Defaults = new List<object>();
            co.VarNames.Add("args");
            co.VarNames.Add("a");
            co.VarNames.Add("b");
            co.Defaults = new List<object>();
            co.KWDefaults = new List<object>();
            co.KWDefaults.Add(3);
            co.KWDefaults.Add(4);
            co.KWOnlyArgCount = 2;
            co.Flags |= CodeObject.CO_FLAGS_VARGS;

            var inputs = new object[][]
            {
                new object[] { 1, 2 },
                new object[] { 1, 2 },
                new object[] { 1, 2 },
                new object[] { 1, 2 },
                new object[] { },
                new object[] { },
            };
            var keywordsIn = new Dictionary<string, object>[]
            {
                new Dictionary<string, object> { },
                new Dictionary<string, object> { { "a", 200 }, { "b", 100 } },
                new Dictionary<string, object> { { "a", 200 }, },
                new Dictionary<string, object> { { "b", 100 }, },
                new Dictionary<string, object> { { "a", 200 }, },
                new Dictionary<string, object> { },
            };
            var outputs = new object[][]
            {
                new object[] { PyTuple.Create(new object[] { 1, 2 }), 3, 4 },
                new object[] { PyTuple.Create(new object[] { 1, 2 }), 200, 100 },
                new object[] { PyTuple.Create(new object[] { 1, 2 }), 200, 4 },
                new object[] { PyTuple.Create(new object[] { 1, 2 }), 3, 100 },
                new object[] { PyTuple.Create(new object[0]), 200, 4 },
                new object[] { PyTuple.Create(new object[0]), 3, 4 },
            };

            InOutTest(co, inputs, keywordsIn, outputs);

        }

    }

    [TestFixture]
    public class Basics : RunCodeTest
    {
        [Test]
        public async Task SimpleAssignment()
        {
            await runBasicTest("a = 10\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(10) }
            }), 1);
        }

        /// <summary>
        /// Dumb to do as a standalone program, but this is done in the REPL all the time.
        /// We didn't run this well before.
        /// </summary>
        [Test]
        public async Task SimpleAssignmentAndLoad()
        {
            await runBasicTest("a = 10\n" +
                         "a\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(10) }
            }), 1);
        }

        [Test]
        public async Task SimpleIntMath()
        {
            await runBasicTest("a = 10 * (2 + 4) / 3\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyFloat.Create(20.0) }
            }), 1);
        }

        [Test]
        public async Task SimpleFloatMath()
        {
            await runBasicTest("a = 10.0 * (2.0 + 4.0) / 3.0\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyFloat.Create(20.0) }
            }), 1);
        }

        [Test]
        public async Task NegativeNumber()
        {
            await runBasicTest(
                "a = 10\n" +
                "b = -a\n",
                 new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(10) },
                { "b", PyInteger.Create(-10) },
            }), 1);
        }

        [Test]
        public async Task ComprehensiveArithmeticOperators()
        {
            await runBasicTest(
                "x = 10\n" +
                "a = x + 2\n" +
                "b = x - 2\n" +
                "c = x * 2\n" +
                "d = x / 2\n" +
                "e = x % 9\n" +
                "f = x // 3\n" +
                "g = x ** 2\n" +
                "h = x & 2\n" +
                "i = x | 14\n" +
                "j = x ^ 2\n" +
                "k = x >> 2\n" +
                "l = x << 2\n"
                , new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(12) },
                { "b", PyInteger.Create(8) },
                { "c", PyInteger.Create(20) },
                { "d", PyFloat.Create(5.0) },
                { "e", PyInteger.Create(1) },
                { "f", PyInteger.Create(3) },
                { "g", PyInteger.Create(100) },
                { "h", PyInteger.Create(2) },
                { "i", PyInteger.Create(14) },
                { "j", PyInteger.Create(8) },
                { "k", PyInteger.Create(2) },
                { "l", PyInteger.Create(40) }
            }), 1);
        }

        [Test]
        public async Task RepeatedArithmeticOperators()
        {
            // Making sure that we're properly parsing and generating all of these when there's multiples of the operator.
            await runBasicTest(
                "x = 100\n" +
                "a = x + 2 + 3\n" +
                "b = x - 2 - 3\n" +
                "c = x * 2 * 3\n" +
                "d = x / 4 / 2\n" +
                "e = x % 9 % 3\n" +
                "f = x // 2 // 3\n" +
                "g = x ** 2 ** 3\n" +
                "h = x & 3 & 2\n" +
                "i = x | 13 | 1 \n" +
                "j = x ^ 2 ^ 1\n" +
                "k = x >> 2 >> 3\n" +
                "l = x << 2 << 3\n"
                , new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(100 + 2 + 3) },
                { "b", PyInteger.Create(100 - 2 - 3) },
                { "c", PyInteger.Create(100 * 2 * 3) },
                { "d", PyFloat.Create(100.0 / 4.0 / 2.0) },
                { "e", PyInteger.Create(100 % 9 % 3) },
                { "f", PyInteger.Create(100 / 2 / 3) },
                { "g", PyInteger.Create((BigInteger) Math.Pow(100.0, 8.0)) },          // 2 ** 3 gets evaluated first and becomes 8. This is what CPython does too!
                { "h", PyInteger.Create(100 & 3 & 2) },
                { "i", PyInteger.Create(100 | 13 | 1) },
                { "j", PyInteger.Create(100 ^ 2 ^ 1) },
                { "k", PyInteger.Create(100 >> 2 >> 3) },
                { "l", PyInteger.Create(100 << 2 << 3) }
            }), 1);
        }

        [Test]
        public async Task AssignmentOperators()
        {
            // https://www.w3schools.com/python/python_operators.asp
            // +=	x += 3	x = x + 3	
            // -=	x -= 3	x = x - 3	
            // *=	x *= 3	x = x * 3	
            // /=	x /= 3	x = x / 3	
            // %=	x %= 3	x = x % 3	
            // //=	x //= 3	x = x // 3	
            // **=	x **= 3	x = x ** 3	
            // &=	x &= 3	x = x & 3	
            // |=	x |= 3	x = x | 3	
            // ^=	x ^= 3	x = x ^ 3	
            // >>=	x >>= 3	x = x >> 3	
            // <<=	x <<= 3	x = x << 3
            await runBasicTest(
                "a = 10\n" +
                "b = 10\n" +
                "c = 10\n" +
                "d = 10\n" +
                "e = 10\n" +
                "f = 10\n" +
                "g = 10\n" +
                "h = 10\n" +
                "i = 10\n" +
                "j = 10\n" +
                "k = 10\n" +
                "l = 10\n" +
                "a += 2\n" +
                "b -= 2\n" +
                "c *= 2\n" +
                "d /= 2\n" +
                "e %= 9\n" +
                "f //= 3\n" +
                "g **= 2\n" +
                "h &= 2\n" +
                "i |= 14\n" +
                "j ^= 2\n" +
                "k >>= 2\n" +
                "l <<= 2\n"
                , new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(12) },
                { "b", PyInteger.Create(8) },
                { "c", PyInteger.Create(20) },
                { "d", PyFloat.Create(5.0) },
                { "e", PyInteger.Create(1) },
                { "f", PyInteger.Create(3) },
                { "g", PyInteger.Create(100) },
                { "h", PyInteger.Create(2) },
                { "i", PyInteger.Create(14) },
                { "j", PyInteger.Create(8) },
                { "k", PyInteger.Create(2) },
                { "l", PyInteger.Create(40) }
            }), 1);
        }

        [Test]
        public async Task SimpleStrAssign()
        {
            await runBasicTest("a = 'Hello!'\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyString.Create("Hello!") }
            }), 1);

            await runBasicTest("a = \"Hello!\"\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyString.Create("Hello!") }
            }), 1);
        }

        [Test]
        public async Task SimpleBoolAssign()
        {
            await runBasicTest("a = True\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyBool.True }
            }), 1);

            await runBasicTest("a = False\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyBool.False }
            }), 1);
        }

        [Test]
        public async Task Comparisons()
        {
            await runBasicTest("a = 10\n" +
                "b = a < 10\n" +
                "c = a == 10\n" +
                "d = a != 10\n" +
                "e = a > 10\n" +
                "f = a <= 10\n" +
                "g = a >= 10\n" +
                "h = a <> 10\n",
                new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(10) },
                { "b", PyBool.False },
                { "c", PyBool.True },
                { "d", PyBool.False },
                { "e", PyBool.False },
                { "f", PyBool.True },
                { "g", PyBool.True },
                { "h", PyBool.False },
            }), 1);
        }

        [Test]
        public async Task InvertWithNot()
        {
            await runBasicTest("a = True\n" +
                "b = not a\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyBool.True },
                { "b", PyBool.False }
            }), 1);
        }

        [Test]
        public async Task IsNoneIsNotNone()
        {
            await runBasicTest("b = a is None\n" +
                "c = a is not None\n",
            new Dictionary<string, object>
            {
                { "a", NoneType.Instance }
            },
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", NoneType.Instance },
                { "b", PyBool.True },
                { "c", PyBool.False }
            }), 1);

            // Now let's flip A around and make sure we're still cool.
            await runBasicTest("b = a is None\n" +
                "c = a is not None\n",
            new Dictionary<string, object>
            {
                { "a", PyInteger.Create(10) }
            },
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(10) },
                { "b", PyBool.False },
                { "c", PyBool.True }
            }), 1);
        }

        [Test]
        public async Task CantAssignToNone()
        {
            FrameContext runContext = null;
            Assert.ThrowsAsync<Exception>(
              async () => {
                  runContext = await runProgram(
                    "None = 1\n", new Dictionary<string, object>(), 1);
              }, "SyntaxError: can't assign to keyword (tried to assign to 'None')");
        }

        [Test]
        public async Task BasicWait()
        {
            await runBasicTest(
                "a = 10 * (2 + 4) / 3\n" +
                "wait\n" +
                "b = a + 3\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyFloat.Create(20.0) },
                { "b", PyFloat.Create(23.0) }
            }), 2);
        }

        [Test]
        public async Task BasicConditionalTrue()
        {
            await runBasicTest(
                "a = 10\n" +
                "if a == 10:\n" +
                "   a = 1\n" +
                "a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(2) }
            }), 1);
        }

        [Test]
        public async Task BasicConditionalExplicitTrue()
        {
            await runBasicTest(
                "a = 10\n" +
                "if True:\n" +
                "   a = 1\n" +
                "a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(2) }
            }), 1);
        }

        [Test]
        public async Task BasicConditionalOffTheEnd()
        {
            // Conditional is last opcode. We want to fall-through without going out of bounds
            await runBasicTest(
                "a = 9\n" +
                "if a == 10:\n" +
                "   a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(9) }
            }), 1);
        }

        [Test]
        public async Task BasicConditionalFalse()
        {
            await runBasicTest(
                "a = 10\n" +
                "if a != 10:\n" +
                "   a = 1\n" +
                "a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(11) }
            }), 1);
        }

        [Test]
        public async Task IfElseInvert()
        {
            await runBasicTest("blip = True\n" +
                         "if blip is True:\n" +
                         "   blip = False\n" +
                         "else:\n" +
                         "   blip = True\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "blip", PyBool.False }
            }), 1);

            await runBasicTest("blip = False\n" +
                         "if blip is True:\n" +
                         "   blip = False\n" +
                         "else:\n" +
                         "   blip = True\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "blip", PyBool.True }
            }), 1);
        }

        [Test]
        public async Task LogicOperations()
        {
            // When first implemented, this was generated a bunch of loads
            // and then storing the last value pushed on the stack to d.
            await runBasicTest("d = a and a or b and not c\n",
            new Dictionary<string, object>
            {
                { "a", PyBool.True },
                { "b", PyBool.True },
                { "c", PyBool.False }
            },
            new VariableMultimap(new TupleList<string, object>
            {
                { "d", PyBool.True }
            }), 1);

            await runBasicTest("d = a and a or b and not c\n",
            new Dictionary<string, object>
            {
                { "a", PyBool.False },
                { "b", PyBool.True },
                { "c", PyBool.True }
            },
            new VariableMultimap(new TupleList<string, object>
            {
                { "d", PyBool.False }
            }), 1);
        }

        [Test]
        public async Task ShortCircuitAnd()
        {
            await runBasicTest(
                        "a = None\n" +
                        "b = a is not None and a()\n",       // a() should never happen.
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", PyBool.False }
            }), 1);
        }

        [Test]
        public async Task ShortCircuitOr()
        {
            await runBasicTest(
                        "a = None\n" +
                        "b = a is None or a()\n",           // a() should never happen.
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", PyBool.True }
            }), 1);
        }

        /// <summary>
        /// The original implementation for logic was only generating code for two AND statements,
        /// but there could be more! So here's one with three.
        /// </summary>
        [Test]
        public async Task BigShortCircuitAnd()
        {
            await runBasicTest(
                        "a = []\n" +
                        "b = a is not None and len(a) > 0 and a[1]==2\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", PyBool.False }
            }), 1);
        }

        [Test]
        public async Task LogicIsNot()
        {
            // When first implemented, this was generated a bunch of loads
            // and then storing the last value pushed on the stack to d.
            await runBasicTest("a = False\n" +
                         "b = a is not True\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", PyBool.True }
            }), 1);
        }

        [Test]
        public async Task Unpack()
        {
            await runBasicTest(
                "a, b = [1, 2]\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(1) },
                { "b", PyInteger.Create(2) },
            }), 1);
        }

        // Found a more elaborate version of this in heapq. Using this to implement unpack.
        [Test]
        public async Task UnpackMultiassign()
        {
            await runBasicTest(
                "a, b = c = [1, 2]\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(1) },
                { "b", PyInteger.Create(2) },
                { "c", PyList.Create(new List<object>() {PyInteger.Create(1), PyInteger.Create(2)}) },
            }), 1);
        }

        [Test]
        public async Task Multiassign()
        {
            await runBasicTest(
                "a = b = 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(1) },
                { "b", PyInteger.Create(1) },
            }), 1);
        }

        [Test]
        public async Task WhileBasic()
        {
            await runBasicTest(
                "a = 0\n" +
                "while a < 3:\n" +
                "   a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(3) }
            }), 1);
        }

        [Test]
        public async Task WhileElse()
        {
            string program =
                "while a < 3:\n" +
                "   a = a + 1\n" +
                "else:\n" +
                "   a = a + 100\n";

            // Runs while loop, then the else clause
            await runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", PyInteger.Create(0) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(103) }
                }), 1);

            // Skips the while loop, runs the else clause
            await runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", PyInteger.Create(10) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(110) }
                }), 1);
        }

        [Test]
        public async Task SingleLayerIfElifElse()
        {
            string program =
                "if a == 10:\n" +
                "   a = 1\n" +
                "elif a == 11:\n" +
                "   a = 3\n" +
                "else:\n" +
                "   a = 5\n" +
                "a = a + 1\n";

            await runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", PyInteger.Create(10) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1);

            await runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", PyInteger.Create(11) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(4) }
                }), 1);

            await runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", PyInteger.Create(12) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(6) }
                }), 1);
        }

        [Test]
        public async Task NamesAreNotDefinedMultipleTimes()
        {
            FrameContext runContext = await runProgram(
                "class Foo:\n" +
                "  def __init__(self):\n" +
                "    self.a = 1\n" +
                "\n" +
                "foo = Foo()\n" +
                "a = 3\n" +
                "foo.a = 2\n" +
                "foo.a = a\n",
                new Dictionary<string, object>(), 1);

            Assert.That(runContext.LocalNames.Count, Is.EqualTo(3));
            Assert.That(runContext.LocalNames.Contains("Foo"));
            Assert.That(runContext.LocalNames.Contains("foo"));
            Assert.That(runContext.LocalNames.Contains("a"));
        }

        // TODO: Add test for basic parse error of things like missing newlines and poor indentation.
    }

    [TestFixture]
    public class FunctionTests : RunCodeTest
    {
        [Test]
        public async Task VoidIntFunction()
        {
            string program =
                "def foo():\n" +
                "   return 1\n" +
                "a = foo()\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(1) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task IntIntFunction()
        {
            string program =
                "def foo(x):\n" +
                "   return x+1\n" +
                "a = foo(3)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(4) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task Int2IntFunction()
        {
            // Using a non-communative operator will help validate ordering
            string program =
                "def foo(x, y):\n" +
                "   return x - y\n" +
                "a = foo(6, 2)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(4) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task DoubleDefine()
        {
            string program =
                "def foo():\n" +
                "   return 1\n" +
                "def foo():\n" +
                "   return 2\n" +
                "a = foo()\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1, new string[] { "foo" });

        }

        [Test]
        public async Task RecursionBasic()
        {
            string program =
                "def foo(x):\n" +
                "   if x > 1:\n" +
                "      return x\n" +
                "   else:\n" +
                "      return foo(x+1)\n" +
                "a = foo(0)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task InnerFunction()
        {
            string program =
                "def foo(x):\n" +
                "   def bar(y):\n" +
                "      return y+1\n" +
                "   return bar(x)\n" +
                "a = foo(0)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(1) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task InnerFunctionRecurse()
        {
            string program =
                "def foo(x):\n" +
                "   def bar(a, y):\n" +
                "      if a == 0:\n" +
                "         return y\n" +
                "      else:\n" +
                "         return bar(a-1, y+1)\n" +
                "   return bar(10, 2)\n" +
                "a = foo(2)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(20) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public async Task Vargs()
        {
            string program =
                "def varg_sum(*args):\n" +
                "   ret_sum = 0\n" +
                "   for arg in args:\n" +
                "      ret_sum += arg\n" +
                "   return ret_sum\n" +
                "a = varg_sum(1, 7, 11)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(19) }
                }), 1);
        }

        [Test]
        public async Task VargsAndKwOnly()
        {
            string program =
                "def varg_sum(*args, a=-1, b=2):\n" +
                "   ret_sum = 0\n" +
                "   for arg in args:\n" +
                "      ret_sum += arg + a + b\n" +
                "   return ret_sum\n" +
                "o = varg_sum()\n" +
                "a = varg_sum(1, 7, 11)\n" +
                "b = varg_sum(1, 7, 11, a=1)\n" +
                "c = varg_sum(1, 7, 11, b=1)\n" +
                "d = varg_sum(1, 7, 11, a=1, b=-2)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "o", PyInteger.Create(0) },
                    { "a", PyInteger.Create(22) },
                    { "b", PyInteger.Create(28) },
                    { "c", PyInteger.Create(19) },
                    { "d", PyInteger.Create(16) },
                }), 3);
        }

        [Test]
        [Ignore("Implement kwargs is a work-in-progress")]
        public async Task KwargsOnly()
        {
            string program =
                "def kwarg_math(**kwargs):\n" +
                "   return kwargs['a'] + 10 * kwargs['b']\n" +
                "a = kwarg_math(a=1, b=3)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(31) }
                }), 1);
        }

        [Test]
        public async Task OneDefaultVariable()
        {
            string program =
                "def defaults_math(a=1):\n" +
                "   return a + 10\n" +
                "result = defaults_math(a=2)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "result", PyInteger.Create(12) },
                }), 2);                 // Two iterations since we have to execute the a=1 default as a separate run.
        }

        [Test]
        public async Task DefaultCombinations()
        {
            string program =
                "def defaults_math(a=1, b=3):\n" +
                "   return a + 10 * b\n" +
                "a = defaults_math()\n" +
                "b = defaults_math(2)\n" +
                "c = defaults_math(2, 4)\n" +
                "d = defaults_math(a=2, b=4)\n" +
                "e = defaults_math(a=4, b=2)\n" +
                "f = defaults_math(a=2)\n" +
                "g = defaults_math(b=4)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(31) },
                    { "b", PyInteger.Create(32) },
                    { "c", PyInteger.Create(42) },
                    { "d", PyInteger.Create(42) },
                    { "e", PyInteger.Create(24) },
                    { "f", PyInteger.Create(32) },
                    { "g", PyInteger.Create(41) },
                }), 3);                 // a=1 and b=3 are all their own iterations
            AssertNoDotNetExceptions();
        }

        // TODO: [DEFAULTS SCOPE] Pass outer scope to defaults
        [Test]
        [Ignore("We don't convey the outer scope into these functions yet, but it is valid Python syntax to do so for defaults.")]
        public async Task UseOuterscopeInDefaults()
        {
            string program =
                "jkl = 33\n" +
                "def kwarg_math(a=1+jkl,b=3+jkl):\n" +
                "   return a-b\n" +
                "a = kwarg_math()\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(1+33-3-33) }
                }), 1);
        }

        [Test]
        public async Task PrecalculatedDefaults()
        {
            string program =
                "def kwarg_math(a=1+2*3-4,b=20-5+3):\n" +
                "   return a-b\n" +
                "a = kwarg_math()\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(1+2*3-4 - (20-5+3)) }
                }), 3);
        }

        [Test]
        public async Task DefaultsVargsArgs()
        {
            string program =
                "def varg_sum(initial, addon=0, *args):\n" +
                "   ret_sum = initial\n" +
                "   for arg in args:\n" +
                "      ret_sum += arg + addon\n" +
                "   return ret_sum\n" +
                "a = varg_sum(1, 7, 11)\n" +
                // [ARGPARAMMATCHER ERRORS] Generate errors when input arguments don't match requirements of code object.
                // "b = varg_sum(1, 7, 11, addon=-1)\n" +      // This should fail with TypeError: varg_sum() got multiple values for argument 'addon'
                // "b = varg_sum(1, addon=-1, 7, 11)\n" +      // SyntaxError: positional argument follows keyword argument
                "c = varg_sum(1)\n" +
                "d = varg_sum(2, addon=3)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(19) },
                    { "c", PyInteger.Create(1) },
                    { "d", PyInteger.Create(2) }        // There are now *args so addon actually doesn't get used
                }), 2);
        }

        [Test]
        [Ignore("Implementing defaults/kwargs is a work-in-progress. This isn't even syntactically correct right now; I'm not sure how to use mad1")]
        public async Task DefaultsVargsArgsKwargs()
        {
            string program =
                "def mad1(initial, addon=0, *numbers, **kwargs):\n" +
                "   ret_sum = initial\n" +
                "   for number in numbers:\n" +
                "      ret_sum += kwargs['mult1'] * number + addon\n" +
                "   return ret_sum\n" +
                "\n" +
                "def mad2(initial, *numbers, addon=0, **kwargs):\n" +
                "   ret_sum = initial\n" +
                "   for number in numbers:\n" +
                "      ret_sum += kwargs['mult2'] * number + addon\n" +
                "   return ret_sum\n" +
                "\n" +
                "kwarg = { 'mult1': 10, 'mult2': 100 }\n" +
                "a = mad1(1, 7, 11, kwarg)\n" +
                "b = mad1(1, addon=6, 11, kwarg)\n" +
                "c = mad1(1, addon=1, 11, 12, kwarg)\n" +
                "d = mad2(1, 7, 11, kwarg)\n" +
                "e = mad2(1, 11, addon=6, kwarg)\n" +
                "f = mad2(1, 11, 12, addon=1, kwarg)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    // These values have not been properly established yet since we're not even sure what are legal calls and what they do yet.
                    { "a", PyInteger.Create(19) },
                    { "b", PyInteger.Create(19) },
                    { "c", PyInteger.Create(19) },
                    { "d", PyInteger.Create(19) },
                    { "e", PyInteger.Create(19) },
                    { "f", PyInteger.Create(19) }
                }), 1);
        }

        // TODO: [CALL_FUNCTION_EX] use CALL_FUNCTION_EX when calling a function taking vargs that's being fed unpacked data
        /// <summary>
        /// This test isn't really testing vargs in any more meaningful way than we already are with the base Vargs test, but
        /// we are adding the unpacking operator because it often gets used for functions with variable arguments when fed a list.
        /// </summary>
        [Test]
        [Ignore("Variable arguments are not yet supported")]
        public async Task VargsUnpackingOperator()
        {
            string program =
                "def varg_sum(*args):\n" +
                "   ret_sum = 0\n" +
                "   for arg in args:\n" +
                "      ret_sum += arg\n" +
                "   return ret_sum\n" +
                "to_unpack = [1, 7, 11]\n" +
                "a = varg_sum(*to_unpack)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(19) }
                }), 1);
        }

    }
}
