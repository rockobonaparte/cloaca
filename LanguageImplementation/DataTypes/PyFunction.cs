using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyFunction : PyObject, IPyCallable
    {
        // needs a reference to:
        // 1. CodeObject
        // 2. Globals           (and how does it get this? When would it be created?)
        private IPyCallable callable;

        public PyFunction(IPyCallable callable)
        {
            this.callable = callable;
            __setattr__("__call__", this);
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            return callable.Call(interpreter, context, args);
        }
    }
}
