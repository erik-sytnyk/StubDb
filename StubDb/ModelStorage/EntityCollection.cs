using System;
using System.Collections.Generic;
using System.Linq;
using Ext.Core;
using Ext.Core.Collections;

namespace StubDb.ModelStorage
{
    public class EntityCollection
    {
        Dictionary<string, EntityCollectionEntry> _storage = new Dictionary<string, EntityCollectionEntry>();

        public void Add(int id, object entity)
        {
            var type = entity.GetType();

            _storage.AddIfNoEntry(type.GetId(), new EntityCollectionEntry());

            _storage[type.GetId()].Add(id, entity);
        }

        public object GetById(int id, Type type)
        {
            _storage.AddIfNoEntry(type.GetId(), new EntityCollectionEntry());

            var dict = _storage[type.GetId()];

            if (dict.ContainsKey(id))
            {
                var entity = dict[id];
                return EntityTypeManager.CloneObject(entity);
            }

            return type.GetDefault();
        }

        public void Remove(int id, Type type)
        {
            _storage.AddIfNoEntry(type.GetId(), new EntityCollectionEntry());

            _storage[type.GetId()].Remove(id);
        }

        public int GetAvailableIdForEntityType(Type entityType)
        {
            _storage.AddIfNoEntry(entityType.GetId(), new EntityCollectionEntry());

            var dict = _storage[entityType.GetId()];

            return dict.GetNextId();
        }

        public List<object> GetEntities(Type entityType)
        {
            var result = new List<object>();

            if (_storage.ContainsKey(entityType.GetId()))
            {
                result = _storage[entityType.GetId()].Values.Select(EntityTypeManager.CloneObject).ToList();
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