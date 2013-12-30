using System;
using System.Collections.Generic;
using System.Linq;
using Ext.Core;
using Ext.Core.Collections;

namespace StubDb.ModelStorage
{
    public class EntityCollection
    {
        Dictionary<string, Dictionary<int, object>> _storage = new Dictionary<string, Dictionary<int, object>>();

        public void Add(int id, object entity)
        {
            var type = entity.GetType();

            _storage.AddIfNoEntry(type.GetId(), new Dictionary<int, object>());

            _storage[type.GetId()].Add(id, entity);
        }

        public T GetById<T>(int id)
        {
            return (T)GetById(id, typeof(T));
        }

        public object GetById(int id, Type type)
        {
            _storage.AddIfNoEntry(type.GetId(), new Dictionary<int, object>());

            var dict = _storage[type.GetId()];

            return dict.ContainsKey(id) ? dict[id] : type.GetDefault();
        }

        public void Remove(int id, Type type)
        {
            _storage.AddIfNoEntry(type.GetId(), new Dictionary<int, object>());

            _storage[type.GetId()].Remove(id);
        }

        public int GetAvailableIdForEntityType(Type entityType)
        {
            _storage.AddIfNoEntry(entityType.GetId(), new Dictionary<int, object>());

            var dict = _storage[entityType.GetId()];

            return dict.Keys.Count > 0 ? dict.Keys.Max(x => x) + 1 : 1;
        }

        public Dictionary<int, object> GetEntities<T>()
        {
            var type = typeof(T);

            return GetEntities(type);
        }

        public Dictionary<int, object> GetEntities(Type entityType)
        {
            var result = new Dictionary<int, object>();

            if (_storage.ContainsKey(entityType.GetId()))
            {
                result = _storage[entityType.GetId()];
            }

            return result;
        }

        public bool IsEmpty
        {
            get { return _storage.Count == 0; }
        }

        public void Clear()
        {
            _storage.Clear();
        }
    }
}