using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ext.Core;

namespace StubDb
{
    public delegate object ObjectActivator(params object[] args);
    public delegate object ObjectCloner(object obj);

    public static class EntityTypeManager
    {
        private static readonly IDictionary<Type, IEnumerable<PropertyInfo>> PropertiesCache = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly IDictionary<Type, IEnumerable<MethodInfo>> MethodsCache = new Dictionary<Type, IEnumerable<MethodInfo>>();
        private static readonly IDictionary<string, ObjectActivator> ActivatorsCache = new Dictionary<string, ObjectActivator>();
        private static ObjectCloner _objectCloner = null;

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

        public static IEnumerable<MethodInfo> GetMethods(Type type)
        {
            if (!MethodsCache.ContainsKey(type))
            {
                lock (Lock)
                {
                    if (!MethodsCache.ContainsKey(type))
                    {
                        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        MethodsCache.Add(type, methods);
                    }
                }
            }
            return MethodsCache[type];
        }

        public static IEnumerable<PropertyInfo> GetSimpleWritableProperties(Type type)
        {
            return GetProperties(type).Where(p => IsSimpleType(p.PropertyType) && p.SetMethod != null);
        }

        public static bool UseFullTypeNameAsId { get; set; }

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

        public static bool IsTypedEnumerable(Type type)
        {
            var genericType = GetEnumerableType(type);
            return genericType != null;
        }

        public static Type GetEnumerableType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            foreach (Type intType in type.GetInterfaces())
            {
                if (intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return intType.GetGenericArguments()[0];
                }
            }
            return null;
        }

        //TODO check if clear instead of creating new will improve performance
        public static IList CreateGenericList(Type listType)
        {
            var genericListType = typeof(List<>);
            var concreteType = genericListType.MakeGenericType(listType);
            var newList = CreateNew(concreteType);
            return newList as IList;
        }

        public static string GetTypeId(Type type)
        {
            return UseFullTypeNameAsId ? type.FullName : type.Name;
        }

        public static string GetId(this Type type)
        {
            return GetTypeId(type);
        }

        private static ObjectActivator GetActivator(ConstructorInfo ctor)
        {
            var typeName = ctor.DeclaringType.FullName;

            if (!ActivatorsCache.ContainsKey(typeName))
            {
                lock (Lock)
                {
                    if (!ActivatorsCache.ContainsKey(typeName))
                    {
                        var activator = GetNewActivator(ctor);
                        ActivatorsCache.Add(typeName, activator);
                    }
                }
            }
            return ActivatorsCache[typeName];
        }

        private static ObjectActivator GetNewActivator(ConstructorInfo ctor)
        {
            Type type = ctor.DeclaringType;
            ParameterInfo[] paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            ParameterExpression param =
                Expression.Parameter(typeof(object[]), "args");

            Expression[] argsExp =
                new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;

                Expression paramAccessorExp =
                    Expression.ArrayIndex(param, index);

                Expression paramCastExp =
                    Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            NewExpression newExp = Expression.New(ctor, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            LambdaExpression lambda =
                Expression.Lambda(typeof(ObjectActivator), newExp, param);

            //compile it
            ObjectActivator compiled = (ObjectActivator)lambda.Compile();
            return compiled;
        }

        private static ObjectCloner GetCloner()
        {
            if (_objectCloner == null)
            {
                var cloneParam = Expression.Parameter(typeof(object));
                var memberwiseClone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
                var cloneExp = Expression.Call(cloneParam, memberwiseClone);
                LambdaExpression lambda = Expression.Lambda(typeof(ObjectCloner), cloneExp, cloneParam);
                _objectCloner = (ObjectCloner)lambda.Compile();
            }
            return _objectCloner;
        }

        public static object CloneObject(object obj)
        {
            return GetCloner().Invoke(obj);
        }

        public static object CreateNew(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            Check.NotNull(constructor, "Object to clone should have parameterless constructor");

            var activator = GetActivator(constructor);

            var newObject = activator();

            return newObject;
        }

        public static int GetEntityId(object entity)
        {
            var type = entity.GetType();

            var idProp = EntityTypeManager.GetProperties(type).SingleOrDefault(x => x.Name == "Id");

            Check.That(idProp != null, "Entity type does not have id property");

            Check.That(idProp.PropertyType == typeof(int), "Entity id is not of type integer");

            return (int)idProp.GetValue(entity);
        }

        public static void SetEntityAsNew(object entity)
        {
            var id = GetEntityId(entity);

            if (id != 0)
            {
                SetEntityId(entity, 0);
            }
        }

        public static void SetEntityId(object entity, int id)
        {
            var type = entity.GetType();
            var idProp = EntityTypeManager.GetProperties(type).SingleOrDefault(x => x.Name == "Id");
            idProp.SetValue(entity, id);
        }
    }
}
