using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ext.Core;
using Ext.Core.Reflection;

namespace StubDb
{
    public static class EntityTypeManager
    {
        public static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            return ReflectionHelper.GetProperties(type);
        }

        public static IEnumerable<MethodInfo> GetMethods(Type type)
        {
            return ReflectionHelper.GetMethods(type);
        }

        public static IEnumerable<PropertyInfo> GetSimpleWritableProperties(Type type)
        {
            return GetProperties(type).Where(p => IsSimpleOrSimpleEnumerableType(p.PropertyType) && p.SetMethod != null);
        }

        public static bool UseFullTypeNameAsId { get; set; }
        
        public static bool IsSimpleOrSimpleEnumerableType(Type type)
        {
            var enumerableType = GetEnumerableType(type);

            if (enumerableType != null)
            {
                return IsSimpleType(enumerableType);
            }

            return IsSimpleType(type);
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
        
        public static bool IsEnumerableEntityType(Type type)
        {
            var genericType = GetEnumerableEntityType(type);
            return genericType != null;
        }
        
        public static Type GetEnumerableEntityType(Type type)
        {
            var result = GetEnumerableType(type);

            if (result != null && IsSimpleType(result))
            {
                result = null;
            }

            return result;
        }

        public static Type GetEnumerableType(Type type)
        {
            return ReflectionHelper.GetEnumerableType(type);
        }

        public static IList CreateGenericList(Type listType)
        {
            return ReflectionHelper.CreateGenericList(listType);
        }

        public static string GetTypeId(Type type)
        {
            return UseFullTypeNameAsId ? type.FullName : type.Name;
        }

        public static string GetId(this Type type)
        {
            return GetTypeId(type);
        }

        public static object CloneObject(object obj)
        {
            return ReflectionHelper.CloneObject(obj);
        }

        public static object CreateNew(Type type)
        {
            return ReflectionHelper.CreateNew(type);
        }

        public static PropertyInfo GetEntityIdProperty(Type type)
        {
            var typeNamePlusId = String.Format("{0}Id", type.Name);

            var result = EntityTypeManager.GetProperties(type).SingleOrDefault(x => x.Name.Equals("id", StringComparison.OrdinalIgnoreCase) || x.Name.Equals(typeNamePlusId, StringComparison.OrdinalIgnoreCase));

            Check.That(result != null, String.Format("Entity type '{0}' does not have id property", type.Name));

            Check.That(result.PropertyType == typeof(int), "Entity id is not of type integer");

            return result;
        }

        public static PropertyInfo GetEntityNavigationIdProperty(Type type, Type navigationType)
        {
            var navigationTypeNamePlusId = String.Format("{0}Id", navigationType.Name);

            var result = EntityTypeManager.GetProperties(type).SingleOrDefault(x => x.Name.Equals(navigationTypeNamePlusId, StringComparison.OrdinalIgnoreCase));

            if (result != null)
            {
                Check.That(result.PropertyType == typeof (int) || result.PropertyType == typeof(int?), "Navigation ID property is not of type integer");
            }

            return result;
        }
    }
}
