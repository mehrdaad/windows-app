namespace wallabag.Data.Common.Messages
{
    public class UpdateItemMessage
    {
        public int ItemId { get; set; }

        public UpdateItemMessage(int itemId)
        {
            ItemId = itemId;
        }
    }
}
