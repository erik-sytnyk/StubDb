using System;
using System.Collections.Generic;
using StubDb.ModelStorage;

namespace StubDb.Persistence
{
    public interface IContextStoragePersistenceProvider
    {
        void SaveContext(ContextStorage storage, EntityTypeCollection types);
        void LoadContext(ContextStorage storage, EntityTypeCollection types);
    }
}