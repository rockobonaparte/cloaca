using LanguageImplementation;
using LanguageImplementation.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloacaInterpreter
{
    // TODO:
    //
    // Write:
    // PyDict/PyList
    // IList
    // Array
    // Dictionary
    //
    // Stretch:
    // subscriptables.
    public class SubscriptHelper
    {
        private static int GetIntIndex(object index)
        {
            var indexPyObject = index as PyObject;
            if (indexPyObject != null)
            {
                var asPyInt = index as PyInteger;
                if (asPyInt == null)
                {
                    throw new Exception("TypeError: Attempted to use non - PyInteger '" + index.GetType().Name + "' as a subscript key.");
                }
                else
                {
                    var arrayIndex = (int)asPyInt.number;
                    return arrayIndex;
                }
            }
            else
            {
                // Not a PyObject, so hopefully it's a .NET type?
                try
                {
                    var arrayIndex = (int)index;
                    return arrayIndex;
                }
                catch (InvalidCastException cast_e)
                {
                    throw new Exception("TypeError: Attempted to use '" + index.GetType().Name + "' as a subscript key. Could not cast to int for .NET array.");
                }
            }
        }

        private static object LoadSubscriptIList(IList container, object index)
        {
            int intIndex = GetIntIndex(index);
            return container[intIndex];
        }

        private static object LoadSubscriptIDict(IDictionary container, object index)
        {
            try
            {
                return container[index];
            } 
            catch(KeyNotFoundException)
            {
                throw new Exception("KeyError: " + index);
            }
        }

        private static async Task<object> LoadSubscript(Interpreter interpreter, FrameContext context, PyObject container, PyObject index)
        {
            try
            {
                var getter = container.__getattribute__("__getitem__");
                var functionToRun = getter as IPyCallable;

                if (functionToRun == null)
                {
                    throw new Exception("TypeError: '" + container.GetType().Name + "' object is not subscriptable; could not be converted to IPyCallable");
                }

                var returned = await functionToRun.Call(interpreter, context, new object[] { index });
                if (returned != null)
                {
                    return returned;
                }
                else
                {
                    return NoneType.Instance;
                }
            }
            catch (KeyNotFoundException)
            {
                // TODO: use __class__.__name__
                throw new Exception("TypeError: '" + container.__class__.GetType().Name + "' object is not subscriptable");
            }
        }

        private static object LoadSubscriptArray(Array asArray, int arrayIndex)
        {
            // Still here? Let's try to get the array value!
            if (arrayIndex < 0)
            {
                // Allow negative indexing, which only works for one wrap around the array.
                arrayIndex = asArray.Length - arrayIndex;
            }

            if (arrayIndex < 0 || arrayIndex >= asArray.Length)
            {
                throw new Exception("IndexError: list index out of range");
            }
            return asArray.GetValue(arrayIndex);
        }

        public static async Task<object> LoadSubscript(Interpreter interpreter, FrameContext context, object container, object index)
        {
            var containerPyObject = container as PyObject;
            var indexPyObject = index as PyObject;

            if (containerPyObject == null)
            {
                // TODO: Expand to cover lists and objects with indexing operators; probably also need to worry about dictionaries.
                // Look at https://stackoverflow.com/questions/14462820/check-if-indexing-operator-exists
                if (container.GetType().IsArray)
                {
                    var asArray = container as Array;
                    var arrayIndex = GetIntIndex(index);
                    return LoadSubscriptArray(asArray, arrayIndex);
                }
                else if(container is IList)
                {
                    return LoadSubscriptIList(container as IList, index);
                }
                else if(container is IDictionary)
                {
                    return LoadSubscriptIDict(container as IDictionary, index);
                }
                else
                {
                    throw new Exception("TypeError: '" + container.GetType().Name + "' object is not subscriptable; could not be converted to PyObject nor .NET array");
                }
            }
            else
            {
                if (indexPyObject == null)
                {
                    throw new Exception("Attempted to use non-PyObject '" + index.GetType().Name + "' as a subscript key.");
                }

                return await LoadSubscript(interpreter, context, containerPyObject, indexPyObject);
            }
        }

        public static async Task StoreSubscript(Interpreter interpreter, FrameContext context, object container, object index, object value)
        {
            // TODO: Stop checking between BigInt and friends once data types are all objects
            // TODO: Raw index conversion to int should probably be moved to its more local section
            context.Cursor += 1;
            var idxAsPyObject = index as PyObject;
            if (idxAsPyObject == null)
            {
                // TODO: use __class__.__name__
                // throw new InvalidCastException("TypeError: list indices must be integers or slices, not " + rawIndex.GetType().Name);
                throw new Exception("Attempted to use non-PyObject '" + index.GetType().Name + "' as a subscript key.");
            }

            var containerPyObject = container as PyObject;
            if (containerPyObject == null)
            {
                throw new Exception("TypeError: '" + container.GetType().Name + "' object is not subscriptable; could not be converted to PyObject");
            }

            try
            {
                var setter = containerPyObject.__getattribute__("__setitem__");
                var functionToRun = setter as IPyCallable;

                if (functionToRun == null)
                {
                    throw new Exception("TypeError: '" + containerPyObject.GetType().Name + "' object is not subscriptable; could not be converted to IPyCallable");
                }

                var returned = await functionToRun.Call(interpreter, context, new object[] { idxAsPyObject, value });
                if (returned != null)
                {
                    context.DataStack.Push(returned);
                }
            }
            catch (KeyNotFoundException)
            {
                // TODO: use __class__.__name__
                throw new Exception("TypeError: '" + containerPyObject.__class__.GetType().Name + "' object is not subscriptable");
            }
        }
    }
}
