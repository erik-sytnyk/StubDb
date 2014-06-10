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

        private static string _keySeparator = "@";

        public ConnectionData(EntityTypeInfo type1, EntityTypeInfo type2, string name)
        {
            if (GetTypeSortingOrder(type1, type2) >= 0)
            {
                TypeFirst = type1;
                TypeSecond = type2;
            }
            else
            {
                TypeFirst = type2;
                TypeSecond = type1;
            }
            ConnectionName = name;
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

        public void AddConnection(EntityTypeInfo typeFirst, EntityTypeInfo typeSecond, string connectionName, int idFirst, int idSecond, bool checkForExistingConnections)
        {            
            if (TypeFirst.UniqueName == typeFirst.UniqueName)
            {
                if (checkForExistingConnections)
                {
                    var existingConnection = this.FirstOrDefault(x => x.Item1 == idFirst && x.Item2 == idSecond);
                    Check.That(existingConnection == null, "Trying to add existing connection");
                }
                this.Add(new Tuple<int, int>(idFirst, idSecond));
            }
            else if (TypeFirst.UniqueName == typeSecond.UniqueName)
            {
                if (checkForExistingConnections)
                {
                    var existingConnection = this.FirstOrDefault(x => x.Item1 == idSecond && x.Item2 == idFirst);
                    Check.That(existingConnection == null, "Trying to add existing connection");
                }
                this.Add(new Tuple<int, int>(idSecond, idFirst));
            }
            else
            {
                throw new ApplicationException("Adding entity connection for wrong entity types");
            }

            this.ClearCache();
        }

        public string GetUniqueKey()
        {
            return String.Format("{1}{0}{2}{0}{3}", _keySeparator, TypeFirst.UniqueName, TypeSecond.UniqueName, ConnectionName);
        }

        public void ClearCache()
        {
            _dictionaryByIdFirst = null;
            _dictionaryByIdSecond = null;
        }

        private static int GetTypeSortingOrder(EntityTypeInfo typeFirst, EntityTypeInfo typeSecond)
        {
            return String.Compare(typeFirst.UniqueName, typeSecond.UniqueName, StringComparison.Ordinal);
        }

        public static void ParseFromKey(string key, out string typeFirst, out string typeSecond, out string connectionName)
        {
            var parts = key.Split(new string[] { _keySeparator }, StringSplitOptions.None);

            Check.That(parts.Length == 3, "Wrong key format");

            typeFirst = parts[0];
            typeSecond = parts[1];
            connectionName = parts[2];
        }
    }

    public class ConnectionsCollection
    {
        private Dictionary<string, ConnectionData> _storage = new Dictionary<string, ConnectionData>();

        public void AddConnection(EntityTypeInfo typeFirst, EntityTypeInfo typeSecond, string connectionName, int idFirst, int idSecond, bool checkForExistingConnections)
        {
            var newConnection = new ConnectionData(typeFirst, typeSecond, connectionName);

            var key = newConnection.GetUniqueKey();

            _storage.AddIfNoEntry(key, newConnection);

            _storage[key].AddConnection(typeFirst, typeSecond, connectionName, idFirst, idSecond, checkForExistingConnections);
        }

        public void RemoveConnectionsFor(EntityTypeInfo entityType, int entityId)
        {
            var clearCache = false;

            foreach (var connectionData in _storage.Values)
            {
                if (connectionData.TypeFirst == entityType)
                {
                    _storage[connectionData.GetUniqueKey()].RemoveAll(x => x.Item1 == entityId);
                    _storage[connectionData.GetUniqueKey()].ClearCache();
                }

                if (connectionData.TypeSecond == entityType)
                {
                    _storage[connectionData.GetUniqueKey()].RemoveAll(x => x.Item2 == entityId);
                    _storage[connectionData.GetUniqueKey()].ClearCache();
                }
            }
        }

        public void RemoveConnectionsFor(EntityTypeInfo entityType, EntityTypeInfo connectedType, string connectionName, int entityId)
        {
            var newConnection = new ConnectionData(entityType, connectedType, connectionName);
            var key = newConnection.GetUniqueKey();
            if (_storage.ContainsKey(key))
            {
                if (newConnection.TypeFirst == entityType)
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
            var newConnection = new ConnectionData(entityType, connectedType, connectionName);
            var key = newConnection.GetUniqueKey();

            if (!_storage.ContainsKey(key)) return result;

            var dict = newConnection.TypeFirst == entityType ? _storage[key].GroupByIdFirst() : _storage[key].GroupByIdSecond();

            if (dict.ContainsKey(entityId))
            {
                result = dict[entityId];
            }

            return result;
        }

        public ConnectionData GetConnectionData(EntityTypeInfo typeFirst, EntityTypeInfo typeSecond, String connectionName)
        {
            var connection = new ConnectionData(typeFirst, typeSecond, connectionName);
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