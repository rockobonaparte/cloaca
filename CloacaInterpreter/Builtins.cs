using System;

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
    }
}
