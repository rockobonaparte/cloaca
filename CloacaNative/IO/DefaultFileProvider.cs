using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloacaNative;
using CloacaNative.IO.DataTypes;
using CloacaInterpreter;
using LanguageImplementation;

namespace CloacaNative.IO
{
    public class DefaultFileProvider : INativeFileProvider
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task<PyIOBase> Open(
            IInterpreter interpreter, FrameContext context,
            Handle handle, string path, string fileMode)
        {
            FileStream nativeStream = File.OpenRead(path);
            PyTextIOWrapper result =
                (PyTextIOWrapper) await PyTextIOWrapperClass.Instance.Call(interpreter, context, new object[0]);
            return result;
        }

        public void RegisterBuiltins(Interpreter interpreter)
        {
            throw new NotImplementedException();
        }
    }
}