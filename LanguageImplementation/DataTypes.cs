using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageImplementation
{
    public class NoneType
    {
        private static NoneType _instance;
        public static NoneType Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new NoneType();
                }
                return _instance;
            }
        }

        private NoneType()
        {
        }
    }

    public class PyTuple
    {
        public object[] values;
        public PyTuple(List<object> values)
        {
            this.values = values.ToArray();
        }

        public PyTuple(object[] values)
        {
            this.values = values;
        }
    }
}
