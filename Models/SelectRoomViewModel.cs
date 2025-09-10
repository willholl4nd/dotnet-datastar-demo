
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace dotnet_html_sortable_table.Models;

public class SelectRoomViewModel
{
    [Required]
    [DisplayName("Room Code")]
    public string RoomCode { get; set; } = string.Empty;
}