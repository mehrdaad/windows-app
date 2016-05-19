using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallabag.Models;

namespace wallabag.Common
{
    class ItemByModificationDateEqualityComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y) => x.LastModificationDate.Equals(y.LastModificationDate);
        public int GetHashCode(Item obj) => obj.GetHashCode();
    }
}
