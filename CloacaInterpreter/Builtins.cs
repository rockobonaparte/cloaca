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
        /// <returns>This class' superclass. Exceptions will be raised if this cannot be deduced.</returns>
        public static PyClass super(FrameContext context)
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
            return self.__class__.__bases__[0];
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
                internalList.Add(new PyString(name));
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
    }
}
