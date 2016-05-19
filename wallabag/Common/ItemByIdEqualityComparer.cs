using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallabag.Models;

namespace wallabag.Common
{
    class ItemByIdEqualityComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y) => x.Id.Equals(y.Id);
        public int GetHashCode(Item obj) => obj.GetHashCode();
    }
}
