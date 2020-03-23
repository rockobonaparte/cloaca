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
        #region Reads
        [Test]
        public async Task PyListReadPyInteger()
        {
            var list = PyList.Create();
            var stored = PyInteger.Create(1);
            PyListClass.append(list, stored);

            var scheduler = new Scheduler();
            var interpreter = new Interpreter(scheduler);
            var context = new FrameContext(null);

            var read = await SubscriptHelper.LoadSubscript(interpreter, context, list, PyInteger.Create(0));
            Assert.That(read, Is.EqualTo(stored));
        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task PyListReadInt32()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task ArrayReadPyInteger()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task ArrayReadInt32()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task IListReadPyInteger()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task IListReadInt32()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task IDictReadPyInteger()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task IDictReadInt32()
        {

        }

        #endregion Reads
        #region Writes

        [Test]
        public async Task PyListWritePyInteger()
        {
        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task PyListWriteInt32()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task ArrayWritePyInteger()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task ArrayWriteInt32()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task IListWritePyInteger()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task IListWriteInt32()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task IDictWritePyInteger()
        {

        }

        [Test]
        [Ignore("Test not yet implemented")]
        public async Task IDictWriteInt32()
        {

        }

        #endregion Writes
    }
}
