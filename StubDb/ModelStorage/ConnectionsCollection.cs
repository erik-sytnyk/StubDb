using System;
using System.Collections.Generic;
using System.Linq;
using Ext.Core;
using Ext.Core.Collections;

namespace StubDb.ModelStorage
{
    public class ConnectionData : List<Tuple<int, int>>
    {
        private Dictionary<int, List<int>> _dictionaryByIdFirst = null;
        private Dictionary<int, List<int>> _dictionaryByIdSecond = null;

        public EntityTypeInfo TypeFirst { get; set; }
        public EntityTypeInfo TypeSecond { get; set; }
        public string ConnectionName { get; set; }

        public ConnectionData(EntityTypeInfo typeFirst, EntityTypeInfo typeSecond, string connectionName)
        {
            TypeFirst = typeFirst;
            TypeSecond = typeSecond;
            ConnectionName = connectionName;
        }

        public Dictionary<int, List<int>> GroupByIdFirst()
        {
            if (_dictionaryByIdFirst == null)
            {
                _dictionaryByIdFirst = new Dictionary<int, List<int>>();
                foreach (var item in this)
                {
                    _dictionaryByIdFirst.AddIfNoEntry(item.Item1, new List<int>());
                    _dictionaryByIdFirst[item.Item1].Add(item.Item2);
                }
            }
            return _dictionaryByIdFirst;
        }

        public Dictionary<int, List<int>> GroupByIdSecond()
        {
            if (_dictionaryByIdSecond == null)
            {
                _dictionaryByIdSecond = new Dictionary<int, List<int>>();
                foreach (var item in this)
                {
                    _dictionaryByIdSecond.AddIfNoEntry(item.Item2, new List<int>());
                    _dictionaryByIdSecond[item.Item2].Add(item.Item1);
                }
            }
            return _dictionaryByIdSecond;
        }

        public void Add(int idFirst, int idSecond, bool checkExisting)
        {
            if (checkExisting)
            {
                var existingConnection = this.FirstOrDefault(x => x.Item1 == idFirst && x.Item2 == idSecond);
                Check.That(existingConnection == null, "Trying to add existing connection");
            }
            this.Add(new Tuple<int, int>(idFirst, idSecond));
            this.ClearCache();
        }

        public void ClearCache()
        {
            _dictionaryByIdFirst = null;
            _dictionaryByIdSecond = null;
        }
    }

    public class ConnectionsCollection
    {
        private Dictionary<string, ConnectionData> _storage = new Dictionary<string, ConnectionData>();

        public void AddConnection(EntityTypeInfo typeFirst, EntityTypeInfo typeSecond, string connectionName, int idFirst, int idSecond, bool checkForExistingConnections)
        {
            var newConnection = new EntityConnection(typeFirst, typeSecond, connectionName, idFirst, idSecond);

            var key = newConnection.GetUniqueKey();

            _storage.AddIfNoEntry(key, new ConnectionData(newConnection.TypeFirst, newConnection.TypeSecond, newConnection.ConnectionName));

            _storage[key].Add(newConnection.IdFirst, newConnection.IdSecond, checkForExistingConnections);
        }

        public void RemoveConnectionsFor(EntityTypeInfo entityType, EntityTypeInfo connectedType, string connectionName, int entityId)
        {
            var newConnection = new EntityConnection(entityType, connectedType, connectionName);
            var key = newConnection.GetUniqueKey();
            if (_storage.ContainsKey(key))
            {
                if (newConnection.TypeFirst.Equals(entityType))
                {
                    _storage[key].RemoveAll(x => x.Item1 == entityId);
                }
                else
                {
                    _storage[key].RemoveAll(x => x.Item2 == entityId);
                }
                _storage[key].ClearCache();
            }
        }

        public List<int> GetConnectionsFor(EntityTypeInfo entityType, EntityTypeInfo connectedType, string connectionName, int entityId)
        {
            var result = new List<int>();
            var newConnection = new EntityConnection(entityType, connectedType, connectionName);
            var key = newConnection.GetUniqueKey();

            if (!_storage.ContainsKey(key)) return result;

            var dict = newConnection.TypeFirst.Equals(entityType) ?  _storage[key].GroupByIdFirst(): _storage[key].GroupByIdSecond();

            if (dict.ContainsKey(entityId))
            {
                result = dict[entityId];
            }

            return result;
        }

        public ConnectionData GetConnectionData(EntityTypeInfo typeFirst, EntityTypeInfo typeSecond, String connectionName)
        {
            var connection = new EntityConnection(typeFirst, typeSecond, connectionName);
            var key = connection.GetUniqueKey();

            if (_storage.ContainsKey(key))
            {
                return _storage[key];
            }

            return null;
        }

        public bool IsEmpty
        {
            get { return _storage.Count == 0; }
        }

        public void Clear()
        {
            _storage.Clear();
        }

        public List<ConnectionData> GetAllConnectionsData()
        {
            return this._storage.Values.ToList();
        }
    }
}