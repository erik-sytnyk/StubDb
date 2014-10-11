﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Ext.Core;
using Newtonsoft.Json;
using StubDb.ModelStorage;
using StubDb.Persistence;

namespace StubDb.Store
{
    public class SerializeToFilePersistenceProvider : IContextStoragePersistenceProvider
    {
        #region Nested Classes

        public class DataContainer
        {
            public const string SeparatorString = "ߊ";
            public const string SeparatorConnectionString = "-";


            public List<EntityContainer> Entities { get; set; }
            public List<ConnectionContainer> Connections { get; set; }

            public DataContainer()
            {
                Entities = new List<EntityContainer>();
                Connections = new List<ConnectionContainer>();
            }

            public DataContainer(ContextStorage storage, EntityTypeCollection types)
                : this()
            {
                foreach (var entityType in types.Values)
                {
                    var entityContainer = new EntityContainer();

                    entityContainer.TypeName = entityType.GetId();

                    var simpleProperties = this.GetSimpleProperties(entityType).ToList();

                    entityContainer.Properties = this.GetPropertiesString(simpleProperties);

                    var entities = storage.Entities.GetEntities(entityType);

                    foreach (var entity in entities)
                    {
                        var valueString = this.GetValuesString(entity, simpleProperties);
                        entityContainer.ValuesList.Add(valueString);
                    }

                    this.Entities.Add(entityContainer);

                }

                var connectionsData = storage.Connections.GetAllConnectionsData();

                foreach (var connectionData in connectionsData)
                {
                    var connectionContainer = new ConnectionContainer();

                    connectionContainer.FirstType = connectionData.TypeFirst.UniqueName;
                    connectionContainer.SecondType = connectionData.TypeSecond.UniqueName;
                    connectionContainer.ConnectionName = connectionData.ConnectionName;

                    var connectionsString = new StringBuilder();

                    foreach (var singleConnection in connectionData)
                    {
                        connectionsString.Append(singleConnection.Item1);
                        connectionsString.Append(SeparatorConnectionString);
                        connectionsString.Append(singleConnection.Item2);
                        connectionsString.Append(SeparatorString);
                    }

                    connectionsString.Remove(connectionsString.Length - 1, 1); //remove last separator symbol

                    connectionContainer.ConnectionsString = connectionsString.ToString();

                    this.Connections.Add(connectionContainer);
                }
            }

            public void LoadContextStorage(ContextStorage storage, EntityTypeCollection types)
            {
                storage.Clear();

                try
                {
                    TryLoadContextStorage(storage, types);
                }
                catch (Exception)
                {
                    storage.Clear();
                    throw;
                }
            }

            private void TryLoadContextStorage(ContextStorage storage, EntityTypeCollection types)
            {
                foreach (var entityContainer in this.Entities)
                {
                    var type = types.GetTypeByName(entityContainer.TypeName);

                    if (type != null)
                    {
                        var simpleProperties = GetSimpleProperties(type).ToList();
                        var retrievedPropertyNames = entityContainer.Properties.Split(new string[] { SeparatorString }, StringSplitOptions.RemoveEmptyEntries);

                        var map = new Dictionary<int, PropertyInfo>();

                        foreach (var retreivedPropertyName in retrievedPropertyNames)
                        {
                            var typeProperty = simpleProperties.FirstOrDefault(x => x.Name == retreivedPropertyName);

                            if (typeProperty != null)
                            {
                                map.Add(retrievedPropertyNames.ToList().IndexOf(retreivedPropertyName), typeProperty);
                            }
                        }

                        foreach (var valuesString in entityContainer.ValuesList)
                        {
                            var values = valuesString.Split(new string[] { SeparatorString }, StringSplitOptions.None);

                            var entity = EntityTypeManager.CreateNew(type.Type);

                            int entityId = -1;

                            foreach (var mapping in map)
                            {
                                var value = this.ConvertToSimpleType(mapping.Value.PropertyType, values[mapping.Key]);
                                mapping.Value.SetValue(entity, value);

                                if (mapping.Value.Name.ToLower() == "id") //TODO
                                {
                                    entityId = (int)value;
                                }
                            }

                            storage.Entities.Add(entityId, entity);
                        }
                    }
                }

                foreach (var connectionContainer in this.Connections)
                {
                    var firstType = types.GetTypeByName(connectionContainer.FirstType);
                    var secondType = types.GetTypeByName(connectionContainer.SecondType);

                    if (firstType != null && secondType != null)
                    {
                        var connections = connectionContainer.ConnectionsString.Split(new string[] { SeparatorString }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var connection in connections)
                        {
                            var ids = connection.Split(new string[] { SeparatorConnectionString }, StringSplitOptions.RemoveEmptyEntries);
                            storage.Connections.AddConnection(firstType, secondType, connectionContainer.ConnectionName, Convert.ToInt32(ids[0]), Convert.ToInt32(ids[1]), false);
                        }
                    }
                }
            }

            private object ConvertToSimpleType(Type type, string value)
            {
                if (type.IsEnum)
                {
                    return Enum.Parse(type, value);
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
                {
                    if (String.IsNullOrEmpty(value))
                    {
                        return null;
                    }
                    else
                    {
                        var nullableType = type.GetGenericArguments()[0];
                        return Convert.ChangeType(value, nullableType);                        
                    }
                }

                return Convert.ChangeType(value, type);
            }

            private string GetValuesString(object entity, IEnumerable<PropertyInfo> properties)
            {
                return String.Join(SeparatorString, properties.Select(x => x.GetValue(entity)));
            }

            private IEnumerable<PropertyInfo> GetSimpleProperties(EntityTypeInfo entityType)
            {
                return entityType.GetSimpleWritableProperties().OrderBy(x => x.Name);
            }

            private string GetPropertiesString(IEnumerable<PropertyInfo> properties)
            {
                return String.Join(SeparatorString, properties.Select(x => x.Name));
            }

        }

        public class EntityContainer
        {
            public string TypeName { get; set; }
            public string Properties { get; set; }
            public List<string> ValuesList { get; set; }

            public EntityContainer()
            {
                ValuesList = new List<string>();
            }
        }

        public class ConnectionContainer
        {
            public string FirstType { get; set; }
            public string SecondType { get; set; }
            public string ConnectionName { get; set; }
            public string ConnectionsString { get; set; }
        }

        #endregion

        private const string DefaultDbFileName = @"StubDb.data";
        private const int NumberOfTries = 60;
        private const int TimeIntervalBetweenTries = 1000;

        private string DbFilePath { get; set; }

        public SerializeToFilePersistenceProvider()
            : this(DefaultDbFileName)
        {
        }

        public SerializeToFilePersistenceProvider(string dbFilePath)
        {
            DbFilePath = dbFilePath;
        }

        public void SaveContext(StubContext context)
        {
            var data = new DataContainer(context.Storage, context.Types);

            string json = JsonConvert.SerializeObject(data);

            try
            {
                using (var writer = new StreamWriter(DbFilePath, false))
                {
                    writer.WriteLine(json);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(String.Format("Error saving context to file {0}", DbFilePath), ex);
            }
        }

        public void LoadContext(StubContext context)
        {
            if (!File.Exists(DbFilePath)) return;

            var tries = 0;
            
            bool successfulExecution = false;
            
            while (!successfulExecution)
            {
                tries++;
                try
                {
                    using (var reader = new StreamReader(DbFilePath, true))
                    {
                        string json = reader.ReadToEnd();
                        var dataContainer = JsonConvert.DeserializeObject<DataContainer>(json);
                        dataContainer.LoadContextStorage(context.Storage, context.Types);
                    }
                    successfulExecution = true;
                }
                catch (Exception ex)
                {
                    if (IsFileLocked(ex))
                    {
                        Check.That(tries <= NumberOfTries, "Stub database file is locked. Maximum number of access tries was exceeded.");
                    }
                    else
                    {
                        throw new Exception(String.Format("Error saving context to file {0}", DbFilePath), ex);   
                    }
                }
            }
        }

        private static bool IsFileLocked(Exception exception)
        {
            var errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }
    }
}