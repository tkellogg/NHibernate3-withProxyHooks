using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using NHibernate.Engine;
using NHibernate.SqlTypes;

namespace NHibernate.Type
{
	/// <summary>
	/// Maps a <see cref="System.UInt32"/> Property 
	/// to a <see cref="DbType.UInt32"/> column.
	/// </summary>
	[Serializable]
	public class UInt32Type : PrimitiveType, IDiscriminatorType, IVersionType
	{
		/// <summary></summary>
		internal UInt32Type() : base(SqlTypeFactory.UInt32)
		{
		}

		/// <summary></summary>
		public override string Name
		{
			get { return "UInt32"; }
		}

		private static readonly UInt32 ZERO = 0;
		public override object Get(IDataReader rs, int index)
		{
			try
			{
				return Convert.ToUInt32(rs[index]);
			}
			catch (Exception ex)
			{
				throw new FormatException(string.Format("Input string '{0}' was not in the correct format.", rs[index]), ex);
			}
		}

		public override object Get(IDataReader rs, string name)
		{
			try
			{
				return Convert.ToUInt32(rs[name]);
			}
			catch (Exception ex)
			{
				throw new FormatException(string.Format("Input string '{0}' was not in the correct format.", rs[name]), ex);
			}
		}

		public override System.Type ReturnedClass
		{
			get { return typeof(UInt32); }
		}

		public override void Set(IDbCommand rs, object value, int index)
		{
			((IDataParameter)rs.Parameters[index]).Value = value;
		}

		public object StringToObject(string xml)
		{
			return FromStringValue(xml);
		}

		public override object FromStringValue(string xml)
		{
			return UInt32.Parse(xml);
		}

		#region IVersionType Members

		public virtual object Next(object current, ISessionImplementor session)
		{
			return (UInt32)current + 1;
		}

		public virtual object Seed(ISessionImplementor session)
		{
			return 1;
		}

		public IComparer Comparator
		{
			get { return Comparer<UInt32>.Default; }
		}

		#endregion

		public override System.Type PrimitiveClass
		{
			get { return typeof(UInt32); }
		}

		public override object DefaultValue
		{
			get { return ZERO; }
		}

		public override string ObjectToSQLString(object value, Dialect.Dialect dialect)
		{
			return value.ToString();
		}
	}
}