using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using wallabag.Data.Interfaces;

namespace wallabag.Data.Common
{
    public class IncrementalObservableCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        private Func<int, Task<List<T>>> _loadDataAsync;
        public bool HasMoreItems => Items.Count < MaxItems;
        public int MaxItems { get; set; }

        public IncrementalObservableCollection(Func<int, Task<List<T>>> load)
        {
            this._loadDataAsync = load;
        }

        public async Task<int> LoadMoreItemsAsync(int count)
        {
            var data = await _loadDataAsync(count);

            foreach (var item in data)
                Add(item);

            return data.Count;
        }
    }
}
