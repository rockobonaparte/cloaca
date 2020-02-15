using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaNative.IO.DataTypes
{
    public abstract class PyIOBaseClass : PyClass
    {
        [ClassMember]
        public static PyInteger fileno(PyIOBase self)
        {
            return new PyInteger(new BigInteger(self.ResourceHandle.Descriptor));
        }

        protected PyIOBaseClass(string name, CodeObject __init__, PyClass[] bases)
            : base(name, __init__, bases)
        {
        }
    }

    public abstract class PyIOBase : PyObject
    {
        public Stream NativeStream { get; private set; }
        public Handle ResourceHandle { get; private set; }

        public PyIOBase()
        {

        }

        public PyIOBase(PyTypeObject fromType, Handle resourceHandle, Stream nativeStream)
            : base(fromType)
        {
            ResourceHandle = resourceHandle;
            NativeStream = nativeStream;
        }
    }
}