using System;
using System.Linq;
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

            var navigationIdProperty = GetNavigationIdProperty(entityType, navigationPropertyName);

            this.NavigationIdProperty = navigationIdProperty;
        }

        private static PropertyInfo GetNavigationIdProperty(EntityTypeInfo entityType, string navigationPropertyName)
        {
            var navigationTypeNamePlusId = String.Format("{0}Id", navigationPropertyName);

            var result =
                EntityTypeManager.GetProperties(entityType.Type)
                    .SingleOrDefault(x => x.Name.Equals(navigationTypeNamePlusId, StringComparison.OrdinalIgnoreCase));

            if (result != null)
            {
                Check.That(result.PropertyType == typeof (int) || result.PropertyType == typeof (int?),
                    "Navigation ID property is not of type integer");
            }
            return result;
        }
    }
}