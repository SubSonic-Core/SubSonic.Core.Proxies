using SubSonic.Core.Proxies;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SubSonic.Core.Remoting
{
    public class RemotingProxyFactory
        : ProxyFactory
    {

        protected override void BuildMethod(MethodInfo methodInfo, TypeBuilder typeBuilder)
        {
            throw new NotImplementedException();
        }

        protected override void BuildProperty(PropertyInfo propertyInfo, TypeBuilder typeBuilder)
        {
            throw new NotImplementedException();
        }

        protected override void GenerateILCodeForMethod(Type baseTypeOfProxy, MethodInfo methodInfo, ILGenerator mIL, Type[] inputArgTypes, Type returnType)
        {
            throw new NotImplementedException();
        }
    }
}
