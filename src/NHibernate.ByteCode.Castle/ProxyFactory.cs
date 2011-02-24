using System;
using Castle.DynamicProxy;
using DP = Castle.DynamicProxy;
using NHibernate.Engine;
using NHibernate.Proxy;
using System.Collections.Generic;

namespace NHibernate.ByteCode.Castle
{
	public class ProxyFactory : AbstractProxyFactory
	{
		protected static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof (ProxyFactory));
		private static readonly ProxyGenerator ProxyGenerator = new ProxyGenerator();

		protected static ProxyGenerator DefaultProxyGenerator
		{
			get { return ProxyGenerator; }
		}

        private static List<Func<System.Type, IEnumerable<DP.IInterceptor>>> s_interceptors = new List<Func<System.Type, IEnumerable<DP.IInterceptor>>>();
        private DP.IInterceptor[] allInterceptors = null;
        private static List<Func<System.Type, IEnumerable<object>>> s_mixins = new List<Func<System.Type, IEnumerable<object>>>();
        private static List<Func<System.Type, IEnumerable<System.Type>>> s_interfaces = new List<Func<System.Type, IEnumerable<System.Type>>>();

        public static void AddInterceptors(Func<System.Type, IEnumerable<DP.IInterceptor>> interceptors)
        {
            s_interceptors.Add(interceptors);
        }

        public static void AddAdditionalInterfaces(Func<System.Type, IEnumerable<System.Type>> interfaces)
        {
            s_interfaces.Add(interfaces);
        }

        public static void AddMixins(Func<System.Type, IEnumerable<object>> mixins)
        {
            s_mixins.Add(mixins);
        }

        private IEnumerable<T> GetExtras<T>(IEnumerable<Func<System.Type, IEnumerable<T>>> creators)
        {
            foreach (var item in creators)
                foreach (var i in item(PersistentClass))
                    yield return i;
        }

        private ProxyGenerationOptions LazyInitAllExtras(List<DP.IInterceptor> interceptors, List<System.Type> interfaces)
        {
            interceptors.AddRange(GetExtras(s_interceptors));
            interfaces.AddRange(GetExtras(s_interfaces));
            var opts = new ProxyGenerationOptions();
            
            foreach (var mixin in GetExtras(s_mixins))
                opts.AddMixinInstance(mixin);
            return opts;
        }

		/// <summary>
		/// Build a proxy using the Castle.DynamicProxy library.
		/// </summary>
		/// <param name="id">The value for the Id.</param>
		/// <param name="session">The Session the proxy is in.</param>
		/// <returns>A fully built <c>INHibernateProxy</c>.</returns>
		public override INHibernateProxy GetProxy(object id, ISessionImplementor session)
		{
			try
			{
				var initializer = new LazyInitializer(EntityName, PersistentClass, id, GetIdentifierMethod,
				                                            SetIdentifierMethod, ComponentIdType, session);
                var interceptors = new List<DP.IInterceptor>(new DP.IInterceptor[] { initializer });
                var interfaces = new List<System.Type>();
                var opts = LazyInitAllExtras(interceptors, interfaces);
                opts.Hook = new MixinProxyGenerationHook(this);
                interfaces.AddRange(Interfaces);
                var interceptorArray = interceptors.ToArray();
				object generatedProxy;
                if (IsClassProxy) {
                    opts.AddMixinInstance(new NHibernateProxyMixin() { HibernateLazyInitializer = initializer });
                    generatedProxy = ProxyGenerator.CreateClassProxy(PersistentClass, opts, interceptorArray);
                }
                else generatedProxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget(Interfaces[0], Interfaces, opts, interceptorArray);

                var changes = ((INHibernateProxy)generatedProxy).HibernateLazyInitializer;
				initializer._constructed = true;
				return (INHibernateProxy) generatedProxy;
			}
			catch (Exception e)
			{
				log.Error("Creating a proxy instance failed", e);
				throw new HibernateException("Creating a proxy instance failed", e);
			}
		}

        public class MixinProxyGenerationHook : IProxyGenerationHook
        {
            private ProxyFactory factory;
            public MixinProxyGenerationHook(ProxyFactory factory)
            {
                this.factory = factory;
            }

            #region IProxyGenerationHook Members

            public void MethodsInspected(){}

            public void NonProxyableMemberNotification(System.Type type, System.Reflection.MemberInfo memberInfo){}

            public bool ShouldInterceptMethod(System.Type type, System.Reflection.MethodInfo methodInfo)
            {
                foreach (var obj in factory.GetExtras(ProxyFactory.s_mixins))
                {
                    foreach (var @interface in obj.GetType().GetInterfaces())
                    {
                        var @params = new System.Type[methodInfo.GetParameters().Length];
                        for(int i=0; i<@params.Length; i++)
                            @params[i] = methodInfo.GetParameters()[i].ParameterType;
                        var meth = @interface.GetMethod(methodInfo.Name, @params);
                        if (meth != null)
                            return false;
                    }
                }
                return true;
            }

            #endregion
        }

        public class NHibernateProxyMixin : INHibernateProxy
        {
            public ILazyInitializer HibernateLazyInitializer { get; set; }
            
        }

		public override object GetFieldInterceptionProxy()
		{
			var proxyGenerationOptions = new ProxyGenerationOptions();
			var interceptor = new LazyFieldInterceptor();
			proxyGenerationOptions.AddMixinInstance(interceptor);
			return ProxyGenerator.CreateClassProxy(PersistentClass, proxyGenerationOptions, interceptor);
		}
	}
}