using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Ext.Core;
using StubDb.ModelStorage;
using StubDb.Persistence;
using StubDb.Store.InternalHelpers;

namespace StubDb.Store
{
    public class FlatFilePersistenceProvider : IContextStoragePersistenceProvider
    {
        private const int NumberOfTries = 60;
        private const string EntitiesLine = "Entities:";
        private const string ConnectionsLine = "Connections:";
        private const string PropertySeparator = "ߊ";
        private const string DashSeparator = "-";

        private string _dbFilePath = String.Empty;

        public FlatFilePersistenceProvider()
        {
        }

        public FlatFilePersistenceProvider(string dbFilePath)
        {
            _dbFilePath = dbFilePath;
        }

        public void SaveContext(StubContext context)
        {
            try
            {
                using (var writer = new StreamWriter(GetFilePath(context)))
                {
                    WriteContextToFile(writer, context);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(String.Format("Error saving context to file {0}", GetFilePath(context)), ex);
            }
        }

        private void WriteContextToFile(StreamWriter writer, StubContext context)
        {
            writer.WriteLine(EntitiesLine);

            foreach (var entityType in context.Types.Values)
            {
                writer.WriteLine("*{0}".FormatString(entityType.GetId()));

                var simpleProperties = this.GetSimpleProperties(entityType).ToList();

                //maybe for readability write property names to file                       

                var entities = context.Storage.Entities.GetEntities(entityType, false);

                foreach (var entity in entities)
                {
                    var valuesStr = String.Join(PropertySeparator, simpleProperties.Select(x => x.GetValue(entity)));

                    writer.WriteLine(valuesStr);
                }
            }

            writer.WriteLine(ConnectionsLine);

            var connectionsData = context.Storage.Connections.GetAllConnectionsData();

            foreach (var connectionData in connectionsData)
            {
                var connectionStr =
                    "*{0}{3}{1}{3}{2}".FormatString(connectionData.TypeFirst.UniqueName,
                                                    connectionData.TypeSecond.UniqueName,
                                                    connectionData.ConnectionName,
                                                    DashSeparator);

                writer.WriteLine(connectionStr);

                foreach (var singleConnection in connectionData)
                {
                    var connectionValues = "{0}{2}{1}".FormatString(singleConnection.Item1, singleConnection.Item2, DashSeparator);
                    writer.WriteLine(connectionValues);
                }
            }

        }

        public void LoadContext(StubContext context)
        {
            var filePath = this.GetFilePath(context);
            
            if (!File.Exists(filePath)) return;

            var tries = 0;
            
            bool successfulExecution = false;
            
            while (!successfulExecution)
            {
                tries++;
                try
                {
                    using (var reader = new StreamReader(filePath, true))
                    {
                        ReadContextFromFile(reader, context);
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
                        throw new Exception(String.Format("Error saving context to file {0}", filePath), ex);   
                    }
                }
            }
        }

        private void ReadContextFromFile(StreamReader reader, StubContext context)
        {
            context.Storage.Clear();

            reader.ReadLine(); //skip Entities line

            var line = reader.ReadLine();

            EntityTypeInfo type = null;
            var properties = new List<PropertyInfo>();

            while (line != ConnectionsLine)
            {
                if (line.StartsWith("*"))
                {
                    var typeName = line.Substring(1);
                    type = context.Types.GetTypeByName(typeName);
                    properties = GetSimpleProperties(type).ToList();
                }
                else
                {
                    var entity = EntityTypeManager.CreateNew(type.Type);
                    var entityId = -1;

                    var values = line.Split(new string[] { PropertySeparator }, StringSplitOptions.None);

                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        var value = ConvertToSimpleType(property.PropertyType, values[i]);
                        property.SetValue(entity, value);

                        if (property == type.IdProperty)
                        {
                            entityId = (int)value;
                        }
                    }

                    context.Storage.Entities.Add(entityId, entity);
                }

                line = reader.ReadLine();
            }

            line = reader.ReadLine();

            EntityTypeInfo connectionFirstType = null;
            EntityTypeInfo connectionSecondType = null;
            string connectionName = string.Empty;

            while (line != null)
            {
                if (line.StartsWith("*"))
                {
                    var connectionParts = line.Substring(1).Split(new[] { DashSeparator }, StringSplitOptions.None);
                    connectionFirstType = context.Types.GetTypeByName(connectionParts[0]);
                    connectionSecondType = context.Types.GetTypeByName(connectionParts[1]);
                    connectionName = connectionParts[2];
                }
                else
                {
                    var ids = line.Split(new string[] { DashSeparator }, StringSplitOptions.RemoveEmptyEntries);
                    context.Storage.Connections.AddConnection(connectionFirstType, connectionSecondType, connectionName, Convert.ToInt32(ids[0]), Convert.ToInt32(ids[1]), false);
                }

                line = reader.ReadLine();
            }
        }

        private static bool IsFileLocked(Exception exception)
        {
            var errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }

        private string GetFilePath(StubContext context)
        {
            if (!String.IsNullOrEmpty(_dbFilePath)) return _dbFilePath;

            var contextFileName = "{0}.data".FormatString(context.GetType().Name);
            
            return contextFileName;
        }

        private IEnumerable<PropertyInfo> GetSimpleProperties(EntityTypeInfo entityType)
        {
            return entityType.GetSimpleWritableProperties().OrderBy(x => x.Name);
        }

        private object ConvertToSimpleType(Type type, string value)
        {
            try
            {
                if (type.IsEnum)
                {
                    return Enum.Parse(type, value);
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
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
            catch (Exception ex)
            {
                
                throw ex;
            }
        }

    }
}