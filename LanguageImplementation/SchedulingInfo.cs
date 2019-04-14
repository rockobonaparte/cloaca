using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageImplementation
{
    public class SchedulingInfo
    {

    }

    public class YieldOnePass : SchedulingInfo
    {

    }

    public class ReturnValue : SchedulingInfo
    {
        public object Returned;

        public ReturnValue(object returned)
        {
            Returned = returned;
        }
    }
}
