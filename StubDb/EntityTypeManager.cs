using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ext.Core;

namespace StubDb
{
    public static class EntityTypeManager
    {
        private static readonly IDictionary<Type, IEnumerable<PropertyInfo>> PropertiesCache = new Dictionary<Type, IEnumerable<PropertyInfo>>();

        private static readonly object Lock = new object();

        public static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            if (!PropertiesCache.ContainsKey(type))
            {
                lock (Lock)
                {
                    if (!PropertiesCache.ContainsKey(type))
                    {
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        PropertiesCache.Add(type, props);
                    }
                }
            }
            return PropertiesCache[type];
        }

        public static IEnumerable<PropertyInfo> GetSimpleWritableProperties(Type type)
        {
            return GetProperties(type).Where(p => IsSimpleType(p.PropertyType) && p.SetMethod != null);
        }

        public static bool IsSimpleType(Type type)
        {
            if (type.IsValueType || type == typeof(string))
            {
                return true;
            }

            return false;
        }

        public static bool IsStubSet(Type type)
        {
            var result = type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IStubSet<>));
            return result;
        }

        public static bool IsCollection(Type type)
        {
            var result = type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
            return result;
        }

        public static Type GetCollectionType(Type type)
        {
            return type.GetGenericArguments().Single();
        }
    }
}
