using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using CloacaInterpreter;
using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;
using LanguageImplementation;
using System;
using System.Threading.Tasks;

namespace CloacaTests
{
    /// <summary>
    /// Testing subscript helpers using direct calls.
    /// </summary>
    [TestFixture]
    public class DirectSubscriptingTests
    {
        private Scheduler scheduler;
        private Interpreter interpreter;
        private FrameContext context;

        [SetUp]
        public void Stage()
        {
            scheduler = new Scheduler();
            interpreter = new Interpreter(scheduler);
            context = new FrameContext(null);
        }

        #region Reads
        [Test]
        public async Task PyListReadPyInteger()
        {
            var list = PyList.Create();
            var stored = PyInteger.Create(1);
            PyListClass.append(list, stored);

            var read = await SubscriptHelper.LoadSubscript(interpreter, context, list, PyInteger.Create(0));
            Assert.That(read, Is.EqualTo(stored));
        }

        [Test]
        public async Task PyListReadInt32()
        {
            var list = PyList.Create();
            var stored = PyInteger.Create(1);
            PyListClass.append(list, stored);

            var read = await SubscriptHelper.LoadSubscript(interpreter, context, list, 0);
            Assert.That(read, Is.EqualTo(stored));
        }

        [Test]
        public async Task ArrayReadPyInteger()
        {
            var array = new int[] { 1 };
            var read = await SubscriptHelper.LoadSubscript(interpreter, context, array, PyInteger.Create(0));
            Assert.That(read, Is.EqualTo(1));
        }

        [Test]
        public async Task ArrayReadInt32()
        {
            var array = new int[] { 1 };
            var read = await SubscriptHelper.LoadSubscript(interpreter, context, array, 0);
            Assert.That(read, Is.EqualTo(1));
        }

        [Test]
        public async Task IListReadPyInteger()
        {
            var list = new List<int> { 1 };
            var read = await SubscriptHelper.LoadSubscript(interpreter, context, list, PyInteger.Create(0));
            Assert.That(read, Is.EqualTo(1));
        }

        [Test]
        public async Task IListReadInt32()
        {
            var list = new List<int> { 1 };
            var read = await SubscriptHelper.LoadSubscript(interpreter, context, list, 0);
            Assert.That(read, Is.EqualTo(1));
        }

        [Test]
        public async Task IDictReadPyInteger()
        {
            var dict = new Dictionary<PyInteger, int> { [PyInteger.Create(0)] = 1 };
            var read = await SubscriptHelper.LoadSubscript(interpreter, context, dict, PyInteger.Create(0));
            Assert.That(read, Is.EqualTo(1));
        }

        [Test]
        public async Task IDictReadInt32()
        {
            var dict = new Dictionary<int, int> { [0] = 1 };
            var read = await SubscriptHelper.LoadSubscript(interpreter, context, dict, 0);
            Assert.That(read, Is.EqualTo(1));
        }

        #endregion Reads
        #region Writes

        [Test]
        public async Task PyListWritePyInteger()
        {
            var list = PyList.Create();
            var stored = PyInteger.Create(1);
            PyListClass.append(list, stored);

            await SubscriptHelper.StoreSubscript(interpreter, context, list, PyInteger.Create(0), PyInteger.Create(2));
            Assert.That(list.list[0], Is.EqualTo(PyInteger.Create(2)));
        }

        [Test]
        public async Task PyListWriteInt32()
        {
            var list = PyList.Create();
            var stored = PyInteger.Create(1);
            PyListClass.append(list, stored);

            await SubscriptHelper.StoreSubscript(interpreter, context, list, 0, PyInteger.Create(2));
            Assert.That(list.list[0], Is.EqualTo(PyInteger.Create(2)));
        }

        [Test]
        public async Task ArrayWritePyInteger()
        {
            var array = new int[] { 1 };
            await SubscriptHelper.StoreSubscript(interpreter, context, array, PyInteger.Create(0), 2);
            Assert.That(array[0], Is.EqualTo(2));
        }

        [Test]
        public async Task ArrayWriteInt32()
        {
            var array = new int[] { 1 };
            await SubscriptHelper.StoreSubscript(interpreter, context, array, 0, 2);
            Assert.That(array[0], Is.EqualTo(2));
        }

        [Test]
        public async Task IListWritePyInteger()
        {
            var list = new List<int> { 1 };
            await SubscriptHelper.StoreSubscript(interpreter, context, list, PyInteger.Create(0), 2);
            Assert.That(list[0], Is.EqualTo(2));
        }

        [Test]
        public async Task IListWriteInt32()
        {
            var list = new List<int> { 1 };
            await SubscriptHelper.StoreSubscript(interpreter, context, list, 0, 2);
            Assert.That(list[0], Is.EqualTo(2));
        }

        [Test]
        public async Task IDictWritePyInteger()
        {
            var dict = new Dictionary<PyInteger, int> { [PyInteger.Create(0)] = 1 };
            await SubscriptHelper.StoreSubscript(interpreter, context, dict, PyInteger.Create(0), 2);
            Assert.That(dict[PyInteger.Create(0)], Is.EqualTo(2));
        }

        [Test]
        public async Task IDictWriteInt32()
        {
            var dict = new Dictionary<int, int> { [0] = 1 };
            await SubscriptHelper.StoreSubscript(interpreter, context, dict, 0, 2);
            Assert.That(dict[0], Is.EqualTo(2));
        }

        #endregion Writes
    }
}
