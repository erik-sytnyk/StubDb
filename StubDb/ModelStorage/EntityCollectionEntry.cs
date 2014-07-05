using System;
using System.Collections.Generic;
using System.Linq;

namespace StubDb.ModelStorage
{
    public class EntityCollectionEntry: Dictionary<int, object>
    {
        //maxId is cached to improve performance
        private int? _maxId;

        public int GetNextId()
        {
            if (_maxId == null)
            {
                _maxId = this.Keys.Count > 0 ? this.Keys.Max(x => x) : 0;
            }
            return (++_maxId).Value;
        }
    }
}