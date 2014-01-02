using System;
using System.Collections.Generic;
using System.Linq;

namespace StubDb.ModelStorage
{
    public class ConnectionsCollection
    {
        private List<EntityConnection> _storage = new List<EntityConnection>();

        public void AddConnection(Type typeFirst, Type typeSecond, int idFirst, int idSecond)
        {
            var newConnection = new EntityConnection(typeFirst.GetId(), typeSecond.GetId(), idFirst, idSecond);

            var existingConnection = _storage.FirstOrDefault(x => x.Equals(newConnection));

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

            if (isRightOrder)
            {
                result = _storage.Where(x => x.TypeFirst == entityType.GetId() && x.TypeSecond == connectionType.GetId() && x.IdFirst == entityId).ToList();
            }
            else
            {
                result = _storage.Where(x => x.TypeSecond == entityType.GetId() && x.TypeFirst == connectionType.GetId() && x.IdSecond == entityId).ToList();
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