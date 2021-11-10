﻿using System.Numerics;
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
    /// Tests specific to lists. Some list basics may come up elsewhere, be we particularly hammer them here.
    /// </summary>
    [TestFixture]
    public class ListTests : RunCodeTest
    {
        [Test]
        public async Task Declare2dList()
        {
            await runBasicTest("l = [[1,2],[3,4]]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "l", PyList.Create(
                            new List<object>()
                            {
                                PyList.Create(new List<object>()
                                {
                                    PyInteger.Create(1), PyInteger.Create(2)
                                }),
                                PyList.Create(new List<object>()
                                {
                                    PyInteger.Create(3), PyInteger.Create(4)
                                })
                            })
                }
            }), 1);
        }

        [Test]
        public async Task AddLists()
        {
            await runBasicTest("a = [0] + [1]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1) }) }
            }), 1);
        }

        [Test]
        public async Task MultiplyList1()
        {
            await runBasicTest("a = [0] * 2\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(0) }) }
            }), 1);
        }

        [Test]
        public async Task MultiplyList2()
        {
            await runBasicTest("a = [0, 1] * 2\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(0), PyInteger.Create(1) }) }
            }), 1);
        }

        [Test]
        public async Task CreateSlice()
        {
            await runBasicTest("s1 = slice(0)\n" +
                               "s2 = slice(0, 1.0)\n" +
                               "s3 = slice(0, 1.0, 'what')\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "s1", PySlice.Create(PyInteger.Create(0)) },
                { "s2", PySlice.Create(PyInteger.Create(0), PyFloat.Create(1.0)) },
                { "s3", PySlice.Create(PyInteger.Create(0), PyFloat.Create(1.0), PyString.Create("what")) },
            }), 1);
        }

        [Test]
        public async Task BasicSliceOneArg()
        {
            await runBasicTest("s = slice(2)\n" +
                               "b = [0, 1, 2][s]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1)}) },
            }), 1);
        }

        [Test]
        public async Task BasicSliceTwoArgs()
        {
            await runBasicTest("s = slice(1, 2)\n" +
                               "b = [0, 1, 2][s]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", PyList.Create(new List<object>() { PyInteger.Create(1)}) },
            }), 1);
        }

        [Test]
        public async Task BasicSliceThreeArgs()
        {
            await runBasicTest("s = slice(0, 3, 2)\n" +
                               "b = [0, 1, 2][s]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "b", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(2)}) },
            }), 1);
        }

        [Test]
        public async Task SlicingObject()
        {
            await runBasicTest("a = [0, 1, 2][slice(0, 3)]\n" +
                               "b = [0, 1, 2][slice(1, 3)]\n" +
                               "c = [0, 1, 2][slice(2, 3)]\n" +
                               "d = [0, 1, 2][slice(3, 3)]\n" +
                               "e = [0, 1, 2][slice(0, 1)]\n" +
                               "f = [0, 1, 2][slice(1, 1)]\n" +
                               "g = [0, 1, 2][slice(0, 2)]\n" +
                               "h = [0, 1, 2][slice(1, 2)]\n" +
                               "i = [0, 1, 2][slice(0)]\n" +
                               "j = [0, 1, 2][slice(1)]\n" +
                               "k = [0, 1, 2][slice(2)]\n" +
                               "l = [0, 1, 2][slice(3)]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(2)}) },
                { "b", PyList.Create(new List<object>() { PyInteger.Create(1), PyInteger.Create(2)}) },
                { "c", PyList.Create(new List<object>() { PyInteger.Create(2)}) },
                { "d", PyList.Create(new List<object>()) },
                { "e", PyList.Create(new List<object>() { PyInteger.Create(0) }) },
                { "f", PyList.Create(new List<object>()) },
                { "g", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1) }) },
                { "h", PyList.Create(new List<object>() { PyInteger.Create(1) }) },
                { "i", PyList.Create(new List<object>()) },
                { "j", PyList.Create(new List<object>() { PyInteger.Create(0)}) },
                { "k", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1)}) },
                { "l", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(2)}) },
            }), 1);
        }

        [Test]
        public async Task SlicingNegativeObject()
        {
            await runBasicTest("a = [0, 1, 2][slice(-0, 3)]\n" +
                               "b = [0, 1, 2][slice(-1, 3)]\n" +
                               "c = [0, 1, 2][slice(-2, 3)]\n" +
                               "d = [0, 1, 2][slice(-3, 3)]\n" +
                               "e = [0, 1, 2][slice(-1, 0)]\n" +
                               "f = [0, 1, 2][slice(-1, -1)]\n" +
                               "g = [0, 1, 2][slice(0, -2)]\n" +
                               "h = [0, 1, 2][slice(-2, -1)]\n" +
                               "i = [0, 1, 2][slice(-1)]\n" +
                               "j = [0, 1, 2][slice(-2)]\n" +
                               "k = [0, 1, 2][slice(-3)]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(2)}) },
                { "b", PyList.Create(new List<object>() { PyInteger.Create(2)}) },
                { "c", PyList.Create(new List<object>() { PyInteger.Create(1), PyInteger.Create(2)}) },
                { "d", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(2)}) },
                { "e", PyList.Create(new List<object>()) },
                { "f", PyList.Create(new List<object>()) },
                { "g", PyList.Create(new List<object>() { PyInteger.Create(0) }) },
                { "h", PyList.Create(new List<object>() { PyInteger.Create(1) }) },
                { "i", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1) }) },
                { "j", PyList.Create(new List<object>() { PyInteger.Create(0) }) },
                { "k", PyList.Create(new List<object>()) },
            }), 1);
        }

        [Test]
        public async Task Slicing()
        {
            await runBasicTest("a = [0, 1, 2][0:]\n" +
                               "b = [0, 1, 2][1:]\n" +
                               "c = [0, 1, 2][2:]\n" +
                               "d = [0, 1, 2][3:]\n" +
                               "e = [0, 1, 2][0:1]\n" +
                               "f = [0, 1, 2][1:1]\n" +
                               "g = [0, 1, 2][0:2]\n" +
                               "h = [0, 1, 2][1:2]\n" +
                               "i = [0, 1, 2][:0]\n" +
                               "j = [0, 1, 2][:1]\n" +
                               "k = [0, 1, 2][:2]\n" +
                               "l = [0, 1, 2][:3]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(2)}) },
                { "b", PyList.Create(new List<object>() { PyInteger.Create(1), PyInteger.Create(2)}) },
                { "c", PyList.Create(new List<object>() { PyInteger.Create(2)}) },
                { "d", PyList.Create(new List<object>()) },
                { "e", PyList.Create(new List<object>() { PyInteger.Create(0) }) },
                { "f", PyList.Create(new List<object>()) },
                { "g", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1) }) },
                { "h", PyList.Create(new List<object>() { PyInteger.Create(1) }) },
                { "i", PyList.Create(new List<object>()) },
                { "j", PyList.Create(new List<object>() { PyInteger.Create(0)}) },
                { "k", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1)}) },
                { "l", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(2)}) },
            }), 1);
        }

        [Test]
        public async Task SlicingNegative()
        {
            await runBasicTest(
                               "a = [0, 1, 2][-0:]\n" +             // Negative zero just to be a prick
                               "b = [0, 1, 2][-1:]\n" +
                               "c = [0, 1, 2][-2:]\n" +
                               "d = [0, 1, 2][-3:]\n" +
                               "e = [0, 1, 2][-1:0]\n" +
                               "f = [0, 1, 2][-1:-1]\n" +
                               "g = [0, 1, 2][0:-2]\n" +
                               "h = [0, 1, 2][-2:-1]\n" +
                               "i = [0, 1, 2][:-1]\n" +
                               "j = [0, 1, 2][:-2]\n" +
                               "k = [0, 1, 2][:-3]\n",
            new VariableMultimap(new TupleList<string, object>
            {
                { "a", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(2)}) },
                { "b", PyList.Create(new List<object>() { PyInteger.Create(2)}) },
                { "c", PyList.Create(new List<object>() { PyInteger.Create(1), PyInteger.Create(2)}) },
                { "d", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1), PyInteger.Create(2)}) },
                { "e", PyList.Create(new List<object>()) },
                { "f", PyList.Create(new List<object>()) },
                { "g", PyList.Create(new List<object>() { PyInteger.Create(0) }) },
                { "h", PyList.Create(new List<object>() { PyInteger.Create(1) }) },
                { "i", PyList.Create(new List<object>() { PyInteger.Create(0), PyInteger.Create(1) }) },
                { "j", PyList.Create(new List<object>() { PyInteger.Create(0) }) },
                { "k", PyList.Create(new List<object>()) },
            }), 1);
        }
    }
}
