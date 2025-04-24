using System.ComponentModel.DataAnnotations;

namespace dotnet_html_sortable_table.Models
{
    public class DemoTable {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RandInt { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        public int DemoObjectId {get;set;}
        public DemoObject DemoObject {get;set;} = default!;
    }

    public class DemoObject {
        [Key]
        public int Id {get;set;}

        [Required]
        public bool IdSort {get; set;} = default!;

        [Required]
        public bool RandIntSort {get; set;} = default!;

        [Required]
        public bool NameSort {get; set;} = default!;

        public IList<DemoTable> Table {get;set;} = default!;
    }
}
