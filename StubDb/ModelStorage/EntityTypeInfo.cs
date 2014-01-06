﻿using System;
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

        public IEnumerable<PropertyInfo> GetProperties()
        {
            return EntityTypeManager.GetProperties(this.Type);
        }
    }
}