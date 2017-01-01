using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace wallabag.Common
{
    public class IncrementalObservableCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        private Func<uint, Task<List<T>>> load;
        public bool HasMoreItems { get { return Items.Count < MaxItems; } }
        public int MaxItems { get; set; }

        public IncrementalObservableCollection(Func<uint, Task<List<T>>> load)
        {
            this.load = load;
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return AsyncInfo.Run(async c =>
            {
                var data = await load(count);

                foreach (var item in data)
                    Add(item);

                return new LoadMoreItemsResult { Count = (uint)data.Count };
            });
        }
    }
}
