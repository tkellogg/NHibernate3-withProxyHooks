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
        private static List<Func<System.Type, IEnumerable<System.Type>>> s_mixins = new List<Func<System.Type, IEnumerable<System.Type>>>();
        private System.Type[] allMixins = null;

        public static void AddInterceptors(Func<System.Type, IEnumerable<DP.IInterceptor>> interceptors)
        {
            s_interceptors.Add(interceptors);
        }

        public static void AddMixins(Func<System.Type, IEnumerable<System.Type>> mixins)
        {
            s_mixins.Add(mixins);
        }

        private IEnumerable<T> GetExtras<T>(IEnumerable<Func<System.Type, IEnumerable<T>>> creators, IEnumerable<T> plusThese)
        {
            /// Note on the plusThese argument: below NH expects their interceptor to be first at one point...not sure if this
            /// actually matters so here is a mechanism to keep it modified as little as possible
            if (plusThese != null)
                foreach (var item in plusThese)
                    yield return item;
            foreach (var item in creators)
                foreach (var i in item(PersistentClass))
                    yield return i;
        }

        private void LazyInitAllExtras(LazyInitializer initializer)
        {
            if (allInterceptors == null)
            {
                var interc = new List<DP.IInterceptor>(GetExtras(s_interceptors, new DP.IInterceptor[] { initializer }));
                allInterceptors = interc.ToArray();
            }
            if (allMixins == null)
            {
                var mix = new List<System.Type>(GetExtras(s_mixins, Interfaces));
                allMixins = mix.ToArray();
            }
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

                LazyInitAllExtras(initializer);
				object generatedProxy = IsClassProxy
				                        	? ProxyGenerator.CreateClassProxy(PersistentClass, Interfaces, initializer)
				                        	: ProxyGenerator.CreateInterfaceProxyWithoutTarget(Interfaces[0], Interfaces,
				                        	                                                    initializer);

				initializer._constructed = true;
				return (INHibernateProxy) generatedProxy;
			}
			catch (Exception e)
			{
				log.Error("Creating a proxy instance failed", e);
				throw new HibernateException("Creating a proxy instance failed", e);
			}
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