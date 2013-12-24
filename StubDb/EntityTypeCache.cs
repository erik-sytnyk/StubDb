using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ext.Core;

namespace StubDb
{
    public static class EntityTypesCache
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
    }
}
