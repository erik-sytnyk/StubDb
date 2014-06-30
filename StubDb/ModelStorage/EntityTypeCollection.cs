using System;
using System.Collections.Generic;
using System.Linq;
using Ext.Core;
using Ext.Core.Collections;

namespace StubDb.ModelStorage
{
    public class EntityTypeCollection : Dictionary<string, EntityTypeInfo>
    {
        public List<Type> IgnoredTypes { get; set; }

        public EntityTypeCollection()
        {
            IgnoredTypes = new List<Type>();
        }

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
                var enumerableType = EntityTypeManager.GetEnumerableType(propertyInfo.PropertyType);

                var connectionInfo = (EntityConnectionInfo)null;

                if (enumerableType != null && this.IsEntityType(enumerableType))
                {
                    connectionInfo = new EntityConnectionInfo(entityTypeInfo, this.GetType(enumerableType), propertyInfo.Name, true);
                }
                else if (this.IsEntityType(propertyInfo.PropertyType))
                {
                    connectionInfo = new EntityConnectionInfo(entityTypeInfo, this.GetType(propertyInfo.PropertyType), propertyInfo.Name, false);
                }

                if (connectionInfo != null)
                {
                    var key = connectionInfo.ConnectedType.UniqueName;
                    connectionsCounterByType.AddIfNoEntry(key, 0);
                    connectionsCounterByType[key]++;
                    connectionInfoList.Add(connectionInfo);
                }
            }

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

        public bool IsEntityType(Type type)
        {
            if (!EntityTypeManager.IsSimpleOrSimpleEnumerableType(type) && !this.IgnoredTypes.Contains(type)) return true;

            return false;
        }
    }
}