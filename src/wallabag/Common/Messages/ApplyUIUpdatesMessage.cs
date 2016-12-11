namespace wallabag.Common.Messages
{
    class ApplyUIUpdatesMessage
    {
        public bool ClearQueue { get; set; } = false;

        public ApplyUIUpdatesMessage() { }
        public ApplyUIUpdatesMessage(bool clearQueue)
        {
            ClearQueue = clearQueue;
        }
    }
}
