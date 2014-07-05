using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Ext.Core.Reflection;

namespace StubDb.ModelStorage
{
    public class ContextStorage: ICloneable
    {
        private EntityCollection _entities = new EntityCollection();
        private ConnectionsCollection _connections = new ConnectionsCollection();

        public EntityCollection Entities
        {
            get { return _entities; }
            set { _entities = value; }
        }

        public ConnectionsCollection Connections
        {
            get { return _connections; }
            set { _connections = value; }
        }

        public bool IsEmpty
        {
            get { return _entities.IsEmpty && _connections.IsEmpty; }
        }

        public void Clear()
        {
            _entities.Clear();
            _connections.Clear();
        }

        public object Clone()
        {
            var result = new ContextStorage();

            result.Entities = (EntityCollection)this.Entities.Clone();
            result.Connections = (ConnectionsCollection)this.Connections.Clone();

            return result;
        }
    }
}