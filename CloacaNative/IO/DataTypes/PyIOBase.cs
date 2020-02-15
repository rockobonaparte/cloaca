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
    public class PyIOBaseClass : PyClass
    {
        protected PyIOBaseClass(CodeObject __init__)
            : base("IOBase", __init__, new PyClass[0])
        {
            __instance = this;
        }

        private static PyIOBaseClass __instance;
        public static PyIOBaseClass Instance => __instance ?? (__instance = new PyIOBaseClass(null));

        [ClassMember]
        public static PyInteger fileno(PyIOBase self)
        {
            return new PyInteger(new BigInteger(self.ResourceHandle.Descriptor));
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