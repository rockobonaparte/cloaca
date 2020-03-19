using LanguageImplementation;
using LanguageImplementation.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloacaInterpreter
{
    public class SubscriptHelper
    {
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

        public static async Task<object> LoadSubscript(Interpreter interpreter, FrameContext context, object container, object index)
        {
            var containerPyObject = container as PyObject;
            var indexPyObject = index as PyObject;

            if (containerPyObject == null)
            {
                // TODO: Expand to cover lists and objects with indexing operators; probably also need to worry about dictionaries.
                // Look at https://stackoverflow.com/questions/14462820/check-if-indexing-operator-exists
                if (!container.GetType().IsArray)
                {
                    throw new Exception("TypeError: '" + container.GetType().Name + "' object is not subscriptable; could not be converted to PyObject nor .NET array");
                }
                else
                {
                    var asArray = container as Array;
                    int arrayIndex = -1;
                    if (indexPyObject != null)
                    {
                        var asPyInt = index as PyInteger;
                        if (asPyInt == null)
                        {
                            throw new Exception("TypeError: Attempted to use non - PyInteger '" + index.GetType().Name + "' as a subscript key.");
                        }
                        else
                        {
                            arrayIndex = (int)asPyInt.number;
                        }
                    }
                    else
                    {
                        // Not a PyObject, so hopefully it's a .NET type?
                        try
                        {
                            arrayIndex = (int)index;
                        }
                        catch (InvalidCastException cast_e)
                        {
                            throw new Exception("TypeError: Attempted to use '" + index.GetType().Name + "' as a subscript key. Could not cast to int for .NET array.");
                        }
                    }

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
