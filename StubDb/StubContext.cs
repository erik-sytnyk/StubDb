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
        public class ContextStorage
        {
            private EntityCollection _entities = new EntityCollection();
            private ConnectionsCollection _connections = new ConnectionsCollection();

            public EntityCollection Entities
            {
                get { return _entities; }
                set { _entities = value; }
            }

            public ConnectionsCollection Connections
            {
                get { return _connections; }
                set { _connections = value; }
            }

            public bool IsEmpty
            {
                get { return _entities.IsEmpty && _connections.IsEmpty; }
            }

            public void Clear()
            {
                _entities.Clear();
                _connections.Clear();
            }
        }

        public class EntityCollection
        {
            Dictionary<string, Dictionary<int, object>> _storage = new Dictionary<string, Dictionary<int, object>>();

            public void Add(int id, object entity)
            {
                var type = entity.GetType();

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

            public void Remove(int id, Type type)
            {
                _storage.AddIfNoEntry(type.FullName, new Dictionary<int, object>());

                _storage[type.FullName].Remove(id);
            }

            public int GetAvailableIdForEntityType(Type entityType)
            {
                _storage.AddIfNoEntry(entityType.FullName, new Dictionary<int, object>());

                var dict = _storage[entityType.FullName];

                return dict.Keys.Count > 0 ? dict.Keys.Max(x => x) + 1 : 1;
            }

            public Dictionary<int, object> GetEntities<T>()
            {
                var type = typeof(T);

                return GetEntities(type);
            }

            public Dictionary<int, object> GetEntities(Type entityType)
            {
                var result = new Dictionary<int, object>();

                if (_storage.ContainsKey(entityType.FullName))
                {
                    result = _storage[entityType.FullName];
                }

                return result;
            }

            public bool IsEmpty
            {
                get { return _storage.Count == 0; }
            }

            public void Clear()
            {
                _storage.Clear();
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

            public IEnumerable<EntityConnection> GetAllConnections()
            {
                return _storage;
            }

            public bool IsEmpty
            {
                get { return _storage.Count == 0; }
            }

            public void Clear()
            {
                _storage.Clear();
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

        protected ContextStorage Storage = new ContextStorage();
        protected Dictionary<string, Type> Types = new Dictionary<string, Type>();

        public IContextStoragePersistenceProvider PersistenceProvider { get; set; }

        public StubContext()
        {
            var stubSets = EntityTypeManager.GetProperties(this.GetType()).Where(x => EntityTypeManager.IsStubSet(x.PropertyType)).ToList();

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

            PersistenceProvider = new SerializeToFilePersistenceProvider();
        }

        //TODO performance
        public StubSet<TEntity> GetStubSet<TEntity>()
        {
            var result = (StubSet<TEntity>)null;

            var property = EntityTypeManager.GetProperties(this.GetType()).SingleOrDefault(x => EntityTypeManager.IsStubSet(x.PropertyType) && EntityTypeManager.GetCollectionType(x.PropertyType) == typeof(TEntity));

            if (property != null)
            {
                result = (StubSet<TEntity>)property.GetValue(this);
            }

            return result;
        }

        public void Add(object entity)
        {
            this.SetEntityAsNew(entity);
            this.Save(entity);
        }

        public void Update(object entity)
        {
            this.Save(entity);
        }

        private void Save(object entity)
        {
            var entityType = entity.GetType();
            this.CheckIsEntityType(entityType);

            var properties = EntityTypeManager.GetProperties(entityType);

            var entityId = GetEntityId(entity);

            var existingEntity = this.Storage.Entities.GetById(entityId, entityType);
            var isExistingEntity = existingEntity != null;

            if (!isExistingEntity)
            {
                var newId = this.Storage.Entities.GetAvailableIdForEntityType(entityType);

                SetEntityId(entity, newId);

                this.Storage.Entities.Add(newId, entity);
            }

            foreach (var propertyInfo in properties)
            {
                var entitiesToAdd = new List<object>();

                if (EntityTypeManager.IsCollection(propertyInfo.PropertyType))
                {
                    var collection = propertyInfo.GetValue(entity) as IEnumerable;

                    if (collection == null)
                    {
                        collection = (IEnumerable)Activator.CreateInstance(propertyInfo.PropertyType);
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

                    this.Storage.Connections.RemoveConnectionsFor(entityType, entityId, connectedType);

                    foreach (var entityToAdd in entitiesToAdd)
                    {
                        this.Storage.Connections.AddConnection(entityType, entityToAdd.GetType(), GetEntityId(entity), GetEntityId(entityToAdd));
                    }
                }
            }
        }

        public void Remove(object entity)
        {
            var entityType = entity.GetType();
            this.CheckIsEntityType(entityType);

            var entityId = this.GetEntityId(entity);
            
            this.Remove(entityType, entityId);
        }

        internal void Remove(Type entityType, int id)
        {
            this.Storage.Connections.RemoveConnectionsFor(entityType, id);
            this.Storage.Entities.Remove(id, entityType);
        }

        public IQueryable<T> Query<T>()
        {
            var list = new List<T>();

            var entities = this.Storage.Entities.GetEntities<T>();

            foreach (var entity in entities.Values)
            {
                list.Add((T)entity);

                var entityId = this.GetEntityId(entity);

                foreach (var propertyInfo in EntityTypeManager.GetProperties(typeof(T)))
                {
                    if (EntityTypeManager.IsCollection(propertyInfo.PropertyType))
                    {
                        var connectedEntityType = EntityTypeManager.GetCollectionType(propertyInfo.PropertyType);
                        var connections = this.Storage.Connections.GetConnectionsFor(typeof(T), entityId, connectedEntityType);

                        if (connections.Count > 0)
                        {
                            var connectedList = propertyInfo.GetValue(entity) as IList;

                            if (connectedList == null)
                            {
                                connectedList = Activator.CreateInstance(propertyInfo.PropertyType) as IList;

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

                                var entityToAdd = this.Storage.Entities.GetById(connectedId, connectedEntityType);

                                connectedList.Add(entityToAdd);
                            }
                        }
                    }
                    else if (!EntityTypeManager.IsSimpleType(propertyInfo.PropertyType))
                    {
                        var connectedEntityType = propertyInfo.PropertyType;
                        var connections = this.Storage.Connections.GetConnectionsFor(typeof(T), entityId, connectedEntityType);

                        Check.That(connections.Count <= 1, "Multiple connections for one to one relation");

                        if (connections.Count == 1)
                        {
                            var entityConnection = connections.Single();

                            var connectedId = entityConnection.GetIdByType(connectedEntityType);

                            var connectedEntity = this.Storage.Entities.GetById(connectedId, connectedEntityType);

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
            PersistenceProvider.SaveContext(Storage, Types);
        }

        public void LoadData()
        {
            PersistenceProvider.LoadContext(this.Storage, Types);
        }

        public bool IsEmpty
        {
            get { return this.Storage.IsEmpty; }
        }

        public static List<Type> GetEntityTypes(Type containerType)
        {
            var result = new Dictionary<string, Type>();

            var enumerableProperties = EntityTypeManager.GetProperties(containerType).Where(x => EntityTypeManager.IsStubSet(x.PropertyType));

            foreach (var enumerableProperty in enumerableProperties)
            {
                var typeOfCollection = EntityTypeManager.GetCollectionType(enumerableProperty.PropertyType);
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

            var idProp = EntityTypeManager.GetProperties(type).SingleOrDefault(x => x.Name == "Id");

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
            var idProp = EntityTypeManager.GetProperties(type).SingleOrDefault(x => x.Name == "Id");
            idProp.SetValue(entity, id);
        }

        private static void AddEntityTypes(Type type, Dictionary<string, Type> typesDict)
        {
            var properties = EntityTypeManager.GetProperties(type);
            foreach (var propertyInfo in properties)
            {
                var entityType = (Type)null;

                if (EntityTypeManager.IsCollection(propertyInfo.PropertyType))
                {
                    entityType = EntityTypeManager.GetCollectionType(propertyInfo.PropertyType);
                }
                else if (!EntityTypeManager.IsSimpleType(propertyInfo.PropertyType))
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

        #endregion
    }
}
