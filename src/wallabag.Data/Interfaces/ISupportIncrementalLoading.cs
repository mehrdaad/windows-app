using System.Threading.Tasks;

namespace wallabag.Data.Interfaces
{
    public interface ISupportIncrementalLoading
    {
        bool HasMoreItems { get; }
        Task<int> LoadMoreItemsAsync(int count);
    }
}
