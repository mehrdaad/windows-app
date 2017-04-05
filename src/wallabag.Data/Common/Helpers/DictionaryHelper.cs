using System.Collections.Generic;

namespace wallabag.Data.Common.Helpers
{
    public static class DictionaryHelper
    {
        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            dict.TryGetValue(key, out var result);
            return result;
        }
    }
}
