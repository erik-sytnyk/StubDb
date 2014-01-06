namespace StubDb.ModelStorage
{
    public class RequiredDependancy
    {
        public EntityTypeInfo DependantType { get; set; }
        public EntityTypeInfo RequiredType { get; set; }
    }
}