using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;
using LanguageImplementation;
using System;
using System.Threading.Tasks;

namespace CloacaTests
{
    class ArgParamMatcher
    {
        // TODO [ARGPARAMMATCHER ERRORS] Generate errors when input arguments don't match requirements of code object.
        public static object[] Resolve(CodeObject co, object[] inArgs, Dictionary<string, object> keywords=null)
        {
            bool hasVargs = (co.Flags & CodeObject.CO_FLAGS_VARGS) > 0;
            var defaultsStart = co.ArgCount - co.Defaults.Count;

            // The number of actual output arguments is not given straightforwardly. ArgCount gets use
            // the first positional and keyword arguments, but then we might have *args and **kwargs, which
            // are designated by flags.
            var outArgsLength = co.ArgCount;

            var num_vargs = inArgs.Length - co.ArgCount;
            object[] vargs = null;
            if (hasVargs)
            { 
                if (num_vargs > 0)
                {
                    vargs = new object[num_vargs];
                }
                else
                {
                    vargs = new object[num_vargs];
                }
                outArgsLength += 1;
            }

            var outArgs = new object[outArgsLength];

            int inArg = 0;
            for(int outArgIdx = 0; outArgIdx < outArgs.Length; ++outArgIdx)
            {
                if(inArg >= inArgs.Length)
                {
                    // Out of inputs, now lean on defaults arguments
                    var varName = co.VarNames[inArg];
                    if (keywords != null && keywords.ContainsKey(varName))
                    {
                        // Keyword argument
                        outArgs[outArgIdx] = keywords[varName];
                    }
                    else 
                    {
                        // Use the default
                        if (hasVargs && inArg == co.ArgCount)
                        {
                            // If it's a variable argument (*args) then the default is an empty tuple.
                            outArgs[outArgIdx] = PyTuple.Create(vargs);
                        }
                        else
                        {
                            outArgs[outArgIdx] = co.Defaults[defaultsStart - inArg];
                        }
                    }
                    inArg += 1;
                }
                else if(hasVargs && inArg >= co.ArgCount)
                {
                    // Variable arguments (*args)
                    while (inArg < inArgs.Length)
                    {
                        vargs[inArg - co.ArgCount] = inArgs[inArg];
                        ++inArg;
                    }
                    outArgs[outArgIdx] = PyTuple.Create(vargs);
                }
                else
                {
                    // Conventional, positional argument
                    outArgs[outArgIdx] = inArgs[inArg];
                    inArg += 1;
                }
            }

            return outArgs;
        }
    }

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
            for(int i = 0; i < ins.Length; ++i)
            {
                var outParams = ArgParamMatcher.Resolve(co, ins[i], keywordsIn[i]);
                Assert.That(outParams, Is.EqualTo(outs[i]));                
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
    }

    [TestFixture]
    public class Basics : RunCodeTest
    {
        [Test]
        public void SimpleAssignment()
        {
            runBasicTest("a = 10\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(10) }
            }), 1);
        }

        /// <summary>
        /// Dumb to do as a standalone program, but this is done in the REPL all the time.
        /// We didn't run this well before.
        /// </summary>
        [Test]
        public void SimpleAssignmentAndLoad()
        {
            runBasicTest("a = 10\n" +
                         "a\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(10) }
            }), 1);
        }

        [Test]
        public void SimpleIntMath()
        {
            runBasicTest("a = 10 * (2 + 4) / 3\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyFloat.Create(20.0) }
            }), 1);
        }

        [Test]
        public void SimpleFloatMath()
        {
            runBasicTest("a = 10.0 * (2.0 + 4.0) / 3.0\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyFloat.Create(20.0) }
            }), 1);
        }

        [Test]
        public void ComprehensiveArithmeticOperators()
        {
            runBasicTest(
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
        public void RepeatedArithmeticOperators()
        {
            // Making sure that we're properly parsing and generating all of these when there's multiples of the operator.
            runBasicTest(
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
        public void AssignmentOperators()
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
            runBasicTest(
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
        public void SimpleStrAssign()
        {
            runBasicTest("a = 'Hello!'\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyString.Create("Hello!") }
            }), 1);

            runBasicTest("a = \"Hello!\"\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyString.Create("Hello!") }
            }), 1);
        }

        [Test]
        public void SimpleBoolAssign()
        {
            runBasicTest("a = True\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyBool.True }
            }), 1);

            runBasicTest("a = False\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyBool.False }
            }), 1);
        }

        [Test]
        public void Comparisons()
        {
            runBasicTest("a = 10\n" +
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
        public void InvertWithNot()
        {
            runBasicTest("a = True\n" +
                "b = not a\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyBool.True },
                { "b", PyBool.False }
            }), 1);
        }

        [Test]
        public void IsNoneIsNotNone()
        {
            runBasicTest("b = a is None\n" +
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
            runBasicTest("b = a is None\n" +
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
        public void BasicWait()
        {
            runBasicTest(
                "a = 10 * (2 + 4) / 3\n" +
                "wait\n" +
                "b = a + 3\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyFloat.Create(20.0) },
                { "b", PyFloat.Create(23.0) }
            }), 2);
        }

        [Test]
        public void BasicConditionalTrue()
        {
            runBasicTest(
                "a = 10\n" +
                "if a == 10:\n" +
                "   a = 1\n" +
                "a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(2) }
            }), 1);
        }

        [Test]
        public void BasicConditionalExplicitTrue()
        {
            runBasicTest(
                "a = 10\n" +
                "if True:\n" +
                "   a = 1\n" +
                "a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(2) }
            }), 1);
        }

        [Test]
        public void BasicConditionalOffTheEnd()
        {
            // Conditional is last opcode. We want to fall-through without going out of bounds
            runBasicTest(
                "a = 9\n" +
                "if a == 10:\n" +
                "   a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(9) }
            }), 1);
        }

        [Test]
        public void BasicConditionalFalse()
        {
            runBasicTest(
                "a = 10\n" +
                "if a != 10:\n" +
                "   a = 1\n" +
                "a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(11) }
            }), 1);
        }

        [Test]
        public void IfElseInvert()
        {
            runBasicTest("blip = True\n" +
                         "if blip is True:\n" +
                         "   blip = False\n" +
                         "else:\n" +
                         "   blip = True\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "blip", PyBool.False }
            }), 1);

            runBasicTest("blip = False\n" +
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
        public void LogicOperations()
        {
            // When first implemented, this was generated a bunch of loads
            // and then storing the last value pushed on the stack to d.
            runBasicTest("d = a and a or b and not c\n",
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

            runBasicTest("d = a and a or b and not c\n",
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
        public void LogicIsNot()
        {
            // When first implemented, this was generated a bunch of loads
            // and then storing the last value pushed on the stack to d.
            runBasicTest("a = False\n" +
                         "b = a is not True\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", PyBool.True }
            }), 1);
        }

        [Test]
        public void WhileBasic()
        {
            runBasicTest(
                "a = 0\n" +
                "while a < 3:\n" +
                "   a = a + 1\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyInteger.Create(3) }
            }), 1);
        }

        [Test]
        public void WhileElse()
        {
            string program =
                "while a < 3:\n" +
                "   a = a + 1\n" +
                "else:\n" +
                "   a = a + 100\n";

            // Runs while loop, then the else clause
            runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", PyInteger.Create(0) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(103) }
                }), 1);

            // Skips the while loop, runs the else clause
            runBasicTest(program,
                new Dictionary<string, object> 
                {
                    { "a", PyInteger.Create(10) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(110) }
                }), 1);
        }

        [Test]
        public void SingleLayerIfElifElse()
        {
            string program =
                "if a == 10:\n" +
                "   a = 1\n" +
                "elif a == 11:\n" +
                "   a = 3\n" +
                "else:\n" +
                "   a = 5\n" +
                "a = a + 1\n";

            runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", PyInteger.Create(10) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1);

            runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", PyInteger.Create(11) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(4) }
                }), 1);

            runBasicTest(program,
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
            Assert.That(runContext.Names.Count, Is.EqualTo(1));
            Assert.That(runContext.Names.Contains("a"));
        }

        // TODO: Add test for basic parse error of things like missing newlines and poor indentation.
    }

    [TestFixture]
    public class FunctionTests : RunCodeTest
    {
        [Test]
        public void VoidIntFunction()
        {
            string program =
                "def foo():\n" +
                "   return 1\n" +
                "a = foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(1) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void IntIntFunction()
        {
            string program =
                "def foo(x):\n" +
                "   return x+1\n" +
                "a = foo(3)\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(4) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void Int2IntFunction()
        {
            // Using a non-communative operator will help validate ordering
            string program =
                "def foo(x, y):\n" +
                "   return x - y\n" +
                "a = foo(6, 2)\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(4) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void DoubleDefine()
        {
            string program =
                "def foo():\n" +
                "   return 1\n" +
                "def foo():\n" +
                "   return 2\n" +
                "a = foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(2) }
                }), 1, new string[] { "foo" });

        }

        [Test]
        public void Vargs()
        {
            string program =
                "def varg_sum(*args):\n" +
                "   ret_sum = 0\n" +
                "   for arg in args:\n" +
                "      ret_sum += arg\n" +
                "   return ret_sum\n" +
                "a = varg_sum(1, 7, 11)\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(19) }
                }), 1);
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
                "def varg_sum(initial, *args, addon=0):\n" +
                "   ret_sum = initial\n" +
                "   for arg in args:\n" +
                "      ret_sum += arg + addon\n" +
                "   return ret_sum\n" +
                "a = varg_sum(1, 7, 11)\n";
                //"b = varg_sum(1, 7, 11, addon=-1)\n" +
                //"c = varg_sum(1)\n" +
                //"d = varg_sum(2, addon=3)\n";

            await runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(19) },
                    { "b", PyInteger.Create(17) },      // *args only has [7, 11] so we only add -1 twice.
                    { "c", PyInteger.Create(1) },
                    { "d", PyInteger.Create(2) }        // There are now *args so addon actually doesn't get used
                }), 1);
        }

        [Test]
        [Ignore("Implementing defaults/kwargs is a work-in-progress. This isn't even syntactically correct right now; I'm not sure how to use mad1")]
        public void DefaultsVargsArgsKwargs()
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

            runBasicTest(program,
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
        public void VargsUnpackingOperator()
        {
            string program =
                "def varg_sum(*args):\n" +
                "   ret_sum = 0\n" +
                "   for arg in args:\n" +
                "      ret_sum += arg\n" +
                "   return ret_sum\n" +
                "to_unpack = [1, 7, 11]\n" +
                "a = varg_sum(*to_unpack)\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(19) }
                }), 1);
        }

    }
}
