using System.Collections;
using NHibernate.Hql.Ast.ANTLR;
using System.Collections.Generic;
using NHibernate.Util;
using NHibernate.Hql.Classic;

namespace NHibernate.Test.HQL.Ast
{
	public class BaseFixture: TestCase
	{
		private readonly IDictionary<string, IFilter> emptyfilters = new CollectionHelper.EmptyMapClass<string, IFilter>();

		protected override bool AppliesTo(Engine.ISessionFactoryImplementor factory)
		{
			return !(sessions.Settings.QueryTranslatorFactory is ClassicQueryTranslatorFactory);
		}

		#region Overrides of TestCase

		protected override IList Mappings
		{
			get { return new string[0]; }
		}

		#endregion

		protected override void Configure(Cfg.Configuration configuration)
		{
			var assembly = GetType().Assembly;
			string mappingNamespace = GetType().Namespace;
			foreach (var resource in assembly.GetManifestResourceNames())
			{
				if (resource.StartsWith(mappingNamespace) && resource.EndsWith(".hbm.xml"))
				{
					configuration.AddResource(resource, assembly);
				}
			}
		}

		public string GetSql(string query)
		{
			return GetSql(query, null);
		}

		public string GetSql(string query, IDictionary<string, string> replacements)
		{
			var qt = new QueryTranslatorImpl(null, new HqlParseEngine(query, false, sessions).Parse(), emptyfilters, sessions);
			qt.Compile(replacements, false);
			return qt.SQLString;
		}

		public string GetSqlWithClassicParser(string query)
		{
			var qt = new NHibernate.Hql.Classic.QueryTranslator(null, query, emptyfilters, sessions);
			qt.Compile(new Dictionary<string, string>(), false);
			return qt.SQLString;
		}
	}
}