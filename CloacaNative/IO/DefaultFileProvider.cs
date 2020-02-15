using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloacaNative;
using CloacaNative.IO.DataTypes;
using CloacaInterpreter;

namespace CloacaNative.IO
{
  public class DefaultFileProvider : INativeFileProvider
  {
    public void Dispose()
    {
      throw new NotImplementedException();
    }

    public PyIOBase Open(Handle handle, string path, string fileMode)
    {
      FileStream nativeStream = File.OpenRead(path);
      return new PyTextIOWrapper(handle, nativeStream);
    }

    public void RegisterBuiltins(Interpreter interpreter)
    {
        throw new NotImplementedException();
    }
  }
}