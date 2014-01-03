using System.Collections.Generic;
using System.Linq;

namespace StubDb.ModelStorage
{
    public class EntityCollectionEntry: Dictionary<int, object>
    {
        //maxId is not calculated to improve performance
        private int _maxId = -1;

        public int GetNextId()
        {
            if (_maxId == -1)
            {
                _maxId = this.Keys.Count > 0 ? this.Keys.Max(x => x) : 0;
            }
            return ++_maxId;
        }
    }
}