using wallabag.Models;

namespace wallabag.Common.Messages
{
    class ShowUndoPopupMessage
    {
        private string _transactionPoint;

        public OfflineTask.OfflineTaskAction Action { get; set; }
        public int NumberOfItems { get; set; }

        public ShowUndoPopupMessage(OfflineTask.OfflineTaskAction action, int numberOfItems)
        {
            Action = action;
            NumberOfItems = numberOfItems;
        }

        public ShowUndoPopupMessage(OfflineTask.OfflineTaskAction action, int numberOfItems, string _transactionPoint) : this(action, numberOfItems)
        {
            this._transactionPoint = _transactionPoint;
        }
    }
}
