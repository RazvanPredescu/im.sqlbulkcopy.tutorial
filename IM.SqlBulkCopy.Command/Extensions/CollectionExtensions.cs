using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace E6.Metrics.BI.Helpers
{
    /// <summary>
    /// Original code by Jörgen Andersson
    /// http://www.codeproject.com/Articles/890048/A-generic-Collection-to-DataTable-Mapper
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Creates a DataTable from an IEnumerable
        /// </summary>
        /// <typeparam name="TSource">The Generic type of the Collection</typeparam>
        /// <param name="Collection"></param>
        /// <returns>DataTable</returns>
        public static DataTable AsDataTable<TSource>(this IEnumerable<TSource> Collection)
        {
            DataTable dt = DataTableCreator<TSource>.GetDataTable();
            Func<TSource, object[]> Map = DataRowMapperCache<TSource>.GetDataRowMapper(dt);

            foreach (TSource item in Collection)
            {
                dt.Rows.Add(Map(item));
            }
            return dt;
        }

        /// <summary>
        /// Creates a DataTable with the same fields as the Generic Type argument
        /// </summary>
        /// <typeparam name="TSource">The Generic type</typeparam>
        /// <returns>DataTable</returns>
        static internal DataTable CreateDataTable<TSource>()
        {
            DataTable dt = new DataTable();
            foreach (FieldInfo SourceMember in typeof(TSource).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                dt.AddTableColumn(SourceMember, SourceMember.FieldType);
            }

            foreach (PropertyInfo SourceMember in typeof(TSource).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (SourceMember.CanRead)
                {
                    dt.AddTableColumn(SourceMember, SourceMember.PropertyType);
                }
            }
            return dt;
        }

        /// <summary>
        /// Adds a Column to a DataTable
        /// </summary>
        public static void AddTableColumn(this DataTable dt, MemberInfo SourceMember, Type MemberType)
        {
            if (MemberType.IsAllowedType())
            {
                DataColumn dc;
                string FieldName = GetFieldNameAttribute(SourceMember);
                if (string.IsNullOrWhiteSpace(FieldName))
                {
                    FieldName = SourceMember.Name;
                }
                if (Nullable.GetUnderlyingType(MemberType) == null)
                {
                    dc = new DataColumn(FieldName, MemberType);
                    dc.AllowDBNull = !MemberType.IsValueType;
                }
                else
                {
                    dc = new DataColumn(FieldName, Nullable.GetUnderlyingType(MemberType));
                    dc.AllowDBNull = true;
                }
                dt.Columns.Add(dc);
            }
        }

        /// <summary>
        /// Returns The FieldNameAttribute if existing
        /// </summary>
        /// <param name="Member">MemberInfo</param>
        /// <returns>String</returns>
        private static string GetFieldNameAttribute(MemberInfo Member)
        {
            if (Member.GetCustomAttributes(typeof(FieldNameAttribute), true).Count() > 0)
            {
                return ((FieldNameAttribute)Member.GetCustomAttributes(typeof(FieldNameAttribute), true)[0]).FieldName;
            }
            else
            {
                return string.Empty;
            }
        }

        private static bool IsAllowedType(this Type t)
        {
            return AllowedTypes.Contains(t);
        }
        readonly static HashSet<Type> AllowedTypes = LoadAllowedTypes();
        private static HashSet<Type> LoadAllowedTypes()
        {
            HashSet<Type> set = new HashSet<Type>
            {
                typeof(String),
                typeof(Char),
                typeof(Byte[]),
                typeof(Byte),
                typeof(SByte),
                typeof(Int16),
                typeof(Int32),
                typeof(Int64),
                typeof(UInt16),
                typeof(UInt32),
                typeof(UInt64),
                typeof(Single),
                typeof(Double),
                typeof(Decimal),
                typeof(DateTime),
                typeof(Guid),
                typeof(Boolean),
                typeof(TimeSpan),
                typeof(Nullable<Char>),
                typeof(Nullable<Byte>),
                typeof(Nullable<SByte>),
                typeof(Nullable<Int16>),
                typeof(Nullable<Int32>),
                typeof(Nullable<Int64>),
                typeof(Nullable<UInt16>),
                typeof(Nullable<UInt32>),
                typeof(Nullable<UInt64>),
                typeof(Nullable<Single>),
                typeof(Nullable<Double>),
                typeof(Nullable<Decimal>),
                typeof(Nullable<DateTime>),
                typeof(Nullable<Guid>),
                typeof(Nullable<Boolean>),
                typeof(Nullable<TimeSpan>)
            };

            return set;
        }


        /// <summary>
        /// Checks if the Field name matches the Member name or Members FieldNameAttribute
        /// </summary>
        /// <param name="Member">The Member of the Instance to check</param>
        /// <param name="Name">The Name to compare with</param>
        /// <returns>True if Fields match</returns>
        /// <remarks>FieldNameAttribute takes precedence over TargetMembers name.</remarks>
        private static bool MemberMatchesName(MemberInfo Member, string Name)
        {
            string FieldnameAttribute = GetFieldNameAttribute(Member);
            return FieldnameAttribute.ToLower() == Name.ToLower() || Member.Name.ToLower() == Name.ToLower();
        }

        /// <summary>
        /// Creates an Expression representing the value of the SourceMember
        /// </summary>
        /// <param name="SourceInstanceExpression"></param>
        /// <param name="SourceMember"></param>
        /// <returns></returns>
        private static Expression GetSourceValueExpression(ParameterExpression SourceInstanceExpression, MemberInfo SourceMember)
        {
            MemberExpression MemberExpression = Expression.PropertyOrField(SourceInstanceExpression, SourceMember.Name);
            Expression SourceValueExpression;

            if (Nullable.GetUnderlyingType(SourceMember.ReflectedType) == null)
            {
                SourceValueExpression = Expression.Convert(MemberExpression, typeof(object));
            }
            else
            {
                SourceValueExpression = Expression.Condition(
                    Expression.Property(Expression.Constant(SourceInstanceExpression), "HasValue"),
                    MemberExpression,
                    Expression.Constant(DBNull.Value),
                    typeof(object));
            }
            return SourceValueExpression;
        }

        /// <summary>
        /// Creates a delegate that maps an instance of TSource to an ItemArray of the supplied DataTable
        /// </summary>
        /// <typeparam name="TSource">The Generic Type to map from</typeparam>
        /// <param name="dt">The DataTable to map to</param>
        /// <returns>Func(Of TSource, Object())</returns>
        static internal Func<TSource, object[]> CreateDataRowMapper<TSource>(DataTable dt)
        {
            Type SourceType = typeof(TSource);
            ParameterExpression SourceInstanceExpression = Expression.Parameter(SourceType, "SourceInstance");
            List<Expression> Values = new List<Expression>();

            foreach (DataColumn col in dt.Columns)
            {
                foreach (FieldInfo SourceMember in SourceType.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (MemberMatchesName(SourceMember, col.ColumnName))
                    {
                        Values.Add(GetSourceValueExpression(SourceInstanceExpression, SourceMember));
                        break;
                    }
                }
                foreach (PropertyInfo SourceMember in SourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (SourceMember.CanRead && MemberMatchesName(SourceMember, col.ColumnName))
                    {
                        Values.Add(GetSourceValueExpression(SourceInstanceExpression, SourceMember));
                        break;
                    }
                }
            }
            NewArrayExpression body = Expression.NewArrayInit(Type.GetType("System.Object"), Values);
            return Expression.Lambda<Func<TSource, object[]>>(body, SourceInstanceExpression).Compile();
        }

        private sealed class DataRowMapperCache<TSource>
        {
            private DataRowMapperCache() { }

            private static readonly object LockObject = new object();
            private static Func<TSource, object[]> Mapper = null;

            static internal Func<TSource, object[]> GetDataRowMapper(DataTable dt)
            {
                if (Mapper == null)
                {
                    lock (LockObject)
                    {
                        if (Mapper == null)
                        {
                            Mapper = CreateDataRowMapper<TSource>(dt);
                        }
                    }
                }
                return Mapper;
            }
        }

        private sealed class DataTableCreator<TSource>
        {
            private DataTableCreator() { }

            private static readonly object LockObject = new object();
            private static DataTable EmptyDataTable;

            static internal DataTable GetDataTable()
            {
                if (EmptyDataTable == null)
                {
                    lock (LockObject)
                    {
                        if (EmptyDataTable == null)
                        {
                            EmptyDataTable = CreateDataTable<TSource>();
                        }
                    }
                }
                return EmptyDataTable.Clone();
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class FieldNameAttribute : Attribute
    {
        private readonly string _FieldName;
        public string FieldName
        {
            get { return _FieldName; }
        }

        public FieldNameAttribute(string FieldName)
        {
            _FieldName = FieldName;
        }
    }
}