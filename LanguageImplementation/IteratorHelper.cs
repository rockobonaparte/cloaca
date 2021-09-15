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
        protected IPyCallable len;
        protected IPyCallable getitem;
        protected PyInteger i;
        protected PyObject container;
        protected PyInteger length;
        public LenGetItemIterator(PyObject container, IPyCallable len, IPyCallable getitem)
        {
            this.container = container;
            this.len = len;
            this.getitem = getitem;
            i = PyInteger.Create(0);
            length = -1;
        }

        public async virtual Task<object> Next(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            if(length.InternalValue == -1)
            {
                var lengthPyInt = (PyInteger)await len.Call(interpreter, context, new object[] { container });
                length = (int) lengthPyInt.InternalValue;
            }
            if(i.InternalValue == length.InternalValue)
            {
                context.CurrentException = new StopIteration();
                return NoneType.Instance;
            }
            else
            {
                var toReturn = await getitem.Call(interpreter, context, new object[] { container, i });
                i.InternalValue += 1;
                return toReturn;
            }
        }
    }

    public class ReversedLenGetItemIterator : LenGetItemIterator
    {
        public ReversedLenGetItemIterator(PyObject container, IPyCallable len, IPyCallable getitem) : base(container, len, getitem) { }

        public override async Task<object> Next(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            if (length.InternalValue == -1)
            {
                var lengthPyInt = (PyInteger)await len.Call(interpreter, context, new object[] { container });
                length = (int)lengthPyInt.InternalValue;
                i = PyInteger.Create(length.InternalValue-1);
            }

            if (i.InternalValue < 0)
            {
                context.CurrentException = new StopIteration();
                return NoneType.Instance;
            }
            else
            {
                var toReturn = await getitem.Call(interpreter, context, new object[] { container, i });
                i.InternalValue -= 1;
                return toReturn;
            }
        }
    }

    public class ZippedItemIterator : PyIterable
    {
        private PyObject[] iterators;
        private bool stopped;               // Used to keep raise StopIteration after the first one.

        public ZippedItemIterator(PyObject[] iterators)
        {
            this.iterators = iterators;
            stopped = false;
        }

        public async Task<object> Next(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            // Technicality: No items to iterate? We're done.
            if(this.iterators.Length == 0 || stopped)
            {
                stopped = true;
                context.CurrentException = new StopIteration();
                return null;
            }

            var values = new object[this.iterators.Length];
            PyTuple tuple = PyTuple.Create(values);
            for(int i = 0; i < iterators.Length; ++i)
            {
                try
                {
                    var nextFunc = (IPyCallable) iterators[i].__dict__["__next__"];
                    values[i] = await nextFunc.Call(interpreter, context, new object[] { iterators[i] });
                    if(context.CurrentException != null)
                    {
                        if(context.CurrentException is StopIteration)
                        {
                            stopped = true;
                        }
                        return null;
                    }
                } 
                catch(StopIterationException stopping_time)
                {
                    context.CurrentException = new StopIteration();
                    stopped = true;
                    return null;
                }
            }
            return tuple;
        }
    }

    /// <summary>
    /// Helper for creating __iter__ in IteratorMaker. This will give us a function that returns the
    /// existing self iterator.
    /// </summary>
    public class ReturnsSelfObject : IPyCallable
    {
        private object self;

        public ReturnsSelfObject(object self)
        {
            this.self = self;
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            return Task.FromResult(self);
        }
    }

    public class IteratorMaker
    {
        public static PyObject MakeIterator(PyIterable iterable)
        {
            var iterator = new PyObject();
            iterator.__dict__["__next__"] = new WrappedCodeObject(iterable.GetType().GetMethod("Next"), iterable);

            // Return ourselves for __iter__. This sounds stupid but comes up in CPython! The result is
            // the same object. Fun!
            iterator.__dict__["__iter__"] = new ReturnsSelfObject(iterator);

            return iterator;
        }

        public static async Task<PyObject> GetOrMakeIterator(IInterpreter interpreter, FrameContext context, object o)
        {
            var asPyIterable = o as PyIterable;
            if(asPyIterable != null)
            {
                return MakeIterator(asPyIterable);
            }

            var asPyObject = o as PyObject;
            if (asPyObject != null)
            {
                if (asPyObject.__dict__.ContainsKey("__iter__"))
                {
                    var iter_call = (IPyCallable)asPyObject.__dict__["__iter__"];
                    var result = await iter_call.Call(interpreter, context, new object[] { asPyObject });
                    return (PyObject) result;
                }
                else if (asPyObject.__dict__.ContainsKey("__len__") && asPyObject.__dict__.ContainsKey("__getitem__"))
                {
                    var len_dunder = (IPyCallable)asPyObject.__dict__["__len__"];
                    var getitem_dunder = (IPyCallable)asPyObject.__dict__["__getitem__"];
                    return MakeIterator(new LenGetItemIterator(asPyObject, len_dunder, getitem_dunder));
                }
                else
                {
                    var typeClass = (PyObject)asPyObject.__dict__["__class__"];
                    var typeName = typeClass.__dict__["__name__"].ToString();
                    context.CurrentException = new TypeError("TypeError: '" + typeName + "' is not iterable");
                    return null;
                }
            }
            else
            {
                context.CurrentException = new TypeError("TypeError: '" + o.GetType().Name + "' is not iterable");
                return null;
            }
        }
    }
}
