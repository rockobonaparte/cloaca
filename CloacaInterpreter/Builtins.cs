using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaInterpreter
{
    public class Builtins
    {
        /// <summary>
        /// Implementation of the super() "pseudo-builtin." It's not considered one per-se, but it functionally
        /// might as well be one.
        /// 
        /// To figure out the self pointer, it inspects the frame to extract self. This is roughly how CPython
        /// does it too!
        /// </summary>
        /// <param name="context">The frame that has invoked super()</param>
        /// <returns>PySuperType wrapping the self for this class and its class type (per Python data model).</returns>
        public static PySuper super(FrameContext context)
        {
            // Believe it or not, this is very similar to how CPython does this! It grabs the code object and the frame and just
            // infers self from it.
            if (context.Locals.Count == 0)
            {
                throw new IndexOutOfRangeException("getSuperClass found no locals from which to steal the class' self pointer.");
            }

            var self = context.LocalFasts[0] as PyObject;
            if (self == null)
            {
                throw new InvalidCastException("getSuperClass could not convert first local (assumed to be self) to PyObject. Element is: " + self);
            }

            // TODO: Shouldn't I be able to use self.__bases__ directly? I suspect that needs to be plumbed.
            if (self.__class__ == null)
            {
                throw new NullReferenceException("getSuperClass needed class information from a self pointer that has no __class__ defined.");
            }

            if (self.__class__.__bases__ == null || self.__class__.__bases__.Length == 0)
            {
                throw new Exception("getSuperClass could not find a superclass for the current context.");
            }

            // TODO: Yeah this needs to deal with multiple bases and method resolution order.
            return PySuper.Create(self, self.__class__.__bases__[0]);
        }

        public static bool isinstance(PyObject obj, PyClass _class)
        {
            return obj.__class__ == _class;
        }

        public static bool issubclass(PyClass child, PyClass parent)
        {
            // Same class also qualifies as true; it doesn't technically have to be a child.
            if(child == parent)
            {
                return true;
            }               
            if(child.__bases__ == null)
            {
                return false;
            }
            foreach(var __base__ in child.__bases__)
            {                
                if(__base__ == parent)
                {
                    return true;
                }
                else
                {
                    if(isinstance(__base__, parent))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Internally-consumed variation on issubclass that assesses if a given object is a child of a 
        /// given class. This is a simplified helper for getting the type of o first and then figuring out
        /// issubclass on that type.
        /// </summary>
        /// <param name="o">The object to test.</param>
        /// <param name="parent">The type of the class to test against this object</param>
        /// <returns>True if o's class type is derived from parent. False otherwise.</returns>
        public static bool issubclass(PyObject o, PyClass parent)
        {
            return issubclass(builtin_type(o), parent);
        }

        /// <summary>
        /// Implements the dir() command used to dump methods and properties of this PyObject.
        /// </summary>
        /// <param name="o">The object to inspect</param>
        /// <returns>A PyList of the names of the methods and properties of this PyObject.</returns>        
        //public static async Task<PyList> dir(IInterpreter interpreter, FrameContext context, PyObject o)
        public static async Task<object> dir(IInterpreter interpreter, FrameContext context, PyObject o)
        {
            // TODO: Figure out how to switch to Task<PyList> signature without everything hanging.
            var internalList = new List<object>();
            foreach(var name in o.__dict__.Keys)
            {
                internalList.Add(PyString.Create(name));
            }

            // Alphabetize them. It's how Python does it and it is quite useful for scanning through the output anyways.
            internalList.Sort((a, b) => a.ToString().CompareTo(b.ToString()));

            var retList = (PyList) await PyListClass.Instance.Call(interpreter, context, new object[0]);
            retList.SetList(internalList);
            return retList;
        }

        /// <summary>
        /// Implements the len() command to get the length of containers.
        /// </summary>
        /// <param name="o">The object to inspect</param>
        /// <returns>A PyList of the names of the methods and properties of this PyObject.</returns>        
        public static async Task<PyInteger> len(IInterpreter interpreter, FrameContext context, object o)
        {
            var asPyObject = o as PyObject;
            if(asPyObject != null)
            {
                if(!asPyObject.__dict__.ContainsKey("__len__"))
                {
                    throw new Exception("TypeError: object of type " + asPyObject.__class__.Name + " has no len()");
                }
                else
                {
                    var callable_len = asPyObject.__dict__["__len__"] as IPyCallable;
                    if(callable_len == null)
                    {
                        // Yeah, same error as if __len__ was not found in the first place...
                        throw new Exception("TypeError: object of type " + asPyObject.__class__.Name + " has no len()");
                    }
                    else
                    {
                        var retVal = await callable_len.Call(interpreter, context, new object[] { o });
                        var asPyInteger = retVal as PyInteger;
                        if(asPyInteger == null)
                        {
                            // Yeah, same error as if __len__ if it's a function but it returns moon crap.
                            throw new Exception("TypeError: object of type " + asPyObject.__class__.Name + " has no len()");
                        }
                        return asPyInteger;
                    }
                }
            }

            var asArray = o as Array;
            if(asArray != null)
            {
                return PyInteger.Create(asArray.Length);
            }

            // Still here? This might be, uh, an IEnumerable<T> with a Count property...
            var asCountProperty = o.GetType().GetProperty("Count");
            if (asCountProperty != null)
            {
                return PyInteger.Create((int) asCountProperty.GetValue(o));
            }

            // No? Look for a Length
            var asLengthProperty = o.GetType().GetProperty("Length");
            if (asLengthProperty != null)
            {
                return PyInteger.Create((int)asLengthProperty.GetValue(o));
            }

            throw new Exception("TypeError: cannot calculate length for object of type " + o.GetType().Name);
        }

        public static PyClass builtin_type(PyObject obj)
        {
            return obj.__class__;
        }

        public static PyInteger int_builtin(object o)
        {
            if(PyNetConverter.CanConvert(o.GetType(), typeof(PyInteger)))
            {
                return (PyInteger) PyNetConverter.Convert(o, typeof(PyInteger));
            }
            else
            {
                throw new InvalidCastException("Cannot convert " + o.GetType() + " to an int.");
            }
        }

        public static PyFloat float_builtin(object o)
        {
            if (PyNetConverter.CanConvert(o.GetType(), typeof(PyFloat)))
            {
                return (PyFloat)PyNetConverter.Convert(o, typeof(PyFloat));
            }
            else
            {
                throw new InvalidCastException("Cannot convert " + o.GetType() + " to a float.");
            }
        }

        public static PyBool bool_builtin(object o)
        {
            if (PyNetConverter.CanConvert(o.GetType(), typeof(PyBool)))
            {
                return (PyBool)PyNetConverter.Convert(o, typeof(PyBool));
            }
            else
            {
                throw new InvalidCastException("Cannot convert " + o.GetType() + " to a bool.");
            }
        }

        public static async Task<PyString> str_builtin(IInterpreter interpreter, FrameContext context, object o)
        {
            if(o == null)
            {
                return PyString.Create("null");
            }

            var asPyObject = o as PyObject;
            if (asPyObject != null)
            {
                var str_func = (IPyCallable)((PyObject)asPyObject).__getattribute__(PyClass.__STR__);

                var returned = await str_func.Call(interpreter, context, new object[0]);
                if (returned != null)
                {
                    var asPyString = (PyString)returned;
                    return asPyString;
                }
                else
                {
                    return PyString.Create("null");
                }
            }
            else
            {
                return PyString.Create(o.ToString());
            }
        }

        public static PyRange range_builtin(FrameContext context, int min, int max, int step)
        {
            if(step == 0)
            {
                context.CurrentException = new ValueError("range() arg 3 must not be zero");
            }
            return PyRange.Create(min, max, step);
        }

        public static PyRange range_builtin(int min, int max)
        {
            return PyRange.Create(min, max, 1);
        }

        public static PyRange range_builtin(int max)
        {
            return PyRange.Create(0, max, 1);
        }


        public static async Task<PyList> list_builtin(IInterpreter interpreter, FrameContext context, params object[] args)
        {
            // Our method resolution is not particularly good if we tried to overload this and I didn't want to deal with
            // the syntactic overhead, so I just the no-arg and one-arg versions in the same function.
            if(args == null || args.Length == 0)
            {
                return PyList.Create();
            }
            else if(args.Length > 1)
            {
                context.CurrentException = new TypeError("TypeError: list expected at most 1 argument, got " + args.Length);
                return null;
            }

            // Everything else is dedicated to the case where one argument was actually given.
            var o = args[0];

            var itr = await IteratorMaker.GetOrMakeIterator(interpreter, context, o);
            if(itr == null)
            {
                // There should be an exception but we'll see if we need to throw one ourselves.
                if(context.CurrentException == null)
                {
                    context.CurrentException = new TypeError("TypeError: '" + o.GetType().Name + "' is not iterable");
                }
                return null;
            }

            var built_list = new List<object>();
            var next_func = (IPyCallable)itr.__dict__["__next__"];
            var next_args = new object[] { itr };        // Let's not repeatedly make this list.

            try
            {
                while(context.CurrentException == null)
                {
                    var result = await next_func.Call(interpreter, context, next_args);
                    if (context.CurrentException == null)
                    {
                        built_list.Add(result);
                    }
                    else if (context.CurrentException is StopIteration)
                    {
                        break;
                    }
                }
            }
            catch(StopIterationException e)
            {
                // Suppress
            }
            catch(System.Reflection.TargetInvocationException task_e)
            {
                if(task_e.InnerException is StopIterationException)
                {
                    // Also suppress
                }
                else
                {
                    throw task_e;
                }
            }

            // StopIteration is expected
            if (context.CurrentException != null)
            {
                if (context.CurrentException is StopIteration)
                {
                    context.CurrentException = null;
                }
                else
                {
                    return null;            // Something failed.
                }
            }

            return PyList.Create(built_list);
        }

        public static async Task<PyDict> dict_builtin(IInterpreter interpreter, FrameContext context, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return PyDict.Create();
            }
            else
            {
                context.CurrentException = new TypeError("TypeError: dict() cannot take any arguments yet");
                return null;
            }
        }

        public static async Task<PyObject> reversed_builtin(IInterpreter interpreter, FrameContext context, object o)
        {
            // 1. Check if there's a __reversed__ dunder. If so, call and return that.
            // 2. Failing that, construct a custom iterator if it has __len__ and __getitem__. Use that to iterate backwards.
            // 3. Failing that, check it's a .NET type that can get a similar treatment. Can we use LINQ magic?
            // 4. Failing that, panic. `TypeError: 'int' object is not reversible`
            var asPyObject = o as PyObject;
            if(asPyObject != null)
            {
                if(asPyObject.__dict__.ContainsKey("__reversed__"))
                {
                    var reversed_dunder = (IPyCallable)asPyObject.__dict__["__reversed__"];
                    var result = await reversed_dunder.Call(interpreter, context, new object[] { asPyObject });
                    return (PyObject)result;
                }
                else if (asPyObject.__dict__.ContainsKey("__len__") && asPyObject.__dict__.ContainsKey("__getitem__"))
                {
                    var len_dunder = (IPyCallable)asPyObject.__dict__["__len__"];
                    var getitem_dunder = (IPyCallable)asPyObject.__dict__["__getitem__"];
                    return IteratorMaker.MakeIterator(new ReversedLenGetItemIterator(asPyObject, len_dunder, getitem_dunder));
                }
                else
                {
                    throw new Exception("TypeError: '" + asPyObject.__class__.Name + "' object is not reversible");
                }
            }
            else
            {
                throw new NotImplementedException("reversed() cannot yet work with non-PyObject types (like .NET types)");
            }
        }

        private static async Task<object> __helper_find_best(IInterpreter interpreter, FrameContext context, object[] inlineArgs, string func_name, string func_dunder)
        {
            if(inlineArgs.Length == 0)
            {
                context.CurrentException = new ValueError("ValueError:" + func_name + "() arg is an empty sequence");
                return null;
            }

            object best = inlineArgs[0];
            if (!(best is PyObject))
            {
                context.CurrentException = new NotImplemented("We cannot compare non-PyObjects in" + func_name + "() yet: " + best.ToString());
                return null;
            }

            for(int i = 0; i < inlineArgs.Length && context.CurrentException == null; ++i)
            {
                object next = inlineArgs[i];
                if (!(next is PyObject))
                {
                    context.CurrentException = new NotImplemented("We cannot compare non-PyObjects in" + func_name + "() yet: " + next.ToString());
                    return null;
                }

                var nextPyObj = next as PyObject;
                var lowestPyObj = best as PyObject;
                if (!lowestPyObj.__dict__.ContainsKey(func_dunder))
                {
                    context.CurrentException = new NotImplemented("We cannot compare PyObjects that do not implement " + func_dunder + "(): " + best.ToString());
                    return null;
                }
                else
                {
                    var ltFunc = (IPyCallable)lowestPyObj.__dict__[func_dunder];
                    var isLower = (PyBool)await ltFunc.Call(interpreter, context, new object[] { lowestPyObj, nextPyObj });
                    if (isLower.InternalValue == false)
                    {
                        best = nextPyObj;
                    }
                }
            }

            return best;
        }

        private static async Task<object> __helper_find_best(IInterpreter interpreter, FrameContext context, PyObject obj, string func_name, string func_dunder)
        {
            // Try to get an iterator off of this thing.
            if (!obj.__dict__.ContainsKey("__iter__"))
            {
                context.CurrentException = new TypeError("TypeError: '" + obj.__class__.Name + "' object is not iterable");
                return null;
            }

            var iterator_func = (IPyCallable)obj.__dict__["__iter__"];
            var iterator = (await iterator_func.Call(interpreter, context, new object[] { obj })) as PyIterable;
            if (iterator == null)
            {
                context.CurrentException = new ValueError("ValueError:" + func_name + "() arg does not implement a PyIterable for __iter__");
                return null;
            }

            object best = await iterator.Next(interpreter, context, obj);
            if (context.CurrentException is StopIteration)
            {
                context.CurrentException = new ValueError("ValueError:" + func_name + "() arg is an empty sequence");
                return null;
            }
            else if (!(best is PyObject))
            {
                context.CurrentException = new NotImplemented("We cannot compare non-PyObjects in" + func_name + "() yet: " + best.ToString());
                return null;
            }

            object next = await iterator.Next(interpreter, context, obj);
            while (context.CurrentException == null)
            {
                if (!(next is PyObject))
                {
                    context.CurrentException = new NotImplemented("We cannot compare non-PyObjects in" + func_name + "() yet: " + next.ToString());
                    return null;
                }

                var nextPyObj = next as PyObject;
                var lowestPyObj = best as PyObject;
                if (!lowestPyObj.__dict__.ContainsKey(func_dunder))
                {
                    context.CurrentException = new NotImplemented("We cannot compare PyObjects that do not implement " + func_dunder + "(): " + best.ToString());
                    return null;
                }
                else
                {
                    var ltFunc = (IPyCallable)lowestPyObj.__dict__[func_dunder];
                    var isLower = (PyBool)await ltFunc.Call(interpreter, context, new object[] { lowestPyObj, nextPyObj });
                    if (isLower.InternalValue == false)
                    {
                        best = nextPyObj;
                    }
                }

                next = await iterator.Next(interpreter, context, obj);
            }

            if (context.CurrentException is StopIteration)
            {
                context.CurrentException = null;
            }

            return best;
        }

        // Help pick which max/max to actually use based on the arguments we get.
        private static async Task<object> minmax_dispatch(IInterpreter interpreter, FrameContext context, string func_name, string func_dunder, params object[] args)
        {
            if (args.Length == 1)
            {
                var asPyObject = args[0] as PyObject;
                if (asPyObject == null)
                {
                    context.CurrentException = new ValueError
                        ("Attempted to call " +
                            func_name +
                            " with a single non-PyObject. We do not support these yet (like .NET lists/arrays). Received " +
                            args[0].GetType().Name);
                    return null;
                }
                else
                {
                    return await __helper_find_best(interpreter, context, asPyObject, func_name, func_dunder);
                }
            }
            else
            {
                return await __helper_find_best(interpreter, context, args, func_name, func_dunder);
            }
        }

        public static async Task<object> min_builtin(IInterpreter interpreter, FrameContext context, params object[] args)
        {
            return await minmax_dispatch(interpreter, context, "min", "__lt__", args);
        }

        public static async Task<object> max_builtin(IInterpreter interpreter, FrameContext context, params object[] args)
        {
            return await minmax_dispatch(interpreter, context, "max", "__gt__", args);
        }

        public static async Task<PyObject> zip_builtin(IInterpreter interpreter, FrameContext context, params object[] iterables)
        {
            var converted_iters = new PyObject[iterables.Length];
            for(int i = 0; i < iterables.Length; ++i)
            {
                var iterable = iterables[i];
                var asPyObject = iterable as PyObject;
                if (asPyObject != null)
                {
                    if (asPyObject.__dict__.ContainsKey("__iter__"))
                    {
                        var iter_dunder = (IPyCallable)asPyObject.__dict__["__iter__"];
                        var result = await iter_dunder.Call(interpreter, context, new object[] { asPyObject });
                        converted_iters[i] = (PyObject) result;
                    }
                    else if (asPyObject.__dict__.ContainsKey("__len__") && asPyObject.__dict__.ContainsKey("__getitem__"))
                    {
                        var len_dunder = (IPyCallable)asPyObject.__dict__["__len__"];
                        var getitem_dunder = (IPyCallable)asPyObject.__dict__["__getitem__"];
                        converted_iters[i] = IteratorMaker.MakeIterator(new LenGetItemIterator(asPyObject, len_dunder, getitem_dunder));
                    }
                    else
                    {
                        throw new Exception("TypeError: '" + asPyObject.__class__.Name + "' object is not iterable");
                    }
                }
                else
                {
                    throw new NotImplementedException("zip() cannot yet work with non-PyObject types (like .NET types)");
                }
            }
            return IteratorMaker.MakeIterator(new ZippedItemIterator(converted_iters));
        }

        public static PySlice slice_builtin(IInterpreter interpreter, FrameContext context, params object[] args)
        {
            if(args.Length == 0)
            {
                context.CurrentException = new TypeError("slice expected at least 1 argument, got 0");
                return null;
            }
            else if(args.Length == 1)
            {
                return PySlice.Create(args[0]);
            }
            else if(args.Length == 2)
            {
                return PySlice.Create(args[0], args[1]);
            }
            else if(args.Length == 3)
            {
                return PySlice.Create(args[0], args[1], args[2]);
            }
            else
            {
                context.CurrentException = new TypeError("slice expected at most 3 arguments, got " + args.Length);
                return null;
            }
        }
    }
}
