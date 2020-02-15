using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaNative.IO.DataTypes
{
    public abstract class PyTextIOBaseClass : PyIOBaseClass
    {
        protected PyTextIOBaseClass(string name, CodeObject __init__, PyClass[] bases)
            : base(name, __init__, bases)
        {
        }

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