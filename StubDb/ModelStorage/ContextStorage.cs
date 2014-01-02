namespace StubDb.ModelStorage
{
    public class ContextStorage
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
    }
}