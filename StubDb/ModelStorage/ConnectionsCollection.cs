using System;
using System.Collections.Generic;
using System.Linq;
using Ext.Core.Collections;

namespace StubDb.ModelStorage
{
    public class ConnectionsCollection
    {
        private List<EntityConnection> _storage = new List<EntityConnection>();

        public void AddConnection(Type typeFirst, Type typeSecond, int idFirst, int idSecond)
        {
            var newConnection = new EntityConnection(typeFirst.GetId(), typeSecond.GetId(), idFirst, idSecond);

            var existingConnection = _storage.FirstOrDefault(x => x.Equals(newConnection)); //TODO Performance do not check, when seeding initial data

            if (existingConnection == null)
            {
                _storage.Add(newConnection);
            }
        }

        public void RemoveConnectionsFor(Type entityType, int entityId)
        {
            _storage.RemoveAll(x => x.TypeFirst == entityType.GetId() && x.IdFirst == entityId);
            _storage.RemoveAll(x => x.TypeSecond == entityType.GetId() && x.IdSecond == entityId);
        }

        public void RemoveConnectionsFor(Type entityType, int entityId, Type connectionType)
        {
            var isRightOrder = EntityConnection.IsDefaultTypeStoringOrder(entityType, connectionType);

            if (isRightOrder)
            {
                _storage.RemoveAll(x => x.TypeFirst == entityType.GetId() && x.TypeSecond == connectionType.GetId() && x.IdFirst == entityId);
            }
            else
            {
                _storage.RemoveAll(x => x.TypeSecond == entityType.GetId() && x.TypeFirst == connectionType.GetId() && x.IdSecond == entityId);
            }
        }

        public List<EntityConnection> GetConnectionsFor(Type entityType, int entityId, Type connectionType)
        {
            var result = (List<EntityConnection>)null;
            var isRightOrder = EntityConnection.IsDefaultTypeStoringOrder(entityType, connectionType);
            var entityTypeId = entityType.GetId();
            var connectionTypeId = connectionType.GetId();

            if (isRightOrder)
            {
                result = new List<EntityConnection>();
                foreach (var x in _storage)
                {
                    if (x.IdFirst == entityId && x.TypeFirst == entityTypeId && x.TypeSecond == connectionTypeId)
                    {
                        result.Add(x);
                    }
                }
                //result = _storage.Where(x => x.IdFirst == entityId && x.TypeFirst == entityTypeId && x.TypeSecond == connectionTypeId).ToList();
            }
            else
            {
                result = _storage.Where(x => x.TypeSecond == entityTypeId && x.TypeFirst == connectionTypeId && x.IdSecond == entityId).ToList();
            }

            return result;
        } 

        public IEnumerable<EntityConnection> GetAllConnections()
        {
            return _storage;
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