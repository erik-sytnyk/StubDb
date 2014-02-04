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
            var entityType = this.GetEntityType(entity.GetType());

            var id = entityType.GetEntityId(entity);
            
            if (id != 0)
            {
                entityType.SetEntityId(entity, 0);
            }

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

            var entityId = entityType.GetEntityId(entity);

            var existingEntity = this.Storage.Entities.GetById(entityId, entityType);
            var isExistingEntity = existingEntity != null;
            if (!isExistingEntity)
            {
                var newId = this.Storage.Entities.GetAvailableIdForEntityType(entityType);

                entityType.SetEntityId(entity, newId);

                entityId = newId;
            }

            var entityToSave = EntityTypeManager.CloneObject(entity);
            ClearNavigationProperties(entityToSave);
            if (!isExistingEntity)
            {
                this.Storage.Entities.Add(entityId, entityToSave);
            }
            else
            {
                this.Storage.Entities.Update(entityId, entityToSave);
            }

            //load connections
            foreach (var propertyInfo in properties)
            {
                var connection = entityType.Connections.SingleOrDefault(x => x.PropertyName == propertyInfo.Name);

                if (connection == null) continue;

                var connectedEntities = new List<object>();

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
                        connectedEntities.Add(item);
                    }
                }
                else
                {
                    var connectedEntity = propertyInfo.GetValue(entity);

                    if (connectedEntity != null)
                    {
                        connectedEntities.Add(connectedEntity);
                    }
                }

                var connectedType = connection.ConnectedType;

                if (isExistingEntity)
                {
                    this.Storage.Connections.RemoveConnectionsFor(entityType, connectedType, connection.ConnectionName, entityId);
                }

                if (connectedEntities.Count > 0)
                {
                    foreach (var connectedEntity in connectedEntities)
                    {
                        var connectedEntityId = connectedType.GetEntityId(connectedEntity);
                        var existingConnectedEntity = this.Storage.Entities.GetById(connectedEntityId, connectedType);
                        if (existingConnectedEntity == null) //do not save existing connected entities
                        {
                            this.Save(connectedEntity);   
                        }
                    }

                    foreach (var connectedEntity in connectedEntities)
                    {
                        this.Storage.Connections.AddConnection(entityType, connectedType, connection.ConnectionName, entityType.GetEntityId(entity), connectedType.GetEntityId(connectedEntity), DoDataConsistencyTest);
                    }
                }
            }

            this.ClearNavigationProperties(entityToSave);
        }

        public void Remove(object entity)
        {
            var entityType = this.GetEntityType(entity.GetType());
            
            var entityId = entityType.GetEntityId(entity);

            this.Remove(entityType.Type, entityId);
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
                connectedIds.AddRange(allConnections.Where(x => x.TypeFirst == requiredDependancy.DependantType && x.TypeSecond == requiredDependancy.RequiredType).SelectMany(x => x).Where(x => x.Item2 == id).Select(x => x.Item1));
                connectedIds.AddRange(allConnections.Where(x => x.TypeFirst == requiredDependancy.RequiredType && x.TypeSecond == requiredDependancy.DependantType).SelectMany(x => x).Where(x => x.Item1 == id).Select(x => x.Item2));

                foreach (var connectedId in connectedIds)
                {
                    this.Remove(this.Types[requiredDependancy.DependantType.UniqueName].Type, connectedId);
                }
            }

            foreach (var connection in entityType.Connections)
            {
                this.Storage.Connections.RemoveConnectionsFor(entityType, connection.ConnectedType, connection.ConnectionName, id);
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

            this.UpdateConnections(this.Types);
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

                var enumerableEntityType = EntityTypeManager.GetEnumerableEntityType(propertyInfo.PropertyType);

                if (enumerableEntityType != null)
                {
                    entityType = enumerableEntityType;
                }
                else if (!EntityTypeManager.IsSimpleOrSimpleEnumerableType(propertyInfo.PropertyType))
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
            var entityId = entityType.GetEntityId(entity);

            foreach (var propertyInfo in EntityTypeManager.GetProperties(entityType.Type))
            {
                var connection = entityType.Connections.SingleOrDefault(x => x.PropertyName == propertyInfo.Name);

                if (connection == null) continue;

                if (connection.IsMultipleConnection)
                {
                    var connectedEntityType = connection.ConnectedType;
                    var connections = this.Storage.Connections.GetConnectionsFor(entityType, connectedEntityType, connection.ConnectionName, entityId);

                    var newList = EntityTypeManager.CreateGenericList(connectedEntityType.Type);

                    if (connections.Count > 0)
                    {
                        foreach (var connectionId in connections)
                        {
                            var entityToAdd = this.Storage.Entities.GetById(connectionId, connectedEntityType);

                            Check.NotNull(entityToAdd, String.Format("Cannot find entity of type {0} with ID equals to {1}.", connectedEntityType.Type.FullName, connectionId));

                            if (dependenciesLevel > 0)
                            {
                                LoadNavigationProperties(dependenciesLevel - 1, entityToAdd);
                            }

                            newList.Add(entityToAdd);
                        }                        
                    }

                    propertyInfo.SetValue(entity, newList);
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

                        Check.NotNull(connectedEntity, String.Format("Cannot find entity of type {0} with ID equals to {1}.", connectedEntityType.Type.FullName, connectedId));

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

        private void ClearNavigationProperties(object entity)
        {
            var entityType = this.GetEntityType(entity.GetType());

            foreach (var propertyInfo in EntityTypeManager.GetProperties(entityType.Type))
            {
                var connection = entityType.Connections.SingleOrDefault(x => x.PropertyName == propertyInfo.Name);

                if (connection == null) continue;

                propertyInfo.SetValue(entity, null);
            }
        }

        public string ToDisplayString()
        {
            var result = new StringBuilder();

            var types = this.Types.Values.ToList();

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

                var connectionsData = this.Storage.Connections.GetAllConnectionsData();
                var connectionsGroupedByType = new Dictionary<string, List<Tuple<int, int>>>();

                foreach (var entityConnection in connectionsData.ToList())
                {
                    if (entityConnection.TypeFirst == entityType || entityConnection.TypeSecond == entityType)
                    {
                        var firstIsEntity = entityConnection.TypeFirst == entityType;

                        var connectedType = firstIsEntity ? entityConnection.TypeSecond : entityConnection.TypeFirst;
                        foreach (var connection in entityConnection)
                        {
                            var entityId = firstIsEntity ? connection.Item1 : connection.Item2;
                            var connectedEntityId = firstIsEntity ? connection.Item2 : connection.Item1;

                            connectionsGroupedByType.AddIfNoEntry(connectedType.UniqueName, new List<Tuple<int, int>>());

                            connectionsGroupedByType[connectedType.UniqueName].Add(new Tuple<int, int>(entityId, connectedEntityId));
                        }

                    }
                }

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

        private void UpdateConnections(EntityTypeCollection types)
        {
            foreach (var type in types)
            {
                var typeConnections = type.Value.Connections.ToList();
                foreach (var connection in typeConnections)
                {
                    var connectionToCurrentTypeFromReferencingType =
                        connection.ConnectedType.Connections.Where(x => x.ConnectedType == type.Value);

                    if (connection.IsNamedConnection)
                    {
                        //there should be no navigation property referencing current type from connected type
                        var excpetionMessage =
                            String.Format(
                                "There are a few properties of type {0} referencing type {1}. In this case type {1} should not have references to type {0}",
                                type.Value.Type.Name, connection.ConnectedType.Type.Name);

                        Check.That(!connectionToCurrentTypeFromReferencingType.Any(), excpetionMessage);
                    }
                    else
                    {
                        if (!connectionToCurrentTypeFromReferencingType.Any())
                        {
                            var missingBackwardConnection = new EntityConnectionInfo();

                            missingBackwardConnection.ConnectedType = type.Value;
                            missingBackwardConnection.IsMultipleConnection = false;
                            missingBackwardConnection.IsNamedConnection = false;
                            missingBackwardConnection.PropertyName = null;

                            connection.ConnectedType.Connections.Add(missingBackwardConnection);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
