using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloacaInterpreter;
using CloacaNative;
using CloacaNative.IO.DataTypes;
using LanguageImplementation;

namespace CloacaNative
{
  public interface INativeFileProvider : INativeResourceProvider
  {
      Task<PyIOBase> Open(
          IInterpreter interpreter, FrameContext context,
          Handle handle, string path, string fileMode);
  }
}
