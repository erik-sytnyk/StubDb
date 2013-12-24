using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ext.Core;
using Ext.Core.Collections;

namespace StubDb
{
    public class StubContext
    {
        #region Nested Classes

        //TODO add lock
        public class EntityCollection
        {
            Dictionary<string, Dictionary<int, object>> _storage = new Dictionary<string, Dictionary<int, object>>();

            public void Add<T>(int id, T entity)
            {
                var type = typeof(T);

                _storage.AddIfNoEntry(type.FullName, new Dictionary<int, object>());

                _storage[type.FullName].Add(id, entity);
            }

            public T GetById<T>(int id)
            {
                return (T)GetById(id, typeof(T));
            }

            public object GetById(int id, Type type)
            {
                _storage.AddIfNoEntry(type.FullName, new Dictionary<int, object>());

                var dict = _storage[type.FullName];

                return dict.ContainsKey(id) ? dict[id] : type.GetDefault();
            }

            public void Remove<T>(int id, T entity)
            {
                var type = typeof(T);

                _storage.AddIfNoEntry(type.FullName, new Dictionary<int, object>());

                _storage[type.FullName].Remove(id);
            }

            public int GetAvailableIdForEntityType<T>(T entityInstance)
            {
                var type = typeof(T);

                _storage.AddIfNoEntry(type.FullName, new Dictionary<int, object>());

                var dict = _storage[type.FullName];

                return dict.Keys.Count > 0 ? dict.Keys.Max(x => x) + 1 : 1;
            }

            public Dictionary<int, object> GetEntities<T>()
            {
                var type = typeof(T);

                return _storage[type.FullName];
            }
        }

        public class ConnectionsCollection
        {
            private List<EntityConnection> _storage = new List<EntityConnection>();

            public void AddConnection(Type typeFirst, Type typeSecond, int idFirst, int idSecond)
            {
                var newConnection = new EntityConnection(typeFirst.FullName, typeSecond.FullName, idFirst, idSecond);

                var existingConnection = _storage.FirstOrDefault(x => x.Equals(newConnection));

                if (existingConnection == null)
                {
                    _storage.Add(newConnection);
                }
            }

            public void RemoveConnectionsFor(Type entityType, int entityId)
            {
                _storage.RemoveAll(x => x.TypeFirst == entityType.FullName && x.IdFirst == entityId);
                _storage.RemoveAll(x => x.TypeSecond == entityType.FullName && x.IdSecond == entityId);
            }

            public void RemoveConnectionsFor(Type entityType, int entityId, Type connectionType)
            {
                var isRightOrder = EntityConnection.IsRightOrder(entityType, connectionType);

                if (isRightOrder)
                {
                    _storage.RemoveAll(x => x.TypeFirst == entityType.FullName && x.TypeSecond == connectionType.FullName && x.IdFirst == entityId);
                }
                else
                {
                    _storage.RemoveAll(x => x.TypeSecond == entityType.FullName && x.TypeFirst == connectionType.FullName && x.IdSecond == entityId);
                }
            }

            public List<EntityConnection> GetConnectionsFor(Type entityType, int entityId, Type connectionType)
            {
                var result = (List<EntityConnection>)null;
                var isRightOrder = EntityConnection.IsRightOrder(entityType, connectionType);

                if (isRightOrder)
                {
                    result = _storage.Where(x => x.TypeFirst == entityType.FullName && x.TypeSecond == connectionType.FullName && x.IdFirst == entityId).ToList();
                }
                else
                {
                    result = _storage.Where(x => x.TypeSecond == entityType.FullName && x.TypeFirst == connectionType.FullName && x.IdSecond == entityId).ToList();
                }

                return result;
            }
        }

        public class EntityConnection
        {
            public string TypeFirst { get; set; }
            public string TypeSecond { get; set; }
            public int IdFirst { get; set; }
            public int IdSecond { get; set; }

            public EntityConnection(string type1, string type2, int id1, int id2)
            {
                if (IsRightOrder(type1, type2))
                {
                    TypeFirst = type1;
                    TypeSecond = type2;
                    IdFirst = id1;
                    IdSecond = id2;
                }
                else
                {
                    TypeFirst = type2;
                    TypeSecond = type1;
                    IdFirst = id2;
                    IdSecond = id1;
                }
            }

            public override bool Equals(object obj)
            {
                var entityConnnection = obj as EntityConnection;

                if (entityConnnection == null) return false;

                return entityConnnection.IdFirst == this.IdFirst && entityConnnection.IdSecond == this.IdSecond
                    && entityConnnection.TypeFirst == this.TypeFirst && entityConnnection.TypeSecond == this.TypeSecond;
            }

            public override int GetHashCode()
            {
                return TypeFirst.GetHashCode() + TypeSecond.GetHashCode();
            }

            //TODO better name
            public static bool IsRightOrder(string typeNameFirst, string typeNameSecond)
            {
                return System.String.Compare(typeNameFirst, typeNameSecond, System.StringComparison.Ordinal) > 0;
            }

            public static bool IsRightOrder(Type typeFirst, Type typeSecond)
            {
                return IsRightOrder(typeFirst.FullName, typeSecond.FullName);
            }

            public int GetIdByType(Type connectedEntityType)
            {
                if (TypeFirst == connectedEntityType.FullName)
                {
                    return IdFirst;
                }
                else if (TypeSecond == connectedEntityType.FullName)
                {
                    return IdSecond;
                }

                throw new Exception("Connection does not have this type");
            }
        }

        #endregion

        protected EntityCollection Entities = new EntityCollection();
        protected Dictionary<string, Type> Types = new Dictionary<string, Type>();
        protected ConnectionsCollection Connections = new ConnectionsCollection();

        public StubContext()
        {
            var stubSets = EntityTypesCache.GetProperties(this.GetType()).Where(x => IsStubSet(x.PropertyType)).ToList();

            foreach (var propertyInfo in stubSets)
            {
                var stubSet = Activator.CreateInstance(propertyInfo.PropertyType);
                stubSet.SetProperty("Context", this);
                propertyInfo.SetValue(this, stubSet);
            }

            var types = GetEntityTypes(this.GetType());
            foreach (var type in types)
            {
                this.RegisterType(type);
            }
        }

        //TODO performance
        public StubSet<TEntity> GetStubSet<TEntity>()
        {
            var result = (StubSet<TEntity>)null;

            var property = EntityTypesCache.GetProperties(this.GetType()).SingleOrDefault(x => IsStubSet(x.PropertyType) && GetCollectionType(x.PropertyType) == typeof(TEntity));

            if (property != null)
            {
                result = (StubSet<TEntity>)property.GetValue(this);
            }

            return result;
        }

        public void Add<T>(T entity)
        {
            this.SetEntityAsNew(entity);
            this.Save(entity);
        }

        public void Update<T>(T entity)
        {
            this.Save(entity);
        }

        private void Save<T>(T entity)
        {
            this.CheckIsEntityType(typeof(T));

            var properties = EntityTypesCache.GetProperties(typeof(T));

            var entityId = GetEntityId(entity);

            var existingEntity = this.Entities.GetById<T>(entityId);
            var isExistingEntity = existingEntity != null;

            if (!isExistingEntity)
            {
                var newId = Entities.GetAvailableIdForEntityType(entity);
                
                SetEntityId(entity, newId);

                this.Entities.Add(newId, entity);
            }

            foreach (var propertyInfo in properties)
            {
                var entitiesToAdd = new List<object>();

                if (IsCollection(propertyInfo.PropertyType))
                {                    
                    var collection = propertyInfo.GetValue(entity) as IEnumerable;

                    if (collection == null)
                    {
                        collection = (IEnumerable) Activator.CreateInstance(propertyInfo.PropertyType);
                        propertyInfo.SetValue(entity, collection);
                    }

                    foreach (var item in collection)
                    {
                        entitiesToAdd.Add(item);
                    }
                }
                else if (Types.ContainsKey(propertyInfo.PropertyType.FullName))
                {
                    var entityToAdd = propertyInfo.GetValue(entity);

                    if (entityToAdd != null)
                    {
                        entitiesToAdd.Add(entityToAdd);
                    }
                }

                if (entitiesToAdd.Count > 0)
                {
                    var connectedType = entitiesToAdd.First().GetType();

                    Connections.RemoveConnectionsFor(typeof(T), entityId, connectedType);

                    foreach (var entityToAdd in entitiesToAdd)
                    {
                        Connections.AddConnection(entity.GetType(), entityToAdd.GetType(), GetEntityId(entity), GetEntityId(entityToAdd));
                    }
                }
            }
        }

        public void Remove<T>(T entity)
        {
            var entityId = this.GetEntityId(entity);
            Connections.RemoveConnectionsFor(entity.GetType(), entityId);
            Entities.Remove(entityId, entity);
        }

        public IQueryable<T> Query<T>()
        {
            var list = new List<T>();

            var entities = Entities.GetEntities<T>();

            foreach (var entity in entities.Values)
            {
                list.Add((T)entity);

                var entityId = this.GetEntityId(entity);

                foreach (var propertyInfo in EntityTypesCache.GetProperties(typeof(T)))
                {
                    if (IsCollection(propertyInfo.PropertyType))
                    {
                        var connectedEntityType = GetCollectionType(propertyInfo.PropertyType);
                        var connections = Connections.GetConnectionsFor(typeof(T), entityId, connectedEntityType);

                        if (connections.Count > 0)
                        {
                            var connectedList = propertyInfo.GetValue(entity) as IList;

                            if (connectedList == null)
                            {
                                connectedList = Activator.CreateInstance(propertyInfo.GetType()) as IList;

                                Check.NotNull(connectedList, "Collections in entity should implement IList"); //TODO support ICollection

                                propertyInfo.SetValue(entity, connectedList);
                            }
                            else
                            {
                                connectedList.Clear();   
                            }

                            foreach (var entityConnection in connections)
                            {
                                var connectedId = entityConnection.GetIdByType(connectedEntityType);

                                var entityToAdd = Entities.GetById(connectedId, connectedEntityType);

                                connectedList.Add(entityToAdd);
                            }
                        }
                    }
                    else if (!IsSimpleType(propertyInfo.PropertyType))
                    {
                        var connectedEntityType = propertyInfo.PropertyType;
                        var connections = Connections.GetConnectionsFor(typeof(T), entityId, connectedEntityType);

                        Check.That(connections.Count <= 1, "Multiple connections for one to one relation");

                        if (connections.Count == 1)
                        {
                            var entityConnection = connections.Single();

                            var connectedId = entityConnection.GetIdByType(connectedEntityType);

                            var connectedEntity = Entities.GetById(connectedId, connectedEntityType);

                            propertyInfo.SetValue(entity, connectedEntity);
                        }
                        else //no connection, so clear property
                        {
                            propertyInfo.SetValue(entity, null);
                        }
                    }
                }
            }

            return list.AsQueryable();
        }

        public void SaveData()
        {            
            //later
            //persist entities
            //persist connections
        }

        public void LoadData()
        {
            //later
            //upload entites
            //upload connections
            //referesh data
        }

        public static List<Type> GetEntityTypes(Type containerType)
        {
            var result = new Dictionary<string, Type>();

            var enumerableProperties = EntityTypesCache.GetProperties(containerType).Where(x => IsStubSet(x.PropertyType));

            foreach (var enumerableProperty in enumerableProperties)
            {
                var typeOfCollection = GetCollectionType(enumerableProperty.PropertyType);
                result.AddIfNoEntry(typeOfCollection.FullName, typeOfCollection);
                AddEntityTypes(typeOfCollection, result);
            }

            return result.Values.ToList();
        }

        #region Helper functions

        public void RegisterType(Type type)
        {
            this.Types.Add(type.FullName, type);
        }

        private void CheckIsEntityType(Type type)
        {
            Check.That(Types.ContainsKey(type.FullName), "Not registered entity type");
        }

        public int GetEntityId<T>(T entity)
        {
            var type = entity.GetType();

            var idProp = EntityTypesCache.GetProperties(type).SingleOrDefault(x => x.Name == "Id");

            Check.That(idProp != null, "Entity type does not have id property");

            Check.That(idProp.PropertyType == typeof(int), "Entity id is not of type integer");

            return (int)idProp.GetValue(entity);
        }

        private bool IsEntityNew<T>(T entity)
        {
            return GetEntityId(entity) != 0; //(int)entity.GetType().GetDefault();
        }

        private void SetEntityAsNew<T>(T entity)
        {
            var id = this.GetEntityId(entity);

            if (id != 0)
            {
                this.SetEntityId(entity, 0);
            }
        }

        private void SetEntityId<T>(T entity, int id)
        {
            var type = entity.GetType();
            var idProp = EntityTypesCache.GetProperties(type).SingleOrDefault(x => x.Name == "Id");
            idProp.SetValue(entity, id);
        }

        private static void AddEntityTypes(Type type, Dictionary<string, Type> typesDict)
        {
            var properties = EntityTypesCache.GetProperties(type);
            foreach (var propertyInfo in properties)
            {
                var entityType = (Type)null;

                if (IsCollection(propertyInfo.PropertyType))
                {
                    entityType = GetCollectionType(propertyInfo.PropertyType);
                }
                else if (!IsSimpleType(propertyInfo.PropertyType))
                {
                    entityType = propertyInfo.PropertyType;
                }

                if (entityType != null && !typesDict.ContainsKey(entityType.FullName))
                {
                    typesDict.AddIfNoEntry(entityType.FullName, entityType);
                    AddEntityTypes(entityType, typesDict);
                }
            }
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

        #endregion
    }
}
