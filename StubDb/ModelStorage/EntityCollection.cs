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

        public void Update(int id, object entity)
        {
            var typeId = entity.GetType().GetId();

            Check.That(_storage[typeId].ContainsKey(id), "Trying to update entity, which is not in context");

            _storage[typeId][id] = entity;
        }

        public object GetById(int id, EntityTypeInfo type)
        {
            _storage.AddIfNoEntry(type.GetId(), new EntityCollectionEntry());

            var dict = _storage[type.GetId()];

            if (dict.ContainsKey(id))
            {
                var entity = dict[id];
                return EntityTypeManager.CloneObject(entity);
            }

            return type.Type.GetDefault();
        }

        public void Remove(int id, EntityTypeInfo type)
        {
            _storage.AddIfNoEntry(type.GetId(), new EntityCollectionEntry());

            _storage[type.GetId()].Remove(id);
        }

        public int GetAvailableIdForEntityType(EntityTypeInfo entityType)
        {
            _storage.AddIfNoEntry(entityType.GetId(), new EntityCollectionEntry());

            var dict = _storage[entityType.GetId()];

            return dict.GetNextId();
        }

        public List<object> GetEntities(EntityTypeInfo entityType)
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