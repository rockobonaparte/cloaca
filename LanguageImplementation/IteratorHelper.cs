using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LanguageImplementation
{
    public interface PyIterable
    {

        /// <summary>
        /// Implements __next__ as required by iterator.
        /// </summary>
        /// <param name="interpreter">The interpreter being used to invoke Next().
        /// Use this to call any helpers you may have.</param>
        /// <param name="context">FrameContext containing current run state. Pass
        /// along to any helpers (functions) that you also need to call.</param>
        /// <param name="self">A reference to this object that is being invoked.
        /// This keeps it consistent with the other iterators. Also, who knows?
        /// Maybe you want to play with the object IteratorMaker created for you!
        /// </param>
        /// <returns>The next object</returns>
        /// <exception cref="StopIterationException">The last element was already
        /// iterated.</exception>
        Task<object> Next(IInterpreter interpreter, FrameContext context, PyObject self);
    }

    /// <summary>
    /// A general-purpose iterator that works on anything that has a set of __len__ and
    /// __getitem__ dunders.
    /// </summary>
    public class LenGetItemIterator : PyIterable
    {
        private CodeObject len;
        private CodeObject getitem;
        private PyInteger i;
        private PyObject container;
        private PyInteger length;
        public LenGetItemIterator(PyObject container, CodeObject len, CodeObject getitem)
        {
            this.container = container;
            this.len = len;
            this.getitem = getitem;
            i = PyInteger.Create(0);
            length = -1;
        }

        public async Task<object> Next(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            if(length.InternalValue == 0)
            {
                var lengthPyInt = (PyInteger)await len.Call(interpreter, context, new object[] { container });
                length = (int) lengthPyInt.InternalValue;
            }
            if(i.InternalValue == length.InternalValue)
            {
                throw new StopIterationException();
            }
            else
            {
                var toReturn = await len.Call(interpreter, context, new object[] { container, i });
                i.InternalValue += 1;
                return toReturn;
            }
        }
    }

    public class IteratorMaker
    {
        public static PyObject MakeIterator(PyIterable iterable)
        {
            var iterator = new PyObject();
            iterator.__dict__["__next__"] = new WrappedCodeObject(iterable.GetType().GetMethod("Next"));
            return iterator;
        }
    }
}
