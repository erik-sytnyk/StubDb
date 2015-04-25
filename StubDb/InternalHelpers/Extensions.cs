using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace StubDb.InternalHelpers
{
    internal static class Extensions
    {
        //for downgrading to 4.0 framework
        public static void SetValue(this PropertyInfo propertyInfo, object obj, object value)
        {
            propertyInfo.SetValue(obj, value, null);
        }

        //for downgrading to 4.0 framework
        public static object GetValue(this PropertyInfo propertyInfo, object obj)
        {
            return propertyInfo.GetValue(obj, null);
        }

        //for downgrading to 4.0 framework
        public static Type[] GetGenericTypeArguments(this Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                return type.GetGenericArguments();
            else
                return Type.EmptyTypes;
        }
    }
}
