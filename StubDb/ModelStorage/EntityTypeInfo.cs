﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace StubDb.ModelStorage
{
    public class EntityConnectionInfo
    {
        public EntityTypeInfo ConnectedType { get; set; }
        public string ConnectionName { get; set; } //empty if it is single connection for that type
        public bool IsMultipleConnection { get; set; }
    }

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

        public List<EntityTypeInfo> Connections { get; set; }

        public EntityTypeInfo()
        {
            Connections = new List<EntityTypeInfo>();
        }

        public IEnumerable<PropertyInfo> GetProperties()
        {
            return EntityTypeManager.GetProperties(this.Type);
        }
    }
}