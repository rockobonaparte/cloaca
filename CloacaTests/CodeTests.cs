using System.Numerics;
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
                { "a", new PyInteger(10) }
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
                { "a", new PyInteger(10) }
            }), 1);
        }

        [Test]
        public void SimpleIntMath()
        {
            runBasicTest("a = 10 * (2 + 4) / 3\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", new PyInteger(20) }
            }), 1);
        }

        [Test]
        public void SimpleFloatMath()
        {
            runBasicTest("a = 10.0 * (2.0 + 4.0) / 3.0\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", new PyFloat(20.0) }
            }), 1);
        }

        [Test]
        public void SimpleStrAssign()
        {
            runBasicTest("a = 'Hello!'\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", new PyString("Hello!") }
            }), 1);

            runBasicTest("a = \"Hello!\"\n", new VariableMultimap(new TupleList<string, object>
            {
                { "a", new PyString("Hello!") }
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
                { "a", new PyInteger(10) },
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
                { "a", new PyInteger(10) }
            },
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", new PyInteger(10) },
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
                { "a", new PyInteger(20) },
                { "b", new PyInteger(23) }
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
                { "a", new PyInteger(2) }
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
                { "a", new PyInteger(2) }
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
                { "a", new PyInteger(9) }
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
                { "a", new PyInteger(11) }
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
                { "a", new PyInteger(3) }
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
                    { "a", new PyInteger(0) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", new PyInteger(103) }
                }), 1);

            // Skips the while loop, runs the else clause
            runBasicTest(program,
                new Dictionary<string, object> 
                {
                    { "a", new PyInteger(10) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", new PyInteger(110) }
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
                    { "a", new PyInteger(10) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", new PyInteger(2) }
                }), 1);

            runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", new PyInteger(11) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", new PyInteger(4) }
                }), 1);

            runBasicTest(program,
                new Dictionary<string, object>
                {
                    { "a", new PyInteger(12) }
                }, new VariableMultimap(new TupleList<string, object>
                {
                    { "a", new PyInteger(6) }
                }), 1);
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
                    { "a", new PyInteger(1) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void InnerAndOuterScopesLocal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   a = 2\n" +
                "foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", new PyInteger(1) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void InnerGlobal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   global a\n" +
                "   a = 2\n" +
                "foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", new PyInteger(2) }
                }), 1, new string[] { "foo" });
        }

        [Test]
        public void ImplicitlyUsesGlobal()
        {
            string program =
                "a = 1\n" +
                "def foo():\n" +
                "   b = a + 1\n" +
                "   return b\n" +
                "a = foo()\n";

            runBasicTest(program,
                new VariableMultimap(new TupleList<string, object>
                {
                    { "a", new PyInteger(2) }
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
                    { "a", new PyInteger(4) }
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
                    { "a", new PyInteger(4) }
                }), 1, new string[] { "foo" });
        }
    }
}
