using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaNative.IO.DataTypes
{
    public class PyTextIOBaseClass : PyClass
    {
        protected PyTextIOBaseClass(CodeObject __init__)
            : base("TextIOBase", __init__, new[] { PyIOBaseClass.Instance })
        {
            __instance = this;
        }

        private static PyTextIOBaseClass __instance;
        public static PyTextIOBaseClass Instance => __instance ?? (__instance = new PyTextIOBaseClass(null));

    }

    public abstract class PyTextIOBase : PyIOBase
    {
        public PyTextIOBase()
        {

        }

        public PyTextIOBase(PyTypeObject fromType, Handle resourceHandle, Stream nativeStream) 
            : base(fromType, resourceHandle, nativeStream)
        {
        }
    }
}