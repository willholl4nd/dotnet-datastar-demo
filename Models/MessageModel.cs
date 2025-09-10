using System.ComponentModel.DataAnnotations;

namespace dotnet_html_sortable_table.Models
{
    public class MessageModel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string ChatRoomKey { get; set; } = string.Empty;

        public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

        public Guid SenderSessionID { get; set; } 

        public string SendIPv4 { get; set; } = string.Empty;

        public string MessageContent { get; set; } = string.Empty;
    }
}
