using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloacaNative
{
  public class Handle
  {
    public int Descriptor { get; private set; }
    public NativeResourceManager ResourceManager { get; private set; }

    public Handle(NativeResourceManager resourceManager, int descriptor)
    {
      ResourceManager = resourceManager;
      Descriptor = descriptor;
    }
  }
}
