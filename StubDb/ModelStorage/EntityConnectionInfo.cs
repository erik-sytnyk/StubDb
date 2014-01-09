namespace StubDb.ModelStorage
{
    public class EntityConnectionInfo
    {
        public EntityTypeInfo ConnectedType { get; set; }
        public string PropertyName { get; set; } 
        public bool IsMultipleConnection { get; set; }
        public bool IsNamedConnection { get; set; }
    }
}