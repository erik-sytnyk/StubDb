using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace StubDb.ModelStorage
{
    public class EntityTypeInfo
    {
        public int Id { get; set; }

        public string UniqueName
        {
            get { return Type.GetId(); }
        }

        public Type Type { get; set; }

        public string GetId()
        {
            return this.UniqueName;
        }

        public PropertyInfo IdProperty { get; set; }

        public List<EntityConnectionInfo> Connections { get; set; }

        public EntityTypeInfo()
        {
            Connections = new List<EntityConnectionInfo>();
        }

        public IEnumerable<PropertyInfo> GetProperties()
        {
            return EntityTypeManager.GetProperties(this.Type);
        }

        public override bool Equals(object obj)
        {
            var entityTypeInfo = obj as EntityTypeInfo;

            if (entityTypeInfo == null) return false;

            return entityTypeInfo.UniqueName == this.UniqueName;
        }

        public static bool operator ==(EntityTypeInfo a, EntityTypeInfo b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(EntityTypeInfo a, EntityTypeInfo b)
        {
            return !(a == b);
        }

        public int GetEntityId(object entity)
        {
            return (int)this.IdProperty.GetValue(entity);
        }

        public void SetEntityId(object entity, int id)
        {
            this.IdProperty.SetValue(entity, id);
        }
    }
}