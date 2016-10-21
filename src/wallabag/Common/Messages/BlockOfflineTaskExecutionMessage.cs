namespace wallabag.Common.Messages
{
    class BlockOfflineTaskExecutionMessage
    {
        public bool IsBlocked { get; set; }
        
        public BlockOfflineTaskExecutionMessage(bool blockOfflineTasks)
        {
            IsBlocked = blockOfflineTasks;
        }
    }
}
