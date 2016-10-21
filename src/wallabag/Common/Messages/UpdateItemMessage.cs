namespace wallabag.Common.Messages
{
    class UpdateItemMessage
    {
        public int ItemId { get; set; }

        public UpdateItemMessage(int itemId)
        {
            ItemId = itemId;
        }
    }
}
