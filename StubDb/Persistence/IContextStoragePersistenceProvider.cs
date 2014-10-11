using System;
using System.Collections.Generic;
using StubDb.ModelStorage;

namespace StubDb.Persistence
{
    public interface IContextStoragePersistenceProvider
    {
        void SaveContext(StubContext context);
        void LoadContext(StubContext context);
    }
}