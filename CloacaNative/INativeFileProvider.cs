using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloacaNative;
using CloacaNative.IO.DataTypes;

namespace CloacaNative
{
  public interface INativeFileProvider : INativeResourceProvider
  {
    PyIOBase Open(Handle handle, string fileName, string fileMode);
  }
}
