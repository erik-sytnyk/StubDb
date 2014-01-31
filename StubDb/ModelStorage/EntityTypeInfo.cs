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

        //TODO add == operator overload
        public override bool Equals(object obj)
        {
            var entityTypeInfo = obj as EntityTypeInfo;

            if (entityTypeInfo == null) return false;

            return entityTypeInfo.UniqueName == this.UniqueName;
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