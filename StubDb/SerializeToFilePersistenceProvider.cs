using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace StubDb
{
    public class SerializeToFilePersistenceProvider : IContextStoragePersistenceProvider
    {
        public const string TemFilePath = @"C:\Temp\StubDb";

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

            public DataContainer(StubContext.ContextStorage storage, List<Type> types)
                : this()
            {
                foreach (var entityType in types)
                {
                    var entityContainer = new EntityContainer();

                    entityContainer.TypeName = entityType.FullName;

                    var simpleProperties = this.GetSimpleProperties(entityType).ToList();

                    entityContainer.Properties = this.GetPropertiesString(simpleProperties);

                    var entities = storage.Entities.GetEntities(entityType).Values;

                    foreach (var entity in entities)
                    {
                        var valueString = this.GetValuesString(entity, simpleProperties);
                        entityContainer.ValuesList.Add(valueString);
                    }

                    this.Entities.Add(entityContainer);

                }

                //NOTE performance?
                var groupedConnections =
                    storage.Connections.GetAllConnections().GroupBy(x => x.TypeFirst + SeparatorString + x.TypeSecond);

                foreach (var groupedConnection in groupedConnections)
                {
                    var connectionContainer = new ConnectionContainer();

                    var typeNames = groupedConnection.Key.Split(new string[] { SeparatorString }, StringSplitOptions.RemoveEmptyEntries);

                    connectionContainer.FirstType = typeNames[0];
                    connectionContainer.SecondType = typeNames[1];

                    var connectionsString = new StringBuilder();

                    foreach (var entityConnection in groupedConnection)
                    {
                        connectionsString.Append(entityConnection.IdFirst);
                        connectionsString.Append(SeparatorConnectionString);
                        connectionsString.Append(entityConnection.IdSecond);
                        connectionsString.Append(SeparatorString);
                    }

                    connectionsString.Remove(connectionsString.Length - 1, 1); //remove last separator symbol

                    connectionContainer.ConnectionsString = connectionsString.ToString();

                    this.Connections.Add(connectionContainer);
                }
            }

            public void GetContextStorage(StubContext.ContextStorage storage, List<Type> types)
            {
                storage.Clear();

                foreach (var entityContainer in this.Entities)
                {
                    var type = types.FirstOrDefault(x => x.FullName == entityContainer.TypeName);

                    if (type != null)
                    {
                        var simpleProperties = GetSimpleProperties(type).ToList();
                        var retrievedPropertyNames = entityContainer.Properties.Split(new string[] {SeparatorString}, StringSplitOptions.RemoveEmptyEntries);

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
                            var values = valuesString.Split(new string[] { SeparatorString }, StringSplitOptions.RemoveEmptyEntries);

                            var entity = Activator.CreateInstance(type);

                            int entityId = -1;

                            foreach (var mapping in map)
                            {
                                var value = this.ConvertToSimpleType(mapping.Value.PropertyType, values[mapping.Key]);
                                mapping.Value.SetValue(entity, value);

                                if (mapping.Value.Name.ToLower() == "id") //TODO
                                {
                                    entityId = (int) value;
                                }
                            }

                            storage.Entities.Add(entityId, entity);
                        }
                    }
                }

                foreach (var connectionContainer in this.Connections)
                {
                    var firstType = types.FirstOrDefault(x => x.FullName == connectionContainer.FirstType);
                    var secondType = types.FirstOrDefault(x => x.FullName == connectionContainer.SecondType);

                    if (firstType != null && secondType != null)
                    {
                        var connections = connectionContainer.ConnectionsString.Split(new string[] {SeparatorString}, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var connection in connections)
                        {
                            var ids = connection.Split(new string[] {SeparatorConnectionString}, StringSplitOptions.RemoveEmptyEntries);
                            storage.Connections.AddConnection(firstType, secondType, Convert.ToInt32(ids[0]), Convert.ToInt32(ids[1]));
                        }
                    }
                }
            }

            //TODO support all simple types, probably move to Ext.Core
            private object ConvertToSimpleType(Type type, string value)
            {
                return  Convert.ChangeType(value, type);
            }

            private string GetValuesString(object entity, IEnumerable<PropertyInfo> properties)
            {
                return String.Join(SeparatorString, properties.Select(x => x.GetValue(entity)));
            }

            private IEnumerable<PropertyInfo> GetSimpleProperties(Type entityType)
            {
                return EntityTypeManager.GetSimpleProperties(entityType).OrderBy(x => x.Name);
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
            public string ConnectionsString { get; set; }
        }

        #endregion

        public void SaveContext(StubContext.ContextStorage storage, Dictionary<string, Type> types)
        {
            var data = new DataContainer(storage, types.Select(x => x.Value).ToList());

            string json = JsonConvert.SerializeObject(data);

            using (var writer = new StreamWriter(TemFilePath, false))
            {
                writer.WriteLine(json);
            }
        }

        public void LoadContext(StubContext.ContextStorage storage, Dictionary<string, Type> types)
        {
            var result = (StubContext.ContextStorage)null;

            using (var reader = new StreamReader(TemFilePath, true))
            {
                string json = reader.ReadToEnd();
                var dataContainer = JsonConvert.DeserializeObject<DataContainer>(json);
                dataContainer.GetContextStorage(storage, types.Values.ToList());
            }
        }
    }
}