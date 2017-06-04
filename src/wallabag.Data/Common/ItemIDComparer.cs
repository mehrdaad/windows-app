using System.Collections.Generic;
using wallabag.Data.Models;

namespace wallabag.Data.Common
{
    class ItemIDComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y) => x.Id == y.Id;
        public int GetHashCode(Item obj) => obj.Id;
    }
}
