using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
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
            this.ConfigureModel();

            var stubSets = this.GetStubSetProperties();

            foreach (var propertyInfo in stubSets)
            {
                var stubSet = EntityTypeManager.CreateNew(propertyInfo.PropertyType);
                stubSet.SetProperty("Context", this);
                propertyInfo.SetValue(this, stubSet);
            }

            ModelBuilder.BeforeRegisteringTypes();

            RegisterEntityTypes(this.GetType(), ModelBuilder.IgnoredTypes);

            ModelBuilder.AfterRegisteringTypes();
        }

        public virtual void ConfigureModel()
        {

        }

        public StubSet<TEntity> GetStubSet<TEntity>()
        {
            var result = (StubSet<TEntity>)null;

            var property = this.GetStubSetProperties().SingleOrDefault(x => x.PropertyType.GenericTypeArguments.First() == typeof(TEntity));

            if (property != null)
            {
                result = (StubSet<TEntity>)property.GetValue(this);
            }

            return result;
        }

        public void Add(object entity)
        {
            var entityType = this.GetEntityType(entity.GetType());

            //set default id value to mark entity as new
            entityType.SetEntityId(entity, 0);

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
                var idType = entityType;

                if (entityType.BaseEntityType != null)
                {
                    idType = entityType.BaseEntityType;
                }

                var newId = this.Storage.Entities.GetAvailableIdForEntityType(idType);

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
                var connection = entityType.Connections.SingleOrDefault(x => x.NavigationPropertyName == propertyInfo.Name);

                if (connection == null) continue;

                var connectedEntities = new List<object>();

                var connectedType = connection.ConnectedType;

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

                if (connection.IsSingleConnection)
                {
                    var connectedEntity = propertyInfo.GetValue(entity);

                    if (connectedEntity != null)
                    {
                        connectedEntities.Add(connectedEntity);
                    }
                    else if (connection.HasNavigationIdProperty)
                    {
                        var connectedEntityId = connection.GetNavigationIdProperty(entity);
                        var existingConnectedEntity = this.Storage.Entities.GetById(connectedEntityId, connectedType);
                        if (existingConnectedEntity != null)
                        {
                            connectedEntities.Add(existingConnectedEntity);
                        }
                    }
                }

                if (isExistingEntity)
                {
                    this.Storage.Connections.RemoveConnectionsFor(entityType, connectedType, connection.ConnectionName, entityId);
                }

                if (connectedEntities.Any())
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
                        var connectionType = connectedType;

                        if (connectedType.DerivedTypes.Any())
                        {
                            connectionType = this.GetEntityType(connectedEntity.GetType());
                        }

                        this.Storage.Connections.AddConnection(entityType, connectionType, connection.ConnectionName, entityType.GetEntityId(entity), connectionType.GetEntityId(connectedEntity), DoDataConsistencyTest);
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

            this.Storage.Connections.RemoveConnectionsFor(entityType, id);

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

            foreach (var derivedType in entiytType.DerivedTypes)
            {
                var derivedEntities = this.Storage.Entities.GetEntities(derivedType);
                entities.AddRange(derivedEntities);
            }

            foreach (var entity in entities)
            {
                list.Add((T)entity);

                LoadNavigationProperties(dependenciesLevel - 1, entity);
            }

            return list.AsQueryable();
        }

        public void SaveData()
        {
            Check.NotNull(this.PersistenceProvider, "Persistence provider is not initialized");
            PersistenceProvider.SaveContext(Storage, Types);
        }

        public void LoadData()
        {
            try
            {
                Check.NotNull(this.PersistenceProvider, "Persistence provider is not initialized");
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

        public void RegisterEntityTypes(Type containerType, List<IgnoredTypeInfo> ignoredTypes)
        {
            var typesToRegister = new Dictionary<string, Type>();

            var stubSetProperties = GetStubSetProperties();

            foreach (var stubSetProperty in stubSetProperties)
            {
                var typeOfStubSet = stubSetProperty.PropertyType.GetGenericArguments().First();
                typesToRegister.AddIfNoEntry(typeOfStubSet.GetId(), typeOfStubSet);
                AddEntityTypes(typeOfStubSet, typesToRegister, ignoredTypes);
            }

            this.Types = new EntityTypeCollection();
            this.Types.IgnoredTypes = ignoredTypes;

            foreach (var keyValuePair in typesToRegister)
            {
                this.Types.Add(keyValuePair.Value);
            }

            foreach (var type in typesToRegister.Values)
            {
                var typeInfo = this.Types.GetType(type);
                this.Types.LoadConnections(typeInfo);
            }

            foreach (var type in this.Types)
            {
                var derivedTypes = this.Types.Where(x => x.Value.Type.IsSubclassOf(type.Value.Type)).Select(x => x.Value).ToList();

                var baseTypes = this.Types.Where(x => type.Value.Type.IsSubclassOf(x.Value.Type)).Select(x => x.Value).ToList();
                Check.That(baseTypes.Count() <= 1, "Currently StubDb does not support more than one level of inheritance for registered Entity Types");

                type.Value.DerivedTypes = derivedTypes;
                type.Value.BaseEntityType = baseTypes.FirstOrDefault();
            }
        }

        #region Helper functions

        internal EntityTypeInfo GetEntityType(Type type)
        {
            var result = Types.GetType(type);

            Check.NotNull(result, String.Format("Type: {0} is not one of registered entity types", type.GetId()));

            return result;
        }

        private void AddEntityTypes(Type type, Dictionary<string, Type> typesDict, List<IgnoredTypeInfo> typesToIgnore)
        {
            var properties = EntityTypeManager.GetProperties(type);

            foreach (var propertyInfo in properties)
            {
                var entityType = (Type)null;

                var enumerableType = EntityTypeManager.GetEnumerableType(propertyInfo.PropertyType);

                if (enumerableType != null)
                {
                    if (this.Types.IsEntityType(enumerableType))
                    {
                        entityType = enumerableType;
                    }
                }
                else if (!EntityTypeManager.IsSimpleOrSimpleEnumerableType(propertyInfo.PropertyType))
                {
                    entityType = propertyInfo.PropertyType;
                }

                if (entityType != null && !typesDict.ContainsKey(entityType.GetId()) && typesToIgnore.All(x => x.Type != entityType))
                {
                    typesDict.AddIfNoEntry(entityType.GetId(), entityType);
                    AddEntityTypes(entityType, typesDict, typesToIgnore);
                }
            }
        }

        private void LoadNavigationProperties(int dependenciesLevel, object entity)
        {
            var entityType = this.GetEntityType(entity.GetType());
            var entityId = entityType.GetEntityId(entity);

            foreach (var propertyInfo in entityType.Type.GetProperties())
            {
                var ignoredType = this.Types.GetIgnoredTypeInfo(propertyInfo.PropertyType);

                if (ignoredType != null)
                {
                    if (ignoredType.Persist)
                    {
                        var clone = EntityTypeManager.DeepCloneObject(propertyInfo.GetValue(entity));
                        propertyInfo.SetValue(entity, clone);   
                    }
                    else
                    {
                        propertyInfo.SetValue(entity, null);
                    }
                    continue;
                }

                var connection = entityType.Connections.SingleOrDefault(x => x.NavigationPropertyName == propertyInfo.Name);

                if (connection == null) continue;

                var connectedEntityType = connection.ConnectedType;

                var connectedTypes = new List<EntityTypeInfo> { connectedEntityType };
                connectedTypes.AddRange(connectedEntityType.DerivedTypes);

                if (connection.IsMultipleConnection)
                {
                    var newList = EntityTypeManager.CreateGenericList(connectedEntityType.Type);

                    foreach (var connectedType in connectedTypes)
                    {
                        var connectionIds = this.Storage.Connections.GetConnectionsFor(entityType, connectedType, connection.ConnectionName, entityId);

                        foreach (var connectionId in connectionIds)
                        {
                            var entityToAdd = this.Storage.Entities.GetById(connectionId, connectedType);

                            Check.NotNull(entityToAdd, String.Format("Cannot find entity of type {0} with ID equals to {1}.", connectedType.Type.FullName, connectionId));

                            if (dependenciesLevel > 0)
                            {
                                LoadNavigationProperties(dependenciesLevel - 1, entityToAdd);
                            }

                            newList.Add(entityToAdd);
                        }
                    }

                    propertyInfo.SetValue(entity, newList);
                }

                if (!connection.IsMultipleConnection)
                {
                    //clear property
                    propertyInfo.SetValue(entity, null);

                    foreach (var connectedType in connectedTypes)
                    {
                        var connectionIds = this.Storage.Connections.GetConnectionsFor(entityType, connectedType, connection.ConnectionName, entityId);

                        Check.That(connectionIds.Count <= 1, "Multiple connections for one to one relation");

                        if (connectionIds.Count == 1)
                        {
                            var connectedId = connectionIds.Single();

                            var connectedEntity = this.Storage.Entities.GetById(connectedId, connectedType);

                            Check.NotNull(connectedEntity, String.Format("Cannot find entity of type {0} with ID equals to {1}.", connectedType.Type.FullName, connectedId));

                            if (dependenciesLevel > 0)
                            {
                                LoadNavigationProperties(dependenciesLevel - 1, connectedEntity);
                            }

                            propertyInfo.SetValue(entity, connectedEntity);

                            if (connection.HasNavigationIdProperty)
                            {
                                connection.SetNavigationIdProperty(entity, connectedId);
                            }
                        }
                    }
                }
            }
        }

        private void ClearNavigationProperties(object entity)
        {
            var entityType = this.GetEntityType(entity.GetType());

            foreach (var propertyInfo in entityType.Type.GetProperties())
            {
                var connection = entityType.Connections.SingleOrDefault(x => x.NavigationPropertyName == propertyInfo.Name);

                if (connection == null) continue;

                propertyInfo.SetValue(entity, null);

                if (connection.HasNavigationIdProperty)
                {
                    connection.ClearNavigationIdProperty(entity);
                }
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
                    result.AppendLine(GlobalConstants.SpecialSymbols.Tab + EntityToDisplayString(entityType, entity));
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

        private string EntityToDisplayString(EntityTypeInfo entityType, object entity)
        {
            var result = new StringBuilder();

            var props = entityType.GetSimpleWritableProperties();

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

        private IEnumerable<PropertyInfo> GetStubSetProperties()
        {
            return EntityTypeManager.GetProperties(this.GetType()).Where(x => EntityTypeManager.IsStubSet(x.PropertyType)).ToList();
        }

        #endregion
    }
}
