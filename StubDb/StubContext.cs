using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ext.Core;
using Ext.Core.Collections;
using StubDb.ModelStorage;
using StubDb.Persistence;

namespace StubDb
{
    public class StubContext
    {
        #region Nested Classes

        public delegate void SeedDataAction(StubContext context);

        #endregion

        internal ContextStorage Storage = new ContextStorage();
        internal List<RequiredDependancy> RequiredDependancies { get; set; }
        protected ModelBuilder ModelBuilder { get; set; }
        public IContextStoragePersistenceProvider PersistenceProvider { get; set; }
        public EntityTypeCollection Types { get; set; }

        internal bool DoDataConsistencyTest = true;

        public StubContext()
        {
            Types = new EntityTypeCollection();
            RequiredDependancies = new List<RequiredDependancy>();

            ModelBuilder = new ModelBuilder(this);

            var stubSets = EntityTypeManager.GetProperties(this.GetType()).Where(x => EntityTypeManager.IsStubSet(x.PropertyType)).ToList();

            foreach (var propertyInfo in stubSets)
            {
                var stubSet = EntityTypeManager.CreateNew(propertyInfo.PropertyType);
                stubSet.SetProperty("Context", this);
                propertyInfo.SetValue(this, stubSet);
            }

            RegisterEntityTypes(this.GetType());

            this.ConfigureModel();

            PersistenceProvider = new SerializeToFilePersistenceProvider();
        }

        public virtual void ConfigureModel()
        {

        }

        public StubSet<TEntity> GetStubSet<TEntity>()
        {
            var result = (StubSet<TEntity>)null;

            var property = EntityTypeManager.GetProperties(this.GetType()).SingleOrDefault(x => EntityTypeManager.IsStubSet(x.PropertyType) && x.PropertyType.GenericTypeArguments.First() == typeof(TEntity));

            if (property != null)
            {
                result = (StubSet<TEntity>)property.GetValue(this);
            }

            return result;
        }

        public void Add(object entity)
        {
            EntityTypeManager.SetEntityAsNew(entity);
            this.Save(entity);
        }

        public void Update(object entity)
        {
            this.Save(entity);
        }

        private void Save(object entity)
        {
            var entityType = this.GetEntityType(entity.GetType());

            var properties = entityType.GetProperties();

            var entityId = EntityTypeManager.GetEntityId(entity);

            var existingEntity = this.Storage.Entities.GetById(entityId, entityType);
            var isExistingEntity = existingEntity != null;

            if (!isExistingEntity)
            {
                var newId = this.Storage.Entities.GetAvailableIdForEntityType(entityType);

                EntityTypeManager.SetEntityId(entity, newId);

                this.Storage.Entities.Add(newId, entity);
            }
            else
            {
                this.Storage.Entities.Update(entityId, entity);
            }

            //load connections
            foreach (var propertyInfo in properties)
            {
                var connection = entityType.Connections.SingleOrDefault(x => x.PropertyName == propertyInfo.Name);

                if (connection == null) continue;

                var entitiesToAdd = new List<object>();

                if (connection.IsMultipleConnection)
                {
                    var collection = propertyInfo.GetValue(entity) as IEnumerable;

                    if (collection == null)
                    {
                        collection = (IEnumerable)EntityTypeManager.CreateNew(propertyInfo.PropertyType);
                        propertyInfo.SetValue(entity, collection);
                    }

                    foreach (var item in collection)
                    {
                        entitiesToAdd.Add(item);
                    }
                }
                else
                {
                    var entityToAdd = propertyInfo.GetValue(entity);

                    if (entityToAdd != null)
                    {
                        entitiesToAdd.Add(entityToAdd);
                    }
                }

                if (entitiesToAdd.Count > 0)
                {
                    var connectedType = connection.ConnectedType;

                    foreach (var entityToAdd in entitiesToAdd)
                    {
                        this.Save(entityToAdd);
                    }

                    if (isExistingEntity)
                    {
                        this.Storage.Connections.RemoveConnectionsFor(entityType, connectedType, connection.ConnectionName, entityId);
                    }

                    foreach (var entityToAdd in entitiesToAdd)
                    {
                        this.Storage.Connections.AddConnection(entityType, this.GetEntityType(entityToAdd.GetType()), connection.ConnectionName, EntityTypeManager.GetEntityId(entity), EntityTypeManager.GetEntityId(entityToAdd), DoDataConsistencyTest);
                    }
                }
            }

            this.DeepClearNavigationProperties(entity);
        }

        public void Remove(object entity)
        {
            var entityType = entity.GetType();
            this.GetEntityType(entityType);

            var entityId = EntityTypeManager.GetEntityId(entity);

            this.Remove(entityType, entityId);
        }

        internal void Remove(Type type, int id)
        {
            var entityType = this.GetEntityType(type);

            var requiredDependancies = this.RequiredDependancies.Where(x => x.RequiredType.UniqueName == entityType.GetId()).ToList();

            foreach (var requiredDependancy in requiredDependancies)
            {
                var connectedIds = new List<int>();

                var allConnections = this.Storage.Connections.GetAllConnectionsData();

                //TODO performance, readability
                connectedIds.AddRange(allConnections.Where(x => x.TypeFirst.Equals(requiredDependancy.DependantType) && x.TypeSecond.Equals(requiredDependancy.RequiredType)).SelectMany(x => x).Where(x => x.Item2 == id).Select(x => x.Item1));
                connectedIds.AddRange(allConnections.Where(x => x.TypeFirst.Equals(requiredDependancy.RequiredType) && x.TypeSecond.Equals(requiredDependancy.DependantType)).SelectMany(x => x).Where(x => x.Item1 == id).Select(x => x.Item2));

                foreach (var connectedId in connectedIds)
                {
                    this.Remove(this.Types[requiredDependancy.DependantType.UniqueName].Type, connectedId);
                }
            }

            foreach (var connection in entityType.Connections)
            {
                this.Storage.Connections.RemoveConnectionsFor(entityType, connection.ConnectedType, connection.PropertyName, id);
            }

            this.Storage.Entities.Remove(id, entityType);
        }

        public IQueryable<T> Query<T>()
        {
            return Query<T>(1);
        }

        public IQueryable<T> Query<T>(int dependenciesLevel)
        {
            var list = new List<T>();

            var entiytType = GetEntityType(typeof(T));

            var entities = this.Storage.Entities.GetEntities(entiytType);

            foreach (var entity in entities)
            {
                list.Add((T)entity);

                LoadNavigationProperties(dependenciesLevel - 1, entity);
            }

            return list.AsQueryable();
        }

        public void SaveData()
        {
            PersistenceProvider.SaveContext(Storage, Types);
        }

        public void LoadData()
        {
            try
            {
                PersistenceProvider.LoadContext(this.Storage, Types);
                this.CheckDataConsistency();
            }
            catch (Exception ex)
            {
                this.Storage.Clear();
                throw;
            }
        }

        public bool IsEmpty
        {
            get { return this.Storage.IsEmpty; }
        }

        public void SeedData(SeedDataAction seedDataAction)
        {
            try
            {
                this.DoDataConsistencyTest = false;

                seedDataAction(this);

                this.CheckDataConsistency();

                this.DoDataConsistencyTest = true;
            }
            catch (Exception ex)
            {
                this.Storage.Clear();
                throw;
            }
            finally
            {
                this.DoDataConsistencyTest = true;
            }
        }

        #region Helper functions

        internal EntityTypeInfo GetEntityType(Type type)
        {
            var result = Types.GetType(type);

            Check.NotNull(result, String.Format("Type: {0} is not one of registered entity types", type.GetId()));

            return result;
        }

        private static void AddEntityTypes(Type type, Dictionary<string, Type> typesDict)
        {
            var properties = EntityTypeManager.GetProperties(type);
            foreach (var propertyInfo in properties)
            {
                var entityType = (Type)null;

                var enumerableType = EntityTypeManager.GetEnumerableEntityType(propertyInfo.PropertyType);

                if (enumerableType != null && !EntityTypeManager.IsSimpleType(enumerableType))
                {
                    entityType = enumerableType;
                }
                else if (!EntityTypeManager.IsSimpleType(propertyInfo.PropertyType))
                {
                    entityType = propertyInfo.PropertyType;
                }

                if (entityType != null && !typesDict.ContainsKey(entityType.GetId()))
                {
                    typesDict.AddIfNoEntry(entityType.GetId(), entityType);
                    AddEntityTypes(entityType, typesDict);
                }
            }
        }

        private void LoadNavigationProperties(int dependenciesLevel, object entity)
        {
            var entityType = this.GetEntityType(entity.GetType());
            var entityId = EntityTypeManager.GetEntityId(entity);

            foreach (var propertyInfo in EntityTypeManager.GetProperties(entityType.Type))
            {
                var connection = entityType.Connections.SingleOrDefault(x => x.PropertyName == propertyInfo.Name);

                if (connection == null) continue;

                if (connection.IsMultipleConnection)
                {
                    var connectedEntityType = connection.ConnectedType;
                    var connections = this.Storage.Connections.GetConnectionsFor(entityType, connectedEntityType, connection.ConnectionName, entityId);

                    if (connections.Count > 0)
                    {
                        var newList = EntityTypeManager.CreateGenericList(connectedEntityType.Type);

                        foreach (var connectionId in connections)
                        {
                            var entityToAdd = this.Storage.Entities.GetById(connectionId, connectedEntityType);

                            if (dependenciesLevel > 0)
                            {
                                LoadNavigationProperties(dependenciesLevel - 1, entityToAdd);
                            }

                            newList.Add(entityToAdd);
                        }

                        propertyInfo.SetValue(entity, newList);
                    }
                }
                else
                {
                    var connectedEntityType = connection.ConnectedType;
                    var connections = this.Storage.Connections.GetConnectionsFor(entityType, connectedEntityType, connection.ConnectionName, entityId);

                    Check.That(connections.Count <= 1, "Multiple connections for one to one relation");

                    if (connections.Count == 1)
                    {
                        var connectedId = connections.Single();

                        var connectedEntity = this.Storage.Entities.GetById(connectedId, connectedEntityType);

                        if (dependenciesLevel > 0)
                        {
                            LoadNavigationProperties(dependenciesLevel - 1, connectedEntity);
                        }

                        propertyInfo.SetValue(entity, connectedEntity);
                    }
                    else //no connection, so clear property
                    {
                        propertyInfo.SetValue(entity, null);
                    }
                }
            }
        }

        private void DeepClearNavigationProperties(object entity)
        {
            var entityType = entity.GetType();

            foreach (var propertyInfo in EntityTypeManager.GetProperties(entityType))
            {
                var enumerableType = EntityTypeManager.GetEnumerableEntityType(propertyInfo.PropertyType);

                if (enumerableType != null && this.Types.ContainsKey(enumerableType.GetId()))
                {
                    var connectedCollection = propertyInfo.GetValue(entity) as IEnumerable;

                    if (connectedCollection != null)
                    {
                        foreach (var collectionItem in connectedCollection)
                        {
                            DeepClearNavigationProperties(collectionItem);
                        }
                        propertyInfo.SetValue(entity, null);
                    }
                }
                else if (!EntityTypeManager.IsSimpleType(propertyInfo.PropertyType))
                {
                    var connectedProperty = propertyInfo.GetValue(entity);

                    if (connectedProperty != null)
                    {
                        DeepClearNavigationProperties(connectedProperty);
                        propertyInfo.SetValue(entity, null);
                    }

                }
            }
        }

        public string ToDisplayString()
        {
            var result = new StringBuilder();

            var types = this.Types.Values.ToList();

            var allConnections = this.Storage.Connections.GetAllConnectionsData();

            foreach (var entityType in types)
            {
                var entities = this.Storage.Entities.GetEntities(entityType);

                result.AppendLine(String.Format("{0}:", entityType.GetId()));

                result.AppendLine("");

                result.Append(GlobalConstants.SpecialSymbols.Tab);
                result.AppendLine("Values:");

                foreach (var entity in entities)
                {
                    result.Append(GlobalConstants.SpecialSymbols.Tab);
                    result.AppendLine(GlobalConstants.SpecialSymbols.Tab + EntityToDisplayString(entity));
                }

                result.AppendLine("");

                result.Append(GlobalConstants.SpecialSymbols.Tab);
                result.AppendLine("Connections:");

                var connectionsGroupedByType = new Dictionary<string, List<Tuple<int, int>>>();

                //                foreach (var entityConnection in allConnections.ToList())
                //                {
                //                    if (entityConnection.TypeFirst.Equals(entityType) || entityConnection.TypeSecond.Equals(entityType))
                //                    {
                //                        var firstIsEntity = entityConnection.TypeFirst.Equals(entityType);
                //
                //                        var connectedType = firstIsEntity ? entityConnection.TypeSecond : entityConnection.TypeFirst;
                //                        var entityId = firstIsEntity ? entityConnection.IdFirst : entityConnection.IdSecond;
                //                        var connectedEntityId = firstIsEntity ? entityConnection.IdSecond : entityConnection.IdFirst;
                //
                //                        connectionsGroupedByType.AddIfNoEntry(connectedType, new List<Tuple<int, int>>());
                //
                //                        connectionsGroupedByType[connectedType].Add(new Tuple<int, int>(entityId, connectedEntityId));
                //                    }
                //                }

                foreach (var connectedType in connectionsGroupedByType)
                {
                    result.AppendLine("");

                    result.Append(GlobalConstants.SpecialSymbols.Tab);
                    result.Append(GlobalConstants.SpecialSymbols.Tab);
                    result.AppendLine(String.Format("To {0}:", connectedType.Key));

                    foreach (var connectionIds in connectedType.Value)
                    {
                        result.Append(GlobalConstants.SpecialSymbols.Tab);
                        result.Append(GlobalConstants.SpecialSymbols.Tab);
                        result.AppendLine(String.Format("{0}-{1}", connectionIds.Item1, connectionIds.Item2));
                    }
                }

                result.AppendLine("");
            }


            return result.ToString();
        }

        private string EntityToDisplayString(object entity)
        {
            var result = new StringBuilder();

            var props = EntityTypeManager.GetSimpleWritableProperties(entity.GetType());

            foreach (var propertyInfo in props)
            {
                var value = propertyInfo.GetValue(entity);
                var name = propertyInfo.Name;

                result.Append(String.Format("{0} = {1}; ", name, value));
            }

            return result.ToString();
        }

        private void CheckDataConsistency()
        {
            //TODO implement
        }

        public void RegisterEntityTypes(Type containerType)
        {
            var typesToRegister = new Dictionary<string, Type>();

            var stubSetProperties = EntityTypeManager.GetProperties(containerType).Where(x => EntityTypeManager.IsStubSet(x.PropertyType)).ToList();

            foreach (var stubSetProperty in stubSetProperties)
            {
                var typeOfStubSet = stubSetProperty.PropertyType.GetGenericArguments().First();
                typesToRegister.AddIfNoEntry(typeOfStubSet.GetId(), typeOfStubSet);
                AddEntityTypes(typeOfStubSet, typesToRegister);
            }

            this.Types = new EntityTypeCollection();

            foreach (var keyValuePair in typesToRegister)
            {
                this.Types.Add(keyValuePair.Value);
            }

            foreach (var type in typesToRegister.Values)
            {
                var typeInfo = this.Types.GetType(type);
                this.Types.LoadConnections(typeInfo);
            }
        }

        #endregion
    }
}
