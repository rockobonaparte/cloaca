using System;
using System.Collections.Generic;

namespace LanguageImplementation.DataTypes
{
    /// <summary>
    /// PyMethod comes from CPython and defines a callable bound to an object.
    /// It is aware of its self argument and supplies it as the first argument
    /// to the actual callable.
    /// </summary>
    public class PyMethod : PyObject, IPyCallable
    {
        PyObject selfHandle;
        public PyMethod(PyObject self, IPyCallable callable)
        {
            selfHandle = self;
            __dict__.Add("__call__", callable);
        }

        public IEnumerable<SchedulingInfo> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            object[] massagedArgs = new object[args.Length + 1];
            massagedArgs[0] = selfHandle;
            Array.Copy(args, 0, massagedArgs, 1, args.Length);

            foreach (var continuation in ((IPyCallable) __dict__["__call__"]).Call(interpreter, context, massagedArgs))
            {
                yield return continuation;
            }
        }
    }
}
