using System;
using System.Collections.Generic;

namespace StubDb
{
    public interface IContextStoragePersistenceProvider
    {
        void SaveContext(StubContext.ContextStorage storage, Dictionary<string, Type> types);
        StubContext.ContextStorage LoadContext(Dictionary<string, Type> types);
    }
}