using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

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

            var self = context.Locals[0] as PyObject;
            if (self == null)
            {
                throw new InvalidCastException("getSuperClass could not convert first local (assumed to be self) to PyObject. Element is: " + context.Locals[0]);
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
            var internalList = new List<PyObject>();
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

        public static PyString str_builtin(object o)
        {
            if (PyNetConverter.CanConvert(o.GetType(), typeof(PyString)))
            {
                return (PyString)PyNetConverter.Convert(o, typeof(PyString));
            }
            else
            {
                throw new InvalidCastException("Cannot convert " + o.GetType() + " to a string.");
            }
        }

        public static PyRange range_builtin(int min, int max, int step)
        {
            return PyRange.Create(min, max, step);
        }
    }
}
