using System;
using System.Collections.Generic;
using StubDb.ModelStorage;

namespace StubDb.Persistence
{
    public interface IContextStoragePersistenceProvider
    {
        void SaveContext(ContextStorage storage, Dictionary<string, Type> types);
        void LoadContext(ContextStorage storage, Dictionary<string, Type> types);
    }
}