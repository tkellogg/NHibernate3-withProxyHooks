using System.Collections;
using NHibernate.Criterion;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH266
{
	[TestFixture]
	public class Fixture : TestCase
	{
		private static int aId = 1;
		private static int bId = 2;
		private static int cId = 3;

		protected override string MappingsAssembly
		{
			get { return "NHibernate.Test"; }
		}

		protected override IList Mappings
		{
			get { return new string[] {"NHSpecificTest.NH266.Mappings.hbm.xml"}; }
		}

		protected override void OnSetUp()
		{
			ISession s = OpenSession();
			A a = new A();
			a.Id = aId;
			a.Name = "the a";

			B b = new B();
			b.Id = bId;
			b.Name = "the b";
			b.Number = 4L;

			C c = new C();
			c.Id = cId;
			c.Name = "the c";
			c.Code = 'c';

			s.Save(a);
			s.Save(b);
			s.Save(c);

			s.Flush();
			s.Close();
		}

		protected override void OnTearDown()
		{
			ISession s = OpenSession();
			s.Delete("from A");
			s.Flush();
			s.Close();
		}


		[Test]
		public void BaseClassLoad()
		{
			// just do a straight load
			ISession s = OpenSession();
			A a = s.Load(typeof(A), aId) as A;
			Assert.AreEqual("the a", a.Name);
			s.Close();


			// load instance through hql
			s = OpenSession();
			IQuery q = s.CreateQuery("from A as a where a.id = :id ");
			q.SetParameter("id", aId);
			a = q.UniqueResult() as A;
			Assert.AreEqual("the a", a.Name);

			s.Close();

			// load instance through Criteria
			s = OpenSession();
			ICriteria c = s.CreateCriteria(typeof(A));
			c.Add(Expression.Eq("Id", aId));
			a = c.UniqueResult() as A;

			Assert.AreEqual("the a", a.Name);
			s.Close();
		}

		/// <summary>
		/// This is testing problems that existed in 0.8.0-2 with extra "AND"
		/// being added to the sql when there was a discriminator-value.
		/// </summary>
		[Test]
		public void SpecificSubclass()
		{
			ISession s = OpenSession();
			B b = s.Load(typeof(B), bId) as B;
			Assert.AreEqual("the b", b.Name);
			s.Close();

			// load a instance of B through hql
			s = OpenSession();
			IQuery q = s.CreateQuery("from B as b where b.id = :id");
			q.SetParameter("id", bId);
			b = q.UniqueResult() as B;
			Assert.AreEqual("the b", b.Name);
			s.Close();

			// load a instance of B through Criteria
			s = OpenSession();
			ICriteria c = s.CreateCriteria(typeof(B));
			c.Add(Expression.Eq("Id", bId));
			b = c.UniqueResult() as B;

			Assert.AreEqual("the b", b.Name);
			s.Close();
		}
	}
}