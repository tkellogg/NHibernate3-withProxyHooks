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

        public static void AddInterceptors(Func<System.Type, IEnumerable<DP.IInterceptor>> interceptors)
        {
            s_interceptors.Add(interceptors);
        }

        public static void AddMixins(Func<System.Type, IEnumerable<object>> mixins)
        {
            s_mixins.Add(mixins);
        }

        private static IEnumerable<T> GetExtras<T>(IEnumerable<Func<System.Type, IEnumerable<T>>> creators, System.Type onType)
        {
            foreach (var item in creators)
                foreach (var i in item(onType))
                    yield return i;
        }

        private IEnumerable<T> GetExtras<T>(IEnumerable<Func<System.Type, IEnumerable<T>>> creators)
        {
            return GetExtras(creators, PersistentClass);
        }

        private ProxyGenerationOptions LazyInitAllExtras(List<DP.IInterceptor> interceptors)
        {
            interceptors.AddRange(GetExtras(s_interceptors));
            var opts = new ProxyGenerationOptions();
            
            foreach (var mixin in GetExtras(s_mixins))
                opts.AddMixinInstance(mixin);
            opts.Hook = new MixinProxyGenerationHook();
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
                var opts = LazyInitAllExtras(interceptors);
                var interceptorArray = interceptors.ToArray();
				object generatedProxy;
                if (IsClassProxy) {
                    opts.AddMixinInstance(new NHibernateProxyMixin() { HibernateLazyInitializer = initializer });
                    generatedProxy = ProxyGenerator.CreateClassProxy(PersistentClass, opts, interceptorArray);
                }
                else generatedProxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget(Interfaces[0], Interfaces, opts, interceptorArray);

				initializer._constructed = true;
				return (INHibernateProxy) generatedProxy;
			}
			catch (Exception e)
			{
				log.Error("Creating a proxy instance failed", e);
				throw new HibernateException("Creating a proxy instance failed", e);
			}
		}

        private class MixinProxyGenerationHook : IProxyGenerationHook
        {
            #region IProxyGenerationHook Members

            public void MethodsInspected(){}

            public void NonProxyableMemberNotification(System.Type type, System.Reflection.MemberInfo memberInfo){}

            private HashSet<System.Reflection.MethodInfo> nonProxiedMethods;

            public bool ShouldInterceptMethod(System.Type type, System.Reflection.MethodInfo methodInfo)
            {
                if (nonProxiedMethods == null)
                {
                    nonProxiedMethods = new HashSet<System.Reflection.MethodInfo>();
                    foreach (var obj in ProxyFactory.GetExtras(ProxyFactory.s_mixins, type))
                        foreach (var @interface in obj.GetType().GetInterfaces())
                            foreach (var method in @interface.GetMethods())
                                nonProxiedMethods.Add(method);
                }
                return !nonProxiedMethods.Contains(methodInfo);
            }

            #endregion
        }

        /// <summary>
        /// This is our own static proxy to make sure the dynamic proxy still has a reference to an
        /// ILazyInitializer and implements INhibernateProxy. We have to use this with the DP as a 
        /// mixin. NH used to just include INHibernateProxy as an additional interface that needed
        /// to be implemented. I think I like this better...
        /// </summary>
        public class NHibernateProxyMixin : INHibernateProxy
        {
            public ILazyInitializer HibernateLazyInitializer { get; set; }
        }

		public override object GetFieldInterceptionProxy()
		{
			var proxyGenerationOptions = new ProxyGenerationOptions();
            var interceptors = new List<DP.IInterceptor>();
			interceptors.Add(new LazyFieldInterceptor());
            proxyGenerationOptions.AddMixinInstance(interceptors[0]);
            // Add all extra interceptors
            interceptors.AddRange(GetExtras(s_interceptors));
            // Add all extra mixins
            foreach (var mixin in GetExtras(s_mixins))
                proxyGenerationOptions.AddMixinInstance(mixin);
            // important line to make sure NH doesn't intercept our mixins...
            proxyGenerationOptions.Hook = new MixinProxyGenerationHook();
			return ProxyGenerator.CreateClassProxy(PersistentClass, proxyGenerationOptions, interceptors.ToArray());
		}
	}
}