using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SubSonic.Core.Proxies
{
    public abstract class ProxyFactory
    {
        private const string PROXY_ASSEMBLY = "SubSonic.Core.ProxyAssembly";
        private const string PROXY_MODULE = "SubSonicCoreTypeGenerator";
        protected const string INVOKE_METHOD = "InvokeMethod";

        protected static ProxyDictionary<string, ProxyBuilder> proxies = new ProxyDictionary<string, ProxyBuilder>();

        private Dictionary<Type, OpCode> ldIndOpCodeTypeMap;
        protected Dictionary<Type, OpCode> LoadOpCodeTypeMap
        {
            get
            {
                if (ldIndOpCodeTypeMap == null)
                {
                    ldIndOpCodeTypeMap = new Dictionary<Type, OpCode>
                    {
                        { typeof(Boolean), OpCodes.Ldind_I1 },
                        { typeof(Byte), OpCodes.Ldind_U1 },
                        { typeof(SByte), OpCodes.Ldind_I1 },
                        { typeof(Int16), OpCodes.Ldind_I2 },
                        { typeof(UInt16), OpCodes.Ldind_U2 },
                        { typeof(Int32), OpCodes.Ldind_I4 },
                        { typeof(UInt32), OpCodes.Ldind_U4 },
                        { typeof(Int64), OpCodes.Ldind_I8 },
                        { typeof(UInt64), OpCodes.Ldind_I8 },
                        { typeof(Char), OpCodes.Ldind_U2 },
                        { typeof(Double), OpCodes.Ldind_R8 },
                        { typeof(Single), OpCodes.Ldind_R4 }
                    };
                }
                return ldIndOpCodeTypeMap;
            }
        }

        private Dictionary<Type, OpCode> stindOpCodeTypeMap;

        protected Dictionary<Type, OpCode> StoreOpCodeTypeMap
        {
            get
            {
                if (stindOpCodeTypeMap == null)
                {
                    stindOpCodeTypeMap = new Dictionary<Type, OpCode>
                    {
                        { typeof(Boolean), OpCodes.Stind_I1 },
                        { typeof(Byte), OpCodes.Stind_I1 },
                        { typeof(SByte), OpCodes.Stind_I1 },
                        { typeof(Int16), OpCodes.Stind_I2 },
                        { typeof(UInt16), OpCodes.Stind_I2 },
                        { typeof(Int32), OpCodes.Stind_I4 },
                        { typeof(UInt32), OpCodes.Stind_I4 },
                        { typeof(Int64), OpCodes.Stind_I8 },
                        { typeof(UInt64), OpCodes.Stind_I8 },
                        { typeof(Char), OpCodes.Stind_I2 },
                        { typeof(Double), OpCodes.Stind_R8 },
                        { typeof(Single), OpCodes.Stind_R4 }
                    };
                }
                return stindOpCodeTypeMap;
            }
        }

        protected virtual TypeAttributes GetTypeAttributes()
        {
            return TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
        }

        protected virtual IEnumerable<Type> GetCTorArgumentTypes(Type ctorArgType = null)
        {
            return new Type[] { ctorArgType };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeOfProxy">type to be extended</param>
        /// <returns></returns>
        protected ProxyBuilder CreateProxyBuilder(string proxyName, Type baseTypeOfProxy, Type interfaceType, Type ctorArgType = null)
        {
            if (proxyName.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(proxyName));
            }

            if (baseTypeOfProxy == null)
            {
                throw new ArgumentNullException(nameof(baseTypeOfProxy));
            }

            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            AssemblyName
                executingName = Assembly.GetExecutingAssembly().GetName(),
                proxyAssemblyName = new AssemblyName(PROXY_ASSEMBLY)
                {
                    KeyPair = executingName.KeyPair
                };
            // proxy assembly takes on the same strong name as the assembly that is executing
            proxyAssemblyName.SetPublicKey(executingName.GetPublicKey());

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                proxyAssemblyName,
                AssemblyBuilderAccess.Run);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(PROXY_MODULE);

            TypeBuilder typeBuilder = moduleBuilder.DefineType($"{PROXY_ASSEMBLY}.{proxyName}", GetTypeAttributes(), baseTypeOfProxy);

            List<Type> interfaces = new List<Type>();

            foreach (Type @interface in interfaceType.GetInterfaces())
            {
                if (!interfaceType.IsInterface)
                {
                    typeBuilder.AddInterfaceImplementation(@interface);
                }

                interfaces.Add(@interface);
            }

            if (interfaceType.IsInterface)
            {
                typeBuilder.AddInterfaceImplementation(interfaceType);
            }

            CreateConstructor(baseTypeOfProxy, typeBuilder, ctorArgType != null ? GetCTorArgumentTypes(ctorArgType).ToArray() : null);

            BuildProperties(baseTypeOfProxy, interfaces, typeBuilder);

            BuildMethods(baseTypeOfProxy, interfaces, typeBuilder);

            return new ProxyBuilder()
            {
                ProxyName = proxyName,
                InterfaceType = interfaceType,
                CtorType = ctorArgType,
                AssemblyBuilder = assemblyBuilder,
                ModuleBuilder = moduleBuilder,
                TypeBuilder = typeBuilder
            };
        }

        protected virtual void BuildProperties(Type baseTypeOfProxy, IEnumerable<Type> interfaces, TypeBuilder typeBuilder)
        {
            foreach (PropertyInfo property in GetAllProperties(interfaces))
            {
                BuildProperty(property, typeBuilder);
            }
        }
        protected abstract void BuildProperty(PropertyInfo propertyInfo, TypeBuilder typeBuilder);

        protected virtual void BuildMethods(Type baseTypeOfProxy, IEnumerable<Type> interfaces, TypeBuilder typeBuilder)
        {
            foreach (MethodInfo methodInfo in GetAllMethods(interfaces))
            {
                BuildMethod(methodInfo, typeBuilder);
            }
        }
        protected abstract void BuildMethod(MethodInfo methodInfo, TypeBuilder typeBuilder);

        protected virtual MethodBuilder ConstructMethod(Type baseTypeOfProxy, MethodInfo methodInfo, TypeBuilder typeBuilder)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();
            int parameterCount = parameters.Length;
            Type[] parameterTypes = new Type[parameterCount];
            for(int i = 0; i < parameterCount; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }
            Type returnType = methodInfo.ReturnType;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                returnType,
                parameterTypes);

            ILGenerator mIL = methodBuilder.GetILGenerator();
            GenerateILCodeForMethod(baseTypeOfProxy, methodInfo, mIL, parameterTypes, returnType);

            return methodBuilder;
        }

        protected abstract void GenerateILCodeForMethod(Type baseTypeOfProxy, MethodInfo methodInfo, ILGenerator mIL, Type[] inputArgTypes, Type returnType);

        protected void CreateConstructor(Type baseTypeOfProxy, TypeBuilder typeBuilder, Type[] ctorArgTypes)
        {
            ConstructorBuilder ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, ctorArgTypes);

            ILGenerator ctorIL = ctor.GetILGenerator();

            BuildILForCallToBaseCtor(ctorIL, baseTypeOfProxy.GetConstructor(ctorArgTypes));
            FinalizeILForCtor(ctorIL);            
        }

        /// <summary>
        /// Call this first when generating the contructor with the extended type constructor info.
        /// </summary>
        /// <param name="ctorIL"><see cref="ILGenerator"/> for constructor builder</param>
        /// <param name="baseCtor"><see cref="ConstructorInfo"/> for base type</param>
        /// <returns></returns>
        protected virtual void BuildILForCallToBaseCtor(ILGenerator ctorIL, ConstructorInfo baseCtor)
        {
            if (baseCtor != null)
            {
                Type opCodeType = typeof(OpCodes);

                ctorIL.Emit(OpCodes.Ldarg_0);
                for (int i = 0, n = baseCtor.GetParameters().Length; i < n; i++)
                {
                    if (opCodeType.GetField($"Ldarg_{i + 1}").GetValue(null) is OpCode opCode)
                    {
                        ctorIL.Emit(opCode);
                    }
                }
                ctorIL.Emit(OpCodes.Call, baseCtor);
            }
        }
        /// <summary>
        /// Call this 2nd when finishing up the IL generation for the CTor
        /// </summary>
        /// <param name="ctorIL"></param>
        protected virtual void FinalizeILForCtor(ILGenerator ctorIL)
        {
            ctorIL.Emit(OpCodes.Ret);
        }

        private static List<MethodInfo> GetAllMethods(IEnumerable<Type> interfaces)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (Type interfaceType in interfaces)
            {
                methods.AddRange(interfaceType.GetMethods());
            }
            return methods;
        }

        private static List<PropertyInfo> GetAllProperties(IEnumerable<Type> interfaces)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();
            foreach (Type interfaceType in interfaces)
            {
                properties.AddRange(interfaceType.GetProperties());
            }
            return properties;
        }
    }
}
