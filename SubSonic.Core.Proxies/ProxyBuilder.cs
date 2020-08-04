using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace SubSonic.Core.Proxies
{
    internal sealed class ProxyBuilder
    {
        public string ProxyName { get; set; }
        public Type TypeOfProxy { get; set; }
        public Type CtorType { get; set; }
        public AssemblyBuilder AssemblyBuilder { get; set; }
        public ModuleBuilder ModuleBuilder { get; set; }
        public TypeBuilder TypeBuilder { get; set; }
    }
}
