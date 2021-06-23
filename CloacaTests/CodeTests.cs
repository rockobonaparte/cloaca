﻿using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;
using LanguageImplementation;
using System;

namespace CloacaTests
{
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
        public void CantAssignToNone()
        {
            FrameContext runContext = null;
            Assert.Throws<Exception>(
              () => {
                  runProgram(
                    "None = 1\n", new Dictionary<string, object>(), 1, out runContext);
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
        public void NamesAreNotDefinedMultipleTimes()
        {
            FrameContext runContext = null;
            runProgram(
                "class Foo:\n" +
                "  def __init__(self):\n" +
                "    self.a = 1\n" +
                "\n" +
                "foo = Foo()\n" +
                "a = 3\n" +
                "foo.a = 2\n" +
                "foo.a = a\n",
                new Dictionary<string, object>(), 1, out runContext);

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
        [Ignore("Variable arguments are not yet supported")]
        public void Vargs()
        {
            string program =
                "def varg_sum(*args):\n" +
                "   ret_sum = 0\n" +
                "   for arg in args:\n" +
                "      ret_sum += arg\n" +
                "   return ret_sum\n" +
                "a = foo(1, 7, 11)\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(19) }
                }), 1, new string[] { "foo" });
        }

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
                "a = foo(*to_unpack)\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", PyInteger.Create(19) }
                }), 1, new string[] { "foo" });
        }

    }
}
