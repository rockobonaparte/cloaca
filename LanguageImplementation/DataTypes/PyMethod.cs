using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    /// <summary>
    /// PyMethod comes from CPython and defines a callable bound to an object.
    /// It is aware of its self argument and supplies it as the first argument
    /// to the actual callable.
    /// </summary>
    public class PyMethod : PyObject, IPyCallable
    {
        public PyObject selfHandle;
        private IPyCallable callable;

        public PyMethod(PyObject self, IPyCallable callable)
        {
            this.callable = callable;
            var asSuper = self as PySuper;
            if(asSuper != null)
            {
                selfHandle = (PyObject) asSuper.__getattribute__("__self__");
            }
            else
            {
                selfHandle = self;
            }
            __setattr__("__call__", this);
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            object[] massagedArgs = new object[args.Length + 1];
            massagedArgs[0] = selfHandle;
            Array.Copy(args, 0, massagedArgs, 1, args.Length);

            return callable.Call(interpreter, context, massagedArgs);
        }
    }
}
