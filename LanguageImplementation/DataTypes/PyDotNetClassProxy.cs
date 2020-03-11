using System;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    /// <summary>
    /// A class proxy for .NET classes. 
    /// </summary>
    public class PyDotNetClassProxy : PyTypeObject
    {
        private Type dotNetType;
        public const string __dotnettype__ = "__dotnettype__";      // Will contain dotNetType. Cannot be changed.

        public PyDotNetClassProxy(Type dotNetType) :
            base(dotNetType.Name, new WrappedCodeObject(dotNetType.GetConstructors()))
        {
            this.dotNetType = dotNetType;
        }

        [ClassMember]
        public static PyString __repr__(PyObject self)
        {
            var asProxy = (PyDotNetClassProxy)self;
            return PyString.Create("<" + asProxy.dotNetType.Name + " proxy object at " + self.GetHashCode() + ">");
        }

        [ClassMember]
        public static PyString __str__(PyObject self)
        {
            // Default for __str__ is same as __repr__
            return __repr__(self);
        }

        /// <summary>
        /// Alternative constructor for this wrapped .NET type. This replaces the __call__ in PyTypeObject
        /// with one that will construct and return an instance of this wrapped .NET type.
        /// </summary>
        /// <param name="interpreter">The interpreter instance that has invoked this code.</param>
        /// <param name="context">The call stack and state at the time this code was invoked.</param>
        /// <param name="args">Arguments given to this class's call.</param>
        /// <returns>An instance of the .NET object.</returns>
        public override async Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            // TODO: Should I await here? Does this block?
            return await this.__init__.Call(interpreter, context, args);
        }
    }
}
