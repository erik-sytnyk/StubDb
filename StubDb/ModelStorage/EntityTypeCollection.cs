using System;
using System.Collections.Generic;
using System.Linq;
using Ext.Core.Collections;

namespace StubDb.ModelStorage
{
    public class EntityTypeCollection : Dictionary<string, EntityTypeInfo>
    {
        public void Add(Type type)
        {
            var entityTypeInfo = new EntityTypeInfo();

            entityTypeInfo.Type = type;
            entityTypeInfo.Id = this.GetNextAvailableId();
            entityTypeInfo.IdProperty = EntityTypeManager.GetEntityIdProperty(type);

            base.Add(type.Name, entityTypeInfo);
        }

        public EntityTypeInfo GetTypeByName(string name)
        {
            return this.ContainsKey(name) ? this[name] : null;
        }

        public EntityTypeInfo GetType(Type type)
        {
            var key = type.GetId();
            return GetTypeByName(key);
        }

        public new void Add(string name, EntityTypeInfo typeInfo)
        {
            throw new NotImplementedException();
        }

        public void LoadConnections(EntityTypeInfo entityTypeInfo)
        {
            var connectionsCounterByType = new Dictionary<string, int>();
            var connectionInfoList = new List<EntityConnectionInfo>();

            foreach (var propertyInfo in entityTypeInfo.GetProperties())
            {
                var enumerableEntityType = EntityTypeManager.GetEnumerableEntityType(propertyInfo.PropertyType);

                var connectionInfo = (EntityConnectionInfo)null;

                if (enumerableEntityType != null)
                {
                    connectionInfo = new EntityConnectionInfo();
                    connectionInfo.IsMultipleConnection = true;
                    connectionInfo.ConnectedType = this.GetType(enumerableEntityType);
                    connectionInfo.PropertyName = propertyInfo.Name;
                }
                else if (!EntityTypeManager.IsSimpleOrSimpleEnumerableType(propertyInfo.PropertyType))
                {
                    connectionInfo = new EntityConnectionInfo();
                    connectionInfo.IsMultipleConnection = false;
                    connectionInfo.ConnectedType = this.GetType(propertyInfo.PropertyType);
                    connectionInfo.PropertyName = propertyInfo.Name;
                }

                if (connectionInfo != null)
                {
                    var key = connectionInfo.ConnectedType.UniqueName;
                    connectionsCounterByType.AddIfNoEntry(key, 0);
                    connectionsCounterByType[key]++;
                    connectionInfoList.Add(connectionInfo);
                }
            }

            //TODO check that type with named connection is not connected to referencing type
            var typesWithNamedConnections = connectionsCounterByType.Where(x => x.Value > 1).Select(x => x.Key).ToList();

            foreach (var entityConnectionInfo in connectionInfoList)
            {
                if (typesWithNamedConnections.Contains(entityConnectionInfo.ConnectedType.UniqueName))
                {
                    entityConnectionInfo.IsNamedConnection = true;
                }
            }

            entityTypeInfo.Connections = connectionInfoList;
        }

        #region Helper methods

        private int GetNextAvailableId()
        {
            if (this.Values.Count == 0)
            {
                return 1;
            }
            return this.Values.Select(x => x.Id).Max(x => x) + 1;
        }

        #endregion
    }
}