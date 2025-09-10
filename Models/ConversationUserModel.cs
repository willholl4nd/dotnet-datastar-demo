using System.ComponentModel.DataAnnotations;

namespace dotnet_html_sortable_table.Models
{
    public class ConversationUserModel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SessionId { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public bool IsStreaming { get; set; } = true;
    }
}
