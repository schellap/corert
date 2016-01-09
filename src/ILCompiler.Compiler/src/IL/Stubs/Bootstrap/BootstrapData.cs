using Internal.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Internal.IL.Stubs.Bootstrap
{
    public class BootstrapData
    {
        public MethodDesc MainMethod;
        public object StringFixupStart;
        public object StringFixupEnd;
        public object StringEEType;
        public DefType OwningType;
    }
}
