namespace dotnet_html_sortable_table.Models
{
    public class MessageViewModel
    {
        public DateTimeOffset DateCreated { get; set; }

        public bool IsMine { get; set; }

        public Guid SenderSessionID { get; set; }

        public string MessageContent { get; set; }
    }

    public class ChatViewModel 
    {
        public Guid MySenderId { get; set; }

        public IEnumerable<MessageViewModel> Messages { get; set; }

        public bool SSERunning { get; set; } = true;
    } 
}
