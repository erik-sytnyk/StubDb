﻿using System;
using System.Collections;
using System.Collections.Generic;
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

        #endregion

        internal ContextStorage Storage = new ContextStorage();
        internal Dictionary<string, Type> Types { get; set; }
        internal List<RequiredDependancy> RequiredDependancies { get; set; }
        protected ModelBuilder ModelBuilder { get; set; }
        public IContextStoragePersistenceProvider PersistenceProvider { get; set; }

        public StubContext()
        {
            Types = new Dictionary<string, Type>();
            RequiredDependancies = new List<RequiredDependancy>();

            ModelBuilder = new ModelBuilder(this);

            var stubSets = EntityTypeManager.GetProperties(this.GetType()).Where(x => EntityTypeManager.IsStubSet(x.PropertyType)).ToList();

            foreach (var propertyInfo in stubSets)
            {
                var stubSet = EntityTypeManager.CreateNew(propertyInfo.PropertyType);
                stubSet.SetProperty("Context", this);
                propertyInfo.SetValue(this, stubSet);
            }

            var types = GetEntityTypes(this.GetType());
            foreach (var type in types)
            {
                this.RegisterType(type);
            }

            this.ConfigureModel();

            PersistenceProvider = new SerializeToFilePersistenceProvider();
        }

        public virtual void ConfigureModel()
        {

        }

        public StubSet<TEntity> GetStubSet<TEntity>()
        {
            var result = (StubSet<TEntity>)null;

            var property = EntityTypeManager.GetProperties(this.GetType()).SingleOrDefault(x => EntityTypeManager.IsStubSet(x.PropertyType) && EntityTypeManager.GetEnumerableType(x.PropertyType) == typeof(TEntity));

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
            var entityType = entity.GetType();
            this.CheckIsEntityType(entityType);

            var properties = EntityTypeManager.GetProperties(entityType);

            var entityId = EntityTypeManager.GetEntityId(entity);

            var existingEntity = this.Storage.Entities.GetById(entityId, entityType);
            var isExistingEntity = existingEntity != null;

            if (!isExistingEntity)
            {
                var newId = this.Storage.Entities.GetAvailableIdForEntityType(entityType);

                EntityTypeManager.SetEntityId(entity, newId);

                this.Storage.Entities.Add(newId, entity);
            }

            foreach (var propertyInfo in properties)
            {
                var entitiesToAdd = new List<object>();

                var enumerableType = EntityTypeManager.GetEnumerableType(propertyInfo.PropertyType);

                if (enumerableType != null && this.Types.ContainsKey(enumerableType.GetId()))
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
                else if (Types.ContainsKey(propertyInfo.PropertyType.GetId()))
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

                    if (this.Types.ContainsKey(connectedType.GetId()))
                    {
                        foreach (var entityToAdd in entitiesToAdd)
                        {
                            this.Save(entityToAdd);
                        }
                    }

                    this.Storage.Connections.RemoveConnectionsFor(entityType, entityId, connectedType);

                    foreach (var entityToAdd in entitiesToAdd)
                    {
                        this.Storage.Connections.AddConnection(entityType, entityToAdd.GetType(), EntityTypeManager.GetEntityId(entity), EntityTypeManager.GetEntityId(entityToAdd));
                    }
                }
            }

            this.DeepClearNavigationProperties(entity);
        }

        public void Remove(object entity)
        {
            var entityType = entity.GetType();
            this.CheckIsEntityType(entityType);

            var entityId = EntityTypeManager.GetEntityId(entity);

            this.Remove(entityType, entityId);
        }

        internal void Remove(Type entityType, int id)
        {
            var requiredDependancies = this.RequiredDependancies.Where(x => x.RequiredType == entityType.GetId()).ToList();

            foreach (var requiredDependancy in requiredDependancies)
            {
                var connectedIds = new List<int>();

                var allConnections = this.Storage.Connections.GetAllConnections().ToList();

                connectedIds.AddRange(allConnections.Where(x => x.TypeFirst == requiredDependancy.DependantType && x.TypeSecond == requiredDependancy.RequiredType && x.IdSecond == id).Select(y => y.IdFirst));
                connectedIds.AddRange(allConnections.Where(x => x.TypeFirst == requiredDependancy.RequiredType && x.TypeSecond == requiredDependancy.DependantType && x.IdFirst == id).Select(y => y.IdSecond));

                foreach (var connectedId in connectedIds)
                {
                    this.Remove(this.Types[requiredDependancy.DependantType], connectedId);
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

            var entities = this.Storage.Entities.GetEntities(typeof(T));

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
            PersistenceProvider.LoadContext(this.Storage, Types);
        }

        public bool IsEmpty
        {
            get { return this.Storage.IsEmpty; }
        }

        public static List<Type> GetEntityTypes(Type containerType)
        {
            var result = new Dictionary<string, Type>();

            var stubSetProperties = EntityTypeManager.GetProperties(containerType).Where(x => EntityTypeManager.IsStubSet(x.PropertyType)).ToList();

            foreach (var stubSetProperty in stubSetProperties)
            {
                var typeOfStubSet = stubSetProperty.PropertyType.GetGenericArguments().First();
                result.AddIfNoEntry(typeOfStubSet.GetId(), typeOfStubSet);
                AddEntityTypes(typeOfStubSet, result);
            }

            return result.Values.ToList();
        }

        #region Helper functions

        public void RegisterType(Type type)
        {
            this.Types.Add(type.GetId(), type);
        }

        internal void CheckIsEntityType(Type type)
        {
            Check.That(Types.ContainsKey(type.GetId()), String.Format("Type: {0} is not one of registered entity types", type.GetId()));
        }

        private static void AddEntityTypes(Type type, Dictionary<string, Type> typesDict)
        {
            var properties = EntityTypeManager.GetProperties(type);
            foreach (var propertyInfo in properties)
            {
                var entityType = (Type)null;

                var enumerableType = EntityTypeManager.GetEnumerableType(propertyInfo.PropertyType);

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
            var entityId = EntityTypeManager.GetEntityId(entity);
            var entityType = entity.GetType();

            foreach (var propertyInfo in EntityTypeManager.GetProperties(entityType))
            {
                var enumerableType = EntityTypeManager.GetEnumerableType(propertyInfo.PropertyType);

                if (enumerableType != null && this.Types.ContainsKey(enumerableType.GetId()))
                {
                    var connectedEntityType = enumerableType;
                    var connections = this.Storage.Connections.GetConnectionsFor(entityType, entityId, connectedEntityType);

                    if (connections.Count > 0)
                    {
                        var newList = EntityTypeManager.CreateGenericList(connectedEntityType);

                        foreach (var entityConnection in connections)
                        {
                            var connectedId = entityConnection.GetIdByType(connectedEntityType);

                            var entityToAdd = this.Storage.Entities.GetById(connectedId, connectedEntityType);

                            if (dependenciesLevel > 0)
                            {
                                LoadNavigationProperties(dependenciesLevel - 1, entityToAdd);
                            }

                            newList.Add(entityToAdd);
                        }

                        propertyInfo.SetValue(entity, newList);
                    }
                }
                else if (!EntityTypeManager.IsSimpleType(propertyInfo.PropertyType))
                {
                    var connectedEntityType = propertyInfo.PropertyType;
                    var connections = this.Storage.Connections.GetConnectionsFor(entityType, entityId, connectedEntityType);

                    Check.That(connections.Count <= 1, "Multiple connections for one to one relation");

                    if (connections.Count == 1)
                    {
                        var entityConnection = connections.Single();

                        var connectedId = entityConnection.GetIdByType(connectedEntityType);

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
                var enumerableType = EntityTypeManager.GetEnumerableType(propertyInfo.PropertyType);

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

            var allConnections = this.Storage.Connections.GetAllConnections();

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

                foreach (var entityConnection in allConnections.ToList())
                {
                    if (entityConnection.TypeFirst == entityType.GetId() || entityConnection.TypeSecond == entityType.GetId())
                    {
                        var firstIsEntity = entityConnection.TypeFirst == entityType.GetId();

                        var connectedType = firstIsEntity ? entityConnection.TypeSecond : entityConnection.TypeFirst;
                        var entityId = firstIsEntity ? entityConnection.IdFirst : entityConnection.IdSecond;
                        var connectedEntityId = firstIsEntity ? entityConnection.IdSecond : entityConnection.IdFirst;

                        connectionsGroupedByType.AddIfNoEntry(connectedType, new List<Tuple<int, int>>());

                        connectionsGroupedByType[connectedType].Add(new Tuple<int, int>(entityId, connectedEntityId));
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

        #endregion
    }
}
