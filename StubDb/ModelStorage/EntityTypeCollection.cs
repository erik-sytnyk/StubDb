using System;
using System.Collections.Generic;
using System.Linq;

namespace StubDb.ModelStorage
{
    public class EntityTypeCollection: Dictionary<string, EntityTypeInfo>
    {
        public void Add(Type type)
        {
            var entityTypeInfo = new EntityTypeInfo();
            entityTypeInfo.Type = type;
            entityTypeInfo.Id = this.GetNextAvailableId();
            base.Add(type.Name, entityTypeInfo);
        }

        public EntityTypeInfo GetTypeByName(string name)
        {
            return this.ContainsKey(name) ? this[name] : null;
        }

        public EntityTypeInfo GetType(Type type)
        {
            var key = type.GetId();
            return GetTypeByName(key);
        }

        public new void Add(string name, EntityTypeInfo typeInfo)
        {
            throw new NotImplementedException();
        }

        private int GetNextAvailableId()
        {
            if (this.Values.Count == 0)
            {
                return 1;
            }
            return this.Values.Select(x => x.Id).Max(x => x) + 1;
        }
    }
}