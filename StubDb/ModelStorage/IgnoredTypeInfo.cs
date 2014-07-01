using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StubDb.ModelStorage
{
    public class IgnoredTypeInfo
    {
        public Type Type { get; set; }
        public bool Persist { get; set; }

        public IgnoredTypeInfo(Type type, bool persist)
        {
            Type = type;
            Persist = persist;
        }
    }
}
