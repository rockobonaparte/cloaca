using LanguageImplementation;
using LanguageImplementation.DataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloacaInterpreter
{
    // TODO:
    // Read:
    // Dictionary
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

        private static object LoadSubscript(IInterpreter interpreter, FrameContext context, IList container, PyObject index)
        {
            int intIndex = GetIntIndex(index);
            return container[intIndex];
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

        private static async Task<object> LoadSubscript(Interpreter interpreter, FrameContext context, Array asArray, int arrayIndex)
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
                    return LoadSubscript(interpreter, context, asArray, arrayIndex);
                }
                else if(container is IList)
                {
                    return LoadSubscript(interpreter, context, container as IList, index);
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

                return LoadSubscript(interpreter, context, containerPyObject, indexPyObject);
            }
        }
    }
}
