using System.Reflection;
using Ext.Core;

namespace StubDb.ModelStorage
{
    public class EntityConnectionInfo
    {
        public EntityTypeInfo ConnectedType { get; set; }
        public string NavigationPropertyName { get; set; }
        public bool IsMultipleConnection { get; set; }
        public bool IsSingleConnection
        {
            get { return !IsMultipleConnection; }
        }
        public bool IsNamedConnection { get; set; }
        public PropertyInfo NavigationIdProperty { get; set; }

        public string ConnectionName
        {
            get { return IsNamedConnection ? NavigationPropertyName : string.Empty; }
        }

        public bool HasNavigationIdProperty
        {
            get { return NavigationIdProperty != null; }
        }

        public void ClearNavigationIdProperty(object obj)
        {
            Check.NotNull(this.NavigationIdProperty, "Entity does not have navigation id property");
            this.NavigationIdProperty.SetValue(obj, -1);
        }

        public void SetNavigationIdProperty(object obj, int id)
        {
            Check.NotNull(this.NavigationIdProperty, "Entity does not have navigation id property");
            this.NavigationIdProperty.SetValue(obj, id);
        }

        public int GetNavigationIdProperty(object obj)
        {
            Check.NotNull(this.NavigationIdProperty, "Entity does not have navigation id property");
            return (int)this.NavigationIdProperty.GetValue(obj);
        }

        public EntityConnectionInfo(EntityTypeInfo entityType, EntityTypeInfo connectedType, string navigationPropertyName, bool isMultipleConnection)
        {
            this.IsMultipleConnection = isMultipleConnection;
            this.ConnectedType = connectedType;
            this.NavigationPropertyName = navigationPropertyName;

            this.NavigationIdProperty = EntityTypeManager.GetEntityNavigationIdProperty(entityType.Type, connectedType.Type);
        }
    }
}