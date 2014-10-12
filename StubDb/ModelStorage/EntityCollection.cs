using System;
using System.Collections.Generic;
using System.Linq;
using Ext.Core;
using Ext.Core.Collections;

namespace StubDb.ModelStorage
{
    public class EntityCollection: ICloneable
    {
        internal Dictionary<Type, EntityCollectionEntry> _storage = new Dictionary<Type, EntityCollectionEntry>();

        public void Add(int id, object entity)
        {
            var type = entity.GetType();

            _storage.AddIfNoEntry(type, new EntityCollectionEntry());

            _storage[type].Add(id, entity);
        }

        public void Update(int id, object entity)
        {
            var type = entity.GetType();

            Check.That(_storage[type].ContainsKey(id), "Trying to update entity, which is not in context");

            _storage[type][id] = entity;
        }

        public object GetById(int id, EntityTypeInfo type)
        {
            _storage.AddIfNoEntry(type.Type, new EntityCollectionEntry());

            var dict = _storage[type.Type];

            if (dict.ContainsKey(id))
            {
                var entity = dict[id];
                return EntityTypeManager.CloneObject(entity);
            }

            return type.Type.GetDefault();
        }

        public void Remove(int id, EntityTypeInfo type)
        {
            _storage.AddIfNoEntry(type.Type, new EntityCollectionEntry());

            _storage[type.Type].Remove(id);
        }

        public int GetAvailableIdForEntityType(EntityTypeInfo entityType)
        {
            _storage.AddIfNoEntry(entityType.Type, new EntityCollectionEntry());

            var dict = _storage[entityType.Type];

            return dict.GetNextId();
        }

        public List<object> GetEntities(EntityTypeInfo entityType)
        {
            return GetEntities(entityType, true);
        }

        public List<object> GetEntities(EntityTypeInfo entityType, bool clone)
        {
            var result = new List<object>();

            if (_storage.ContainsKey(entityType.Type))
            {
                result = _storage[entityType.Type].Values.Select(x => clone ? EntityTypeManager.CloneObject(x) : x).ToList();
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

        public object Clone()
        {
            var result = new EntityCollection();

            foreach (var entityCollectionPair in _storage)
            {
                var entityCollectionEntryClone = new EntityCollectionEntry();

                foreach (var entityPair in entityCollectionPair.Value)
                {
                    entityCollectionEntryClone.Add(entityPair.Key, EntityTypeManager.CloneObject(entityPair.Value));
                }

                result._storage.Add(entityCollectionPair.Key, entityCollectionEntryClone);
            }

            return result;
        }
    }
}