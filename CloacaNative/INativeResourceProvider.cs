using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloacaInterpreter;

namespace CloacaNative
{
  public interface INativeResourceProvider
  {
      void RegisterBuiltins(Interpreter interpreter);
  }
}
