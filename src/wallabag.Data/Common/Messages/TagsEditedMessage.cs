using wallabag.Data.Models;

namespace wallabag.Data.Common.Messages
{
    public class TagsEditedMessage
    {
        public Item Item { get; set; }

        public TagsEditedMessage(Item item) => Item = item;
    }
}
